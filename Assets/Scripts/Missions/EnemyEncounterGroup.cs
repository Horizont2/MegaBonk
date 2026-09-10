using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyEncounterGroup : MonoBehaviour
{
    public enum EncounterStyle { Patrol, Camp }

    [Header("Style")]
    public EncounterStyle style = EncounterStyle.Patrol;

    [Header("Sentry Horn")]
    [Tooltip("This patrol carries a horn: it spots the player further out and raises a region-wide alarm.")]
    public bool hasHorn = false;
    [Tooltip("How far the horn-bearer notices the player. Deliberately much longer than aggroRange.")]
    public float hornSpotRange = 30f;
    [Tooltip("Seconds between being seen and the horn actually sounding. This is the player's window to kill the sentry or break sight.")]
    public float hornSpotDelay = 1.6f;

    [Header("Enemy Setup")]
    public GameObject[] enemyPrefabs;
    [Range(1, 8)] public int enemyCount = 3;
    [Tooltip("Радіус, у якому вороги розставляються відносно центру групи. Для кампу — радіус кільця навколо вогнища")]
    public float spawnSpread = 2.5f;

    [Header("Passive Behavior")]
    [Tooltip("Радіус блукання навколо anchor у патрулі. Для кампу не використовується (камп-стражі стоять)")]
    public float roamRadius = 3.5f;
    [Tooltip("Дистанція, на якій група агриться")]
    public float aggroRange = 14f;

    [Header("Patrol (Style=Patrol)")]
    [Tooltip("Якщо передано — група циклічно ходить по точках. Інакше WorldEncounterDirector згенерує їх")]
    public Transform[] patrolPoints;
    [Tooltip("Скільки секунд група стоїть на кожному waypoint")]
    public float waypointPauseDuration = 6f;
    [Tooltip("Швидкість руху центру групи між waypoints")]
    public float patrolMoveSpeed = 1.6f;

    [Header("Camp (Style=Camp)")]
    [Tooltip("Префаб вогнища у центрі табору. Якщо null — без вогнища")]
    public GameObject campfirePrefab;
    [Tooltip("Чи мають вороги повертатися обличчям до вогнища, коли стоять")]
    public bool campGuardsFaceFire = true;

    [Header("Streaming")]
    // Doubling the number of encounters on the map without this would put ~260
    // enemies live at once -- 260 Animators and CharacterControllers, which the
    // frame budget will not carry. Instead every encounter exists as a cheap
    // marker and only becomes real enemies when the player comes near, so the
    // region can be densely populated while the live count stays bounded.
    [Tooltip("Spawn the group only once the player is this close. 0 = always spawned.")]
    public float activationDistance = 0f;
    [Tooltip("Despawn again beyond this distance. Only ever applies to a group that is still untouched, so nothing can be farmed by walking away.")]
    public float deactivationDistance = 150f;

    [Header("Lifecycle")]
    public bool autoStart = true;
    [Tooltip("Винагорода, що випадає на місці групи, коли всіх вбили. Звичайно XP кристал")]
    public GameObject clearedRewardPrefab;
    [Tooltip("Скільки штук винагороди упасти при зачистці")]
    [Range(0, 8)] public int clearedRewardCount = 3;

    private readonly List<EnemyAI> spawnedEnemies = new List<EnemyAI>(8);
    private int currentPatrolIndex = 0;
    private bool spawned = false;
    private bool clearedFired = false;
    private GameObject spawnedCampfire;
    private float clearedCheckTimer = 0f;

    public IReadOnlyList<EnemyAI> SpawnedEnemies => spawnedEnemies;

    private void Start()
    {
        if (autoStart && activationDistance <= 0f) SpawnGroup();
    }

    // ---- Streaming -----------------------------------------------------------
    private float streamCheckTimer;
    private Transform streamPlayer;

    private void UpdateStreaming()
    {
        if (activationDistance <= 0f || !autoStart) return;

        streamCheckTimer -= Time.deltaTime;
        if (streamCheckTimer > 0f) return;
        streamCheckTimer = 0.75f;

        if (streamPlayer == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            streamPlayer = pc.transform;
        }

        float dist = Vector3.Distance(transform.position, streamPlayer.position);

        if (!spawned)
        {
            // Hold everything back while a totem is being captured. Streaming is
            // distance-driven, and the player fighting a wave at a totem is
            // standing still inside the activation radius of every encounter
            // around it — so exactly at the moment the fight peaks, the map
            // quietly hands it another camp. The group is not lost, only
            // deferred: this runs again once the totem is purified.
            if (RegionTotem.AnyCaptureFightRunning) return;
            if (dist <= activationDistance) SpawnGroup();
            return;
        }

        if (dist <= deactivationDistance) return;

        // Only pack an encounter away if the player never engaged it. A group
        // that has lost members or been alerted stays real: rebuilding it later
        // would quietly heal the survivors and refund the fight.
        if (clearedFired) return;
        if (spawnedEnemies.Count != enemyCount) return;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyAI e = spawnedEnemies[i];
            if (e == null) return;                              // someone died here
            if (e.IsAggroed || e.IsSearching) return;           // still in play
            if (e.CurrentHealth < e.maxHealth - 0.01f) return;  // wounded
        }

        DespawnGroup();
    }

    private void DespawnGroup()
    {
        StopAllCoroutines();
        for (int i = 0; i < spawnedEnemies.Count; i++)
            if (spawnedEnemies[i] != null) Destroy(spawnedEnemies[i].gameObject);
        spawnedEnemies.Clear();

        if (spawnedCampfire != null) { Destroy(spawnedCampfire); spawnedCampfire = null; }

        spawned = false;
        currentPatrolIndex = 0;
        hornSpotTimer = 0f;
    }

    public void SpawnGroup()
    {
        if (spawned) return;
        // Stagger the actual spawns over a few frames so a 10-12 enemy
        // camp doesn't Instantiate everything at once (Animator init +
        // material upload + collider bake all hit the main thread). The
        // group-level state (spawned flag, campfire) still happens
        // synchronously, only the enemy-by-enemy work spreads out.
        StartCoroutine(SpawnGroupRoutine());
    }

    private IEnumerator SpawnGroupRoutine()
    {
        spawned = true;

        Vector3 groupCenter = GroundedCenter(transform.position);
        transform.position = groupCenter;

        if (style == EncounterStyle.Camp && campfirePrefab != null)
        {
            spawnedCampfire = Instantiate(campfirePrefab, groupCenter, campfirePrefab.transform.rotation);
            spawnedCampfire.transform.SetParent(transform, true);
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0) yield break;

        // Spawn ~4 enemies per frame; small groups still feel instant,
        // big groups (10+) lose the spawn-frame stutter.
        const int SPAWNS_PER_FRAME = 4;
        for (int i = 0; i < enemyCount; i++)
        {
            if (i > 0 && i % SPAWNS_PER_FRAME == 0) yield return null;
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null) continue;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (style == EncounterStyle.Camp)
            {
                // Even ring around the fire so it reads as a council, not a mob.
                float angle = (i / (float)enemyCount) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                spawnPos = groupCenter + new Vector3(Mathf.Cos(angle) * spawnSpread, 0f, Mathf.Sin(angle) * spawnSpread);
                spawnPos = GroundedCenter(spawnPos);

                Vector3 inward = groupCenter - spawnPos;
                inward.y = 0f;
                spawnRot = inward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(inward.normalized) : Quaternion.identity;
            }
            else
            {
                Vector2 off = Random.insideUnitCircle * spawnSpread;
                spawnPos = groupCenter + new Vector3(off.x, 0f, off.y);
                spawnPos = GroundedCenter(spawnPos);
                spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            GameObject obj = Instantiate(prefab, spawnPos, spawnRot);
            EnemyAI ai = obj.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.startPassive = true;
                ai.parentGroup = this;
                ai.aggroRange = aggroRange;
                // Encounter enemies belong to a place. They may give up a chase
                // and walk back to it; the radial spawner's horde may not.
                ai.canDeAggro = true;

                if (style == EncounterStyle.Camp)
                {
                    // Each camp guard stays put at its own spot. The fire is the anchor only
                    // for facing direction.
                    ai.anchorPoint = spawnPos;
                    ai.anchorTransform = null;
                    ai.roamRadius = 0.2f;
                    ai.roamWhilePassive = false;
                    ai.faceAnchorWhenIdle = campGuardsFaceFire;
                }
                else
                {
                    // Patrol enemies follow the group center, which the patrol routine moves.
                    ai.anchorTransform = transform;
                    ai.roamRadius = roamRadius;
                    ai.roamWhilePassive = true;
                    ai.faceAnchorWhenIdle = false;
                }

                // Record the post AFTER the anchor is configured, so a search or
                // a de-aggro sends this enemy back to its own station rather
                // than to wherever it stood when the fight ended.
                ai.CapturePost();
                spawnedEnemies.Add(ai);
            }
        }

        if (style == EncounterStyle.Patrol && patrolPoints != null && patrolPoints.Length > 1)
        {
            StartCoroutine(PatrolRoutine());
        }
    }

    private Vector3 GroundedCenter(Vector3 pos)
    {
        // Raycast wins (handles non-terrain ground) with terrain SampleHeight as fallback.
        if (Physics.Raycast(pos + Vector3.up * 60f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            pos.y = hit.point.y;
            return pos;
        }
        if (Terrain.activeTerrain != null)
        {
            pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        }
        return pos;
    }

    private IEnumerator PatrolRoutine()
    {
        // Let spawned enemies finish their rise-from-ground animation before
        // we start dragging the group center.
        yield return new WaitForSeconds(2f);

        while (true)
        {
            if (AnyBusy())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            Transform wp = patrolPoints[currentPatrolIndex];
            if (wp == null)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                yield return null;
                continue;
            }

            bool interrupted = false;
            while (true)
            {
                // Pause the route while the group is fighting or searching, then
                // pick it back up. This used to `yield break`, which killed the
                // coroutine outright: a patrol that aggroed ONCE never patrolled
                // again for the rest of the run, and the region filled up with
                // groups frozen wherever their last fight ended.
                if (AnyBusy()) { interrupted = true; break; }

                Vector3 toWP = wp.position - transform.position;
                toWP.y = 0f;
                float distXZ = toWP.magnitude;
                if (distXZ < 1.2f) break;

                Vector3 step = toWP / distXZ * (patrolMoveSpeed * Time.deltaTime);
                Vector3 nextPos = transform.position + step;
                if (Terrain.activeTerrain != null)
                    nextPos.y = Terrain.activeTerrain.SampleHeight(nextPos) + Terrain.activeTerrain.transform.position.y;
                transform.position = nextPos;

                yield return null;
            }

            // Interrupted mid-leg: don't burn the waypoint or stand around at a
            // spot we never reached. Loop back and re-approach the same one once
            // the fight or search is over.
            if (interrupted) continue;

            yield return new WaitForSeconds(waypointPauseDuration);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void Update()
    {
        UpdateStreaming();
        UpdateHorn();

        // Cleanup check — fires the cleared reward once when everyone is dead/null.
        if (!spawned || clearedFired || spawnedEnemies.Count == 0) return;

        clearedCheckTimer -= Time.deltaTime;
        if (clearedCheckTimer > 0f) return;
        clearedCheckTimer = 1f;

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null) return; // someone alive
        }

        clearedFired = true;
        OnGroupCleared();
    }

    private void OnGroupCleared()
    {
        if (TutorialHints.Instance != null)
            TutorialHints.Instance.ShowIfNew("EncounterCleared",
                "Cleared encounter — bonus loot dropped at the camp center. Wipe more groups to stack rewards.", 5f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Encounter_Cleared);

        if (clearedRewardPrefab == null || clearedRewardCount <= 0) return;

        Vector3 center = transform.position;
        if (Terrain.activeTerrain != null)
            center.y = Terrain.activeTerrain.SampleHeight(center) + Terrain.activeTerrain.transform.position.y;

        for (int i = 0; i < clearedRewardCount; i++)
        {
            Vector2 off = Random.insideUnitCircle * 1.2f;
            Vector3 pos = center + new Vector3(off.x, 0.5f, off.y);

            GameObject reward = null;
            if (ObjectPoolManager.Instance != null)
                reward = ObjectPoolManager.Instance.SpawnFromPool(clearedRewardPrefab, pos, Quaternion.identity);
            if (reward == null)
                Instantiate(clearedRewardPrefab, pos, Quaternion.identity);
        }
    }

    private bool AnyAggroed()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyAI e = spawnedEnemies[i];
            if (e != null && e.IsAggroed) return true;
        }
        return false;
    }

    // Fighting OR searching. The patrol route must stay parked for both, or the
    // group centre would drag its own searchers away from the area they were
    // sent to comb.
    private bool AnyBusy()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyAI e = spawnedEnemies[i];
            if (e != null && (e.IsAggroed || e.IsSearching)) return true;
        }
        return false;
    }

    public void AlertAll()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyAI e = spawnedEnemies[i];
            if (e != null) e.Aggro();
        }
    }

    // A horn went up somewhere. Go and look -- do NOT simply aggro. The group
    // converges on the reported position and searches it; if the player is not
    // there they drift back to their own route. That is what makes moving after
    // being spotted worthwhile.
    public void RespondToAlert(Vector3 position)
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            EnemyAI e = spawnedEnemies[i];
            if (e != null) e.AlertTo(position);
        }
    }

    // ---- Sentry horn ---------------------------------------------------------
    //
    // Long sight, slow reaction. The delay is the whole design: being seen is not
    // the failure state, being seen AND letting the horn finish is. It gives the
    // player a readable window to close the distance and silence the sentry, or
    // to break line of sight and have the sighting come to nothing.
    private float hornSpotTimer;
    private float hornRecheckTimer;

    private void UpdateHorn()
    {
        if (!hasHorn || !spawned) return;
        if (RegionAlertDirector.Instance == null) return;
        if (RegionAlertDirector.Instance.AlertActive) return;   // already ringing

        hornRecheckTimer -= Time.deltaTime;
        if (hornRecheckTimer > 0f) return;
        hornRecheckTimer = 0.2f;

        EnemyAI bearer = LivingBearer();
        if (bearer == null) return;    // sentry is dead: nobody left to sound it

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.currentHealth <= 0) { hornSpotTimer = 0f; return; }

        Vector3 eye = bearer.transform.position + Vector3.up * 1.5f;
        Vector3 to = (player.transform.position + Vector3.up) - eye;
        float dist = to.magnitude;

        bool seen = dist <= hornSpotRange && !Physics.Raycast(eye, to / dist, dist - 0.5f,
                                                              SightBlockerMask(), QueryTriggerInteraction.Ignore);
        if (!seen)
        {
            // Bleed the timer back down rather than resetting it, so flickering
            // in and out of cover still eventually gets you spotted.
            hornSpotTimer = Mathf.Max(0f, hornSpotTimer - 0.4f);
            return;
        }

        hornSpotTimer += 0.2f;
        if (hornSpotTimer < hornSpotDelay) return;

        hornSpotTimer = 0f;
        if (RegionAlertDirector.Instance.RaiseAlert(player.transform.position, bearer.transform))
            AlertAll();   // the sentry's own group engages immediately
    }

    private EnemyAI LivingBearer()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
            if (spawnedEnemies[i] != null) return spawnedEnemies[i];
        return null;
    }

    private static int s_sightMask = -1;
    private static int SightBlockerMask()
    {
        if (s_sightMask != -1) return s_sightMask;
        int mask = 0;
        string[] names = { "Default", "Obstacles", "Nature" };
        foreach (var n in names)
        {
            int l = LayerMask.NameToLayer(n);
            if (l >= 0) mask |= 1 << l;
        }
        s_sightMask = mask;
        return s_sightMask;
    }
}
