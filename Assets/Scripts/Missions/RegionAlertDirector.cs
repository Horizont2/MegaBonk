using System.Collections.Generic;
using UnityEngine;

// Region-wide alarm coordination.
//
// A watchtower's beam or a sentry's horn does not simply hand every enemy the
// player's coordinates. It raises an ALERT at a place: nearby groups converge on
// that place and search it, and while the alert stands a slow trickle of
// reinforcements marches in from off-screen. If nobody finds the player the
// alert lapses and the region goes back to sleep.
//
// The whole point is that breaking contact works. Three rules keep it that way:
//
//   * an alert EXPIRES. Outrun it, hide, wait, and the region forgets you.
//   * reinforcements spawn FAR AWAY and walk in. Enemies materialising next to
//     the player reads as the game cheating rather than as the world reacting,
//     and it removes any decision from the encounter.
//   * there is a BUDGET. One concurrent alert, a floor on how often a new one
//     may be raised, and a cap on reinforcements per alert. The fantasy is a
//     garrison noticing an intruder, not an endless faucet.
[DisallowMultipleComponent]
public class RegionAlertDirector : MonoBehaviour
{
    public static RegionAlertDirector Instance { get; private set; }

    [Header("Alert Shape")]
    [Tooltip("How far from the alarm neighbouring groups are called in.")]
    public float alertRadius = 70f;
    [Tooltip("Seconds an alert stays live before the region calms down.")]
    public float alertDuration = 30f;
    [Tooltip("Minimum seconds between two alerts, however many alarms go off.")]
    public float alertCooldown = 25f;
    [Tooltip("Groups pulled in by one alert. Keeps a horn from emptying the map.")]
    public int maxGroupsPerAlert = 3;

    [Header("Reinforcements")]
    public bool spawnReinforcements = true;
    public GameObject[] reinforcementPrefabs;
    [Tooltip("Seconds between reinforcement squads while an alert is live.")]
    public float reinforcementInterval = 10f;
    [Range(1, 5)] public int reinforcementSquadSize = 2;
    [Tooltip("Total reinforcements one alert may ever produce.")]
    public int maxReinforcementsPerAlert = 6;
    [Tooltip("Distance from the player they march in from. Must be beyond sight.")]
    public float reinforcementSpawnDistance = 45f;

    [Header("Feedback")]
    // AudioID is a static class of string constants, not an enum.
    public string hornSound = AudioID.Enemy_Agro;

    private bool alertActive;
    private float alertEndTime;
    private float nextAlertAllowedTime;
    private float nextReinforcementTime;
    private int reinforcementsThisAlert;
    private Vector3 alertPosition;

    public bool AlertActive => alertActive;
    public Vector3 AlertPosition => alertPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // The one entry point. Anything that spots the player calls this.
    // Returns false when the alarm was swallowed by the cooldown or an alert
    // that is already running, so the caller can skip its own horn animation.
    public bool RaiseAlert(Vector3 position, Transform source)
    {
        if (alertActive) return false;
        if (Time.time < nextAlertAllowedTime) return false;
        // A totem capture is already the fight. Layering a region-wide alarm and
        // its marching reinforcements on top of the wave turns the climax of a
        // region into an undifferentiated crowd, and the horn stops meaning
        // anything because it is drowned out by the thing it interrupted.
        if (RegionTotem.AnyCaptureFightRunning) return false;

        alertActive = true;
        alertPosition = position;
        alertEndTime = Time.time + alertDuration;
        nextReinforcementTime = Time.time + reinforcementInterval;
        reinforcementsThisAlert = 0;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX3D(hornSound, source != null ? source.position : position);

        SummonNearbyGroups(position);

        if (TutorialHints.Instance != null)
            TutorialHints.Instance.ShowIfNew("RegionAlarm",
                "You've been spotted — a horn has gone up. Patrols are converging on your last known position. Break line of sight and move; the alarm lapses if they lose you.", 7f);

        return true;
    }

    // Send the closest groups to the noise. Closest, not all: a horn should
    // commit a believable number of nearby patrols, and pulling in everything
    // within the radius both trivialises the rest of the map and buries the
    // player under a single unavoidable wave.
    private void SummonNearbyGroups(Vector3 position)
    {
        var groups = FindObjectsByType<EnemyEncounterGroup>(FindObjectsSortMode.None);
        var byDistance = new List<(float d, EnemyEncounterGroup g)>(groups.Length);

        float radiusSqr = alertRadius * alertRadius;
        foreach (var g in groups)
        {
            if (g == null) continue;
            float d = (g.transform.position - position).sqrMagnitude;
            if (d > radiusSqr) continue;
            byDistance.Add((d, g));
        }

        byDistance.Sort((a, b) => a.d.CompareTo(b.d));

        int taken = Mathf.Min(maxGroupsPerAlert, byDistance.Count);
        for (int i = 0; i < taken; i++)
            byDistance[i].g.RespondToAlert(position);
    }

    private void Update()
    {
        if (!alertActive) return;

        if (Time.time >= alertEndTime)
        {
            alertActive = false;
            nextAlertAllowedTime = Time.time + alertCooldown;
            return;
        }

        // A capture started while the alarm was still ringing: call it off. The
        // patrols already dispatched keep coming — they were sent, and taking
        // them back would read as the region forgetting mid-stride — but no
        // further reinforcements march into the totem fight.
        if (RegionTotem.AnyCaptureFightRunning)
        {
            alertActive = false;
            nextAlertAllowedTime = Time.time + alertCooldown;
            return;
        }

        if (!spawnReinforcements) return;
        if (reinforcementsThisAlert >= maxReinforcementsPerAlert) return;
        if (Time.time < nextReinforcementTime) return;

        nextReinforcementTime = Time.time + reinforcementInterval;
        SpawnReinforcementSquad();
    }

    private void SpawnReinforcementSquad()
    {
        if (reinforcementPrefabs == null || reinforcementPrefabs.Length == 0) return;

        Transform player = FindPlayer();
        if (player == null) return;

        // Come in from one direction as a squad, so the player can read where
        // they are arriving from and choose to go the other way. Scattering them
        // evenly around the player would be a surround, which is not a reaction
        // to an alarm -- it is an ambush the player had no chance to avoid.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 squadCenter = player.position + dir * reinforcementSpawnDistance;

        int count = Mathf.Min(reinforcementSquadSize, maxReinforcementsPerAlert - reinforcementsThisAlert);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = reinforcementPrefabs[Random.Range(0, reinforcementPrefabs.Length)];
            if (prefab == null) continue;

            Vector2 jitter = Random.insideUnitCircle * 3f;
            Vector3 pos = squadCenter + new Vector3(jitter.x, 0f, jitter.y);
            pos.y = GroundY(pos);

            GameObject obj = Instantiate(prefab, pos, Quaternion.LookRotation(-dir));
            var ai = obj.GetComponent<EnemyAI>();
            if (ai != null)
            {
                // Reinforcements can give up too. Without this an alarm would
                // leave a permanent tail of enemies following the player for the
                // rest of the run, which is the exact problem the alert system
                // is supposed to avoid.
                ai.canDeAggro = true;
                ai.startPassive = true;
                ai.anchorPoint = pos;
                ai.roamRadius = 3f;
                ai.aggroRange = 16f;
                ai.leashRange = 90f;      // they were sent after the player; give them rope
                ai.CapturePost();
                ai.AlertTo(player.position);
            }
            reinforcementsThisAlert++;
        }
    }

    private static Transform FindPlayer()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        return pc != null ? pc.transform : null;
    }

    private static float GroundY(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 250f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        // Region levels are built from several terrains, so pick the one that
        // actually contains this point rather than whichever registered first.
        Terrain[] all = Terrain.activeTerrains;
        if (all != null)
        {
            foreach (var t in all)
            {
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position;
                Vector3 s = t.terrainData.size;
                if (pos.x >= o.x && pos.x <= o.x + s.x && pos.z >= o.z && pos.z <= o.z + s.z)
                    return t.SampleHeight(pos) + o.y;
            }
        }
        return pos.y;
    }
}
