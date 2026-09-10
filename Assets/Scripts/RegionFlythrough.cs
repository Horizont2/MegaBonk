using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A cinematic flythrough of a region assault, on a key press.
//
// ==== WHY THIS INSTEAD OF A BUILT SHOT ====
//
// The trailer's marching-legion shot built its own terrain, its own crowd and its
// own lighting, and every one of those was a thing that could be — and was —
// wrong. A live region already has a dressed, lit, populated world with a real
// battle in it. Filming that is both far less work and far more honest: what the
// trailer shows is what the game is.
//
// ==== WHAT THE FLYTHROUGH SAYS ====
//
// Four beats, in the order that tells a region's story in one unbroken move:
//
//   PLACE. High and wide, looking down the region toward the corrupted totem.
//   Before anything else the audience needs to know where they are and what is
//   wrong with it.
//
//   THREAT. A long descending run at the largest concentration of enemies, losing
//   height and gaining speed, so the ground rises up around the lens. Descending
//   INTO something reads as commitment; circling it reads as a menu screen.
//
//   HERO. A low pass close by the player — the one thing standing against it.
//   This beat only works because the previous one made the odds visible.
//
//   OBJECTIVE. Climb away and turn to end on the totem. The shot finishes by
//   pointing at what the player has to do.
//
// The path runs through a Catmull-Rom spline rather than straight between
// waypoints: a camera that changes direction sharply at a waypoint reads as a
// machine following instructions. The aim is damped separately from the move, so
// the framing floats instead of locking.
//
// Everything is restored on finish or on cancel, including when the flythrough is
// interrupted — a cinematic that can leave the player stuck in a camera is worse
// than no cinematic.
[DisallowMultipleComponent]
public class RegionFlythrough : MonoBehaviour
{
    [Header("Trigger")]
    // F6, not F9. F9 is already QuickLoad (SaveSlotManager), which is why
    // pressing it restored a save and played the resource sound instead of
    // starting anything. F5, F8 and F10 are taken as well.
    [Tooltip("Press this during a region assault to play the flythrough. EDITOR ONLY — the key does not exist in a build.")]
    public KeyCode hotkey = KeyCode.F6;
    [Tooltip("Keys that abort it and hand control straight back.")]
    public KeyCode[] cancelKeys = { KeyCode.Escape, KeyCode.Space };

    [Header("Timing (seconds)")]
    public float establish = 3.2f;
    public float descend = 4.6f;
    public float heroPass = 3.4f;
    public float reveal = 4.0f;

    [Header("Framing")]
    [Tooltip("Height above the player the shot opens at.")]
    public float openHeight = 42f;
    [Tooltip("How far behind the player the opening sits, along the line to the objective.")]
    public float openBack = 34f;
    [Tooltip("Height of the low pass beside the player. Low enough to feel the ground going by.")]
    public float heroHeight = 3.2f;
    [Tooltip("Sideways offset of the hero pass. The camera goes PAST the player, not at them.")]
    public float heroSide = 7f;
    public float endHeight = 26f;
    [Tooltip("Never let the lens get closer than this to the ground, whatever the spline asks for.")]
    public float groundClearance = 2.2f;

    [Header("Lens")]
    public float openFov = 34f;      // long: compresses the land, makes it read as vast
    public float actionFov = 62f;    // wide: speed, and the ground rushing past
    public float endFov = 40f;

    [Header("Feel")]
    [Tooltip("How fast the aim catches the camera. Lower floats more.")]
    public float lookDamping = 2.8f;
    [Tooltip("Handheld amplitude in metres. Tiny — this is a crane, not a shoulder.")]
    public float handheld = 0.06f;

    public static bool IsPlaying { get; private set; }

    // EDITOR ONLY.
    //
    // This is a capture tool, not a feature: it takes the camera away from the
    // player and blocks their input, which is exactly what a shipped build must
    // never let a stray key press do. Compiled out entirely rather than merely
    // hidden, so there is no key to find and nothing to disable.
    //
    // Play() itself stays compiled in every configuration, so a Timeline or an
    // in-game trigger could still drive it deliberately one day.
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<RegionFlythrough>() != null) return;
        var go = new GameObject("[RegionFlythrough]");
        DontDestroyOnLoad(go);
        go.AddComponent<RegionFlythrough>();
    }

    private void Update()
    {
        if (IsPlaying) return;
        if (!Input.GetKeyDown(hotkey)) return;
        Play();
    }
#endif

    public void Play()
    {
        if (IsPlaying) return;

        Camera cam = Camera.main;
        var player = FindFirstObjectByType<PlayerController>();
        if (cam == null || player == null)
        {
            // Named separately, because "needs a camera and a player" does not
            // tell you which one is missing when you are staring at a scene that
            // obviously has both.
            Debug.LogWarning($"[Flythrough] Cannot start: " +
                             $"{(cam == null ? "no camera tagged MainCamera" : "camera OK")}, " +
                             $"{(player == null ? "no PlayerController in the scene" : "player OK")}.");
            return;
        }
        Debug.Log("[Flythrough] Starting.");
        StartCoroutine(Run(cam, player));
    }

    // ======================= the shot =======================

    private IEnumerator Run(Camera cam, PlayerController player)
    {
        IsPlaying = true;

        var camFollow = cam.GetComponent<CameraFollow>();
        Transform camT = cam.transform;

        // Remember everything before touching it. Restoring from a snapshot taken
        // here is the only way an interrupted flythrough can hand back exactly
        // what it borrowed.
        Vector3 homePos = camT.position;
        Quaternion homeRot = camT.rotation;
        float homeFov = cam.fieldOfView;
        bool homeCinematic = camFollow != null && camFollow.isCinematicMode;
        bool homeBlocked = player.isControlBlocked;

        if (camFollow != null) camFollow.isCinematicMode = true;
        player.isControlBlocked = true;
        player.isCinematicInvincible = true;   // a stray arrow mid-shot ruins the take

        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.ShowCinematicBars();
            GlobalHUD.Instance.SetCinematicDoF(true);
            GlobalHUD.Instance.ShowSkipPrompt();
        }

        BuildPath(player.transform, out Vector3[] path, out Transform[] lookAt, out Vector3[] lookFallback);

        float total = establish + descend + heroPass + reveal;
        float t = 0f;
        Vector3 aim = ResolveLook(lookAt, lookFallback, 0);
        float seed = Random.Range(0f, 100f);
        bool cancelled = false;

        while (t < total)
        {
            if (WantsCancel()) { cancelled = true; break; }

            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / total);

            // Ease the whole traverse, so the shot starts and stops without a jerk
            // even though the spline itself is evenly parameterised.
            float e = u * u * u * (u * (u * 6f - 15f) + 10f);

            Vector3 pos = Spline(path, e);
            pos.y = Mathf.Max(pos.y, GroundAt(pos) + groundClearance);

            // A crane has a pulse, not a tremor. Perlin, so it never repeats.
            pos += camT.right * ((Mathf.PerlinNoise(seed + t * 0.7f, 0f) - 0.5f) * handheld * 2f)
                 + camT.up * ((Mathf.PerlinNoise(0f, seed + t * 0.9f) - 0.5f) * handheld * 2f);

            camT.position = pos;

            Vector3 want = ResolveLook(lookAt, lookFallback, SegmentFor(t));
            aim = Vector3.Lerp(aim, want, 1f - Mathf.Exp(-lookDamping * Time.unscaledDeltaTime));
            Vector3 toAim = aim - camT.position;
            if (toAim.sqrMagnitude > 0.001f) camT.rotation = Quaternion.LookRotation(toAim.normalized);

            cam.fieldOfView = FovAt(t);

            yield return null;
        }

        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.HideSkipPrompt();
            GlobalHUD.Instance.HideCinematicBars();
            GlobalHUD.Instance.SetCinematicDoF(false);
        }

        // Hand back exactly what was borrowed. On a cancel the camera snaps home
        // rather than easing, because the player asked for control NOW.
        cam.fieldOfView = homeFov;
        camT.position = homePos;
        camT.rotation = homeRot;
        if (camFollow != null) camFollow.isCinematicMode = homeCinematic;
        player.isControlBlocked = homeBlocked;
        player.isCinematicInvincible = false;

        IsPlaying = false;
        Debug.Log(cancelled ? "[Flythrough] Cancelled — control returned."
                            : "[Flythrough] Finished.");
    }

    private bool WantsCancel()
    {
        if (Input.GetKeyDown(hotkey)) return true;
        if (cancelKeys == null) return false;
        for (int i = 0; i < cancelKeys.Length; i++)
            if (Input.GetKeyDown(cancelKeys[i])) return true;
        return false;
    }

    private int SegmentFor(float t)
    {
        if (t < establish) return 0;
        if (t < establish + descend) return 1;
        if (t < establish + descend + heroPass) return 2;
        return 3;
    }

    private float FovAt(float t)
    {
        // Long to open (the land reads as vast), wide through the descent (speed,
        // ground rushing past), settling back for the reveal.
        if (t < establish)
            return Mathf.Lerp(openFov, actionFov, Mathf.Clamp01(t / Mathf.Max(0.01f, establish)));
        if (t < establish + descend + heroPass)
            return actionFov;
        float k = Mathf.Clamp01((t - establish - descend - heroPass) / Mathf.Max(0.01f, reveal));
        return Mathf.Lerp(actionFov, endFov, k);
    }

    // ======================= the path =======================

    private void BuildPath(Transform player, out Vector3[] path, out Transform[] lookAt, out Vector3[] lookFallback)
    {
        Transform totem = FindObjective();
        Vector3 objective = totem != null ? totem.position : player.position + player.forward * 60f;

        Vector3 toObjective = objective - player.position;
        toObjective.y = 0f;
        if (toObjective.sqrMagnitude < 1f) toObjective = player.forward;
        toObjective.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, toObjective);

        Vector3 threat = FindEnemyCluster(player.position, out int enemyCount);
        bool haveThreat = enemyCount > 0;
        if (!haveThreat) threat = Vector3.Lerp(player.position, objective, 0.5f);

        // PLACE: high, behind the player, already looking down the line of march.
        Vector3 p0 = player.position - toObjective * openBack + Vector3.up * openHeight;
        // Falling toward the threat, still high enough to see the ground plan.
        Vector3 p1 = Vector3.Lerp(player.position, threat, 0.55f) + Vector3.up * (openHeight * 0.38f) + side * 12f;
        // THREAT: low over them.
        Vector3 p2 = threat + Vector3.up * 7f - toObjective * 6f;
        // HERO: past the player, low and to one side. Past, not at — a camera that
        // stops on someone is a portrait; one that passes them is a moment.
        Vector3 p3 = player.position + side * heroSide + Vector3.up * heroHeight - toObjective * 4f;
        // OBJECTIVE: climbing away, ending pointed at the totem.
        Vector3 p4 = Vector3.Lerp(player.position, objective, 0.45f) + Vector3.up * endHeight - side * 8f;

        path = new[] { p0, p1, p2, p3, p4 };

        // Aim targets per beat. Transforms where possible, so a moving subject is
        // tracked rather than a stale position filmed.
        lookAt = new Transform[4];
        lookFallback = new Vector3[4];

        lookAt[0] = totem; lookFallback[0] = objective;          // place: the problem
        lookAt[1] = null;  lookFallback[1] = threat;             // descent: the threat
        lookAt[2] = player; lookFallback[2] = player.position;   // hero
        lookAt[3] = totem; lookFallback[3] = objective;          // objective

        Debug.Log($"[Flythrough] Path built: objective {(totem != null ? totem.name : "none — using player forward")}, " +
                  $"{enemyCount} enemies clustered at {threat}. " +
                  $"Press {hotkey} again, Esc or Space to cancel.");
    }

    // The corrupted totem the player is meant to reach: the first that is neither
    // purified nor already activated.
    private static Transform FindObjective()
    {
        RegionTotem firstDormant = null;
        foreach (var t in FindObjectsByType<RegionTotem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t == null || t.isPurified) continue;
            // An ALREADY-ACTIVATED totem is where the fight is, so it wins
            // outright. Otherwise the first dormant one is the next objective.
            if (t.isActivated) return t.transform;
            if (firstDormant == null) firstDormant = t;
        }
        return firstDormant != null ? firstDormant.transform : null;
    }

    // Where the fighting is. The centroid of the enemies near the player, which is
    // a better subject than any single one: a crowd has a shape, an individual
    // just has a position.
    private static Vector3 FindEnemyCluster(Vector3 near, out int count)
    {
        Vector3 sum = Vector3.zero;
        count = 0;
        foreach (var e in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (e == null || e.IsDead) continue;
            if ((e.transform.position - near).sqrMagnitude > 120f * 120f) continue;
            sum += e.transform.position;
            count++;
        }
        return count > 0 ? sum / count : near;
    }

    private Vector3 ResolveLook(Transform[] lookAt, Vector3[] fallback, int segment)
    {
        segment = Mathf.Clamp(segment, 0, fallback.Length - 1);
        Transform t = lookAt[segment];
        return t != null ? t.position + Vector3.up * 1.4f : fallback[segment];
    }

    // Catmull-Rom through the waypoints. A camera that changes direction sharply
    // at a waypoint reads as a machine following instructions; a spline reads as
    // an operator who knew where they were going.
    private static Vector3 Spline(Vector3[] p, float u)
    {
        if (p.Length < 2) return p.Length == 1 ? p[0] : Vector3.zero;

        float scaled = Mathf.Clamp01(u) * (p.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(scaled), p.Length - 2);
        float f = scaled - i;

        Vector3 a = p[Mathf.Max(i - 1, 0)];
        Vector3 b = p[i];
        Vector3 c = p[i + 1];
        Vector3 d = p[Mathf.Min(i + 2, p.Length - 1)];

        return 0.5f * ((2f * b)
                     + (-a + c) * f
                     + (2f * a - 5f * b + 4f * c - d) * f * f
                     + (-a + 3f * b - 3f * c + d) * f * f * f);
    }

    private static float GroundAt(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 600f, ~0, QueryTriggerInteraction.Ignore))
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
