using System.Collections.Generic;
using UnityEngine;

// Birds that the player actually sees.
//
// ==== WHY THE OLD ONES WERE INVISIBLE ====
//
// WorldGenerator used to drop seven bird prefabs at fixed points across the whole
// terrain, 14-32 m up, and leave them there. Every part of that fights being seen:
//
//   They did not MOVE. The prefabs are animated — wings flap — but the object
//   itself never went anywhere, and peripheral vision is triggered by travel, not
//   by animation. A bird flapping on the spot is a piece of scenery.
//
//   Seven of them across an entire region is nothing. The player would have to
//   wander within a few dozen metres of one AND happen to look up.
//
//   And looking up is the problem: this is a third-person camera 2.5-5.5 m behind
//   the player. In a survivors-style game nobody tilts the camera skyward, so a
//   band of sky at 30 m altitude is simply not part of the screen.
//
// ==== WHAT THIS DOES INSTEAD ====
//
// Two behaviours, because they do different jobs.
//
//   CIRCLING FLOCKS keep the sky alive. A handful of them live in a ring around
//   the player and are RECYCLED — when one drifts out of range it is moved to a
//   fresh spot on the far side rather than destroyed and respawned, so the cost is
//   a fixed handful of transforms no matter how far the player walks. They are
//   deliberately kept out of the zenith: a bird directly overhead is off-screen,
//   and one low on the horizon is not. They are ambience — you register them
//   without looking at them.
//
//   FLUSHES are the ones players remember. Birds sitting on the ground ahead break
//   cover when the player comes near, bursting upward and away with a cry. It
//   reads instantly, it happens in the middle of the screen where the player is
//   already looking, and it is the single cheapest thing that makes a forest feel
//   inhabited rather than decorated.
//
// Nothing here touches an Animator parameter. The prefabs carry their own looping
// flap and that is all they need — and writing a parameter a controller does not
// have is how an earlier trailer script logged thousands of warnings a second and
// locked up a machine.
[DisallowMultipleComponent]
public class AmbientBirdLife : MonoBehaviour
{
    [Header("Flock prefabs")]
    [Tooltip("The Zacxophone bird prefabs work directly. Bigger flocks (10/15) read better high up, small ones (01/03) better for a flush.")]
    public GameObject[] flockPrefabs;

    [Header("Circling flocks")]
    [Tooltip("How many are alive at once. They are recycled, never accumulated, so this is the whole budget.")]
    public int circlingFlocks = 4;
    [Tooltip("Inner radius of the ring they are kept in. Too close and they fly through the camera.")]
    public float ringInner = 40f;
    public float ringOuter = 110f;
    [Tooltip("Past this distance from the player a flock is recycled to a new spot.")]
    public float recycleRadius = 165f;
    [Tooltip("Height above the ground they cruise at (random per flock).")]
    public Vector2 altitude = new Vector2(20f, 38f);
    public Vector2 cruiseSpeed = new Vector2(4f, 7f);
    [Tooltip("Degrees per second of lazy course change. Birds that fly straight look like aircraft.")]
    public float wanderTurnRate = 14f;
    [Tooltip("Degrees of roll fed from the turn rate. Banking is most of what sells flight.")]
    public float bankPerTurn = 1.6f;

    [Header("Flush (birds breaking cover)")]
    [Tooltip("How many roost points are kept seeded ahead of the player.")]
    public int roosts = 5;
    public float roostAheadMin = 20f;
    public float roostAheadMax = 55f;
    [Tooltip("The player has to get this close to startle them.")]
    public float flushRange = 13f;
    [Tooltip("...and be moving at least this fast. Standing still next to a bush should not set birds off.")]
    public float flushMinPlayerSpeed = 1.5f;
    [Tooltip("Seconds before another flush may happen anywhere. Keeps it an event rather than a texture.")]
    public float flushCooldown = 9f;
    public float flushClimbSpeed = 11f;
    public float flushDuration = 2.4f;

    [Header("Model")]
    [Tooltip("Degrees to add to the flock's yaw if the prefab's nose does not point down +Z.")]
    public float modelYawOffset = 0f;

    [Header("Audio")]
    public string flushSound = AudioID.Ambient_Crow;

    public static AmbientBirdLife Instance { get; private set; }

    // ---- state ---------------------------------------------------------------

    private enum Mode { Cruising, Flushing }

    private class Flock
    {
        public Transform t;
        public Vector3 velocity;
        public float altitude;
        public float speed;
        public float wanderSeed;
        public float bank;
        public Mode mode;
        public float modeUntil;
    }

    private readonly List<Flock> flocks = new List<Flock>(8);
    private readonly List<Vector3> roostPoints = new List<Vector3>(8);
    private Transform player;
    private Camera cam;
    private Vector3 lastPlayerPos;
    private float playerSpeed;
    private float nextFlushAllowed;
    private bool ready;

    // Created by WorldGenerator once the world exists. Everything it needs is
    // passed in, so the generator keeps owning the designer-facing settings and
    // this stays a behaviour rather than a second place to configure birds.
    public static AmbientBirdLife Install(GameObject[] prefabs, int flockCount, float minHeight, float maxHeight)
    {
        if (prefabs == null || prefabs.Length == 0 || flockCount <= 0) return null;

        var go = new GameObject("AmbientBirdLife");
        var life = go.AddComponent<AmbientBirdLife>();
        life.flockPrefabs = prefabs;
        life.circlingFlocks = Mathf.Clamp(flockCount, 1, 12);
        life.altitude = new Vector2(minHeight, maxHeight);
        return life;
    }

    private void OnEnable() { Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }

    private void Start()
    {
        if (flockPrefabs == null || flockPrefabs.Length == 0)
        {
            Debug.LogWarning("[Birds] No flock prefabs assigned — ambient birds are off.");
            enabled = false;
            return;
        }
        cam = Camera.main;
    }

    private void Update()
    {
        if (!ResolvePlayer()) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Player speed, used to decide whether a flush is earned.
        Vector3 delta = player.position - lastPlayerPos;
        delta.y = 0f;
        playerSpeed = delta.magnitude / dt;
        lastPlayerPos = player.position;

        if (!ready)
        {
            SeedFlocks();
            SeedRoosts();
            ready = true;
        }

        for (int i = 0; i < flocks.Count; i++) TickFlock(flocks[i], dt);
        TickRoosts();
    }

    private bool ResolvePlayer()
    {
        if (player != null) return true;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return false;
        player = pc.transform;
        lastPlayerPos = player.position;
        if (cam == null) cam = Camera.main;
        return true;
    }

    // ---- circling ------------------------------------------------------------

    private void SeedFlocks()
    {
        for (int i = 0; i < circlingFlocks; i++)
        {
            GameObject prefab = flockPrefabs[Random.Range(0, flockPrefabs.Length)];
            if (prefab == null) continue;

            var go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            StripForAmbience(go);

            var f = new Flock
            {
                t = go.transform,
                wanderSeed = Random.Range(0f, 100f),
                speed = Random.Range(cruiseSpeed.x, cruiseSpeed.y),
                altitude = Random.Range(altitude.x, altitude.y),
                mode = Mode.Cruising,
            };
            flocks.Add(f);
            Recycle(f, firstPlacement: true);
        }
    }

    private void TickFlock(Flock f, float dt)
    {
        if (f.t == null) return;

        if (f.mode == Mode.Flushing)
        {
            // Climbing away from whatever startled them: fast, steep, and it
            // decays back into an ordinary cruise instead of ending abruptly.
            f.t.position += f.velocity * dt;
            f.velocity = Vector3.Lerp(f.velocity, new Vector3(f.velocity.x, 0f, f.velocity.z).normalized * f.speed,
                                      1f - Mathf.Exp(-1.2f * dt));
            Face(f, f.velocity, 28f, dt);
            if (Time.time >= f.modeUntil)
            {
                f.mode = Mode.Cruising;
                f.altitude = Random.Range(altitude.x, altitude.y);
            }
            return;
        }

        // Lazy course change. Perlin rather than a sine so no two flocks ever fall
        // into step with each other, which is what makes scripted flight read as
        // scripted.
        float turn = (Mathf.PerlinNoise(f.wanderSeed, Time.time * 0.12f) - 0.5f) * 2f * wanderTurnRate;
        Quaternion yaw = Quaternion.AngleAxis(turn * dt, Vector3.up);
        Vector3 heading = yaw * new Vector3(f.velocity.x, 0f, f.velocity.z).normalized;
        if (heading.sqrMagnitude < 0.01f) heading = Vector3.forward;

        // Hold altitude above the ground beneath, not above sea level — otherwise
        // a flock crossing a hill flies into it.
        float wantY = GroundAt(f.t.position) + f.altitude;
        float climb = Mathf.Clamp(wantY - f.t.position.y, -6f, 6f);

        f.velocity = heading * f.speed + Vector3.up * climb * 0.5f;
        f.t.position += f.velocity * dt;

        f.bank = Mathf.Lerp(f.bank, -turn * bankPerTurn, 1f - Mathf.Exp(-2.5f * dt));
        Face(f, f.velocity, 90f, dt);

        Vector3 flat = f.t.position - player.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > recycleRadius * recycleRadius) Recycle(f, firstPlacement: false);
    }

    private void Face(Flock f, Vector3 dir, float turnDegPerSec, float dt)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(dir.normalized) *
                          Quaternion.Euler(0f, modelYawOffset, f.bank);
        f.t.rotation = Quaternion.RotateTowards(f.t.rotation, want, turnDegPerSec * dt);
    }

    // Move a flock to a fresh spot in the ring instead of destroying it. A pop-in
    // in front of the lens would be worse than no bird at all, so new arrivals are
    // placed well off the camera's axis — they fly INTO view rather than appearing
    // in it.
    private void Recycle(Flock f, bool firstPlacement)
    {
        Vector3 camForward = cam != null ? cam.transform.forward : player.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.forward;
        camForward.Normalize();

        float offAxis = Random.Range(70f, 180f) * (Random.value < 0.5f ? -1f : 1f);
        Vector3 dir = Quaternion.AngleAxis(offAxis, Vector3.up) * camForward;

        // First placement is allowed anywhere in the ring, so the sky is not empty
        // for the first minute of a run while flocks wander into range.
        float r = firstPlacement ? Random.Range(ringInner, ringOuter)
                                 : Random.Range(ringOuter * 0.75f, ringOuter);

        Vector3 pos = player.position + dir * r;
        pos.y = GroundAt(pos) + f.altitude;
        f.t.position = pos;

        // Heading points across the player rather than at them, so the flock
        // crosses the visible band of sky instead of shrinking into the distance.
        Vector3 toPlayer = player.position - pos;
        toPlayer.y = 0f;
        Vector3 across = Vector3.Cross(Vector3.up, toPlayer.normalized);
        if (Random.value < 0.5f) across = -across;
        f.velocity = (across * 0.85f + toPlayer.normalized * 0.15f).normalized * f.speed;
        f.t.rotation = Quaternion.LookRotation(f.velocity) * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    // ---- flushing ------------------------------------------------------------

    private void SeedRoosts()
    {
        roostPoints.Clear();
        for (int i = 0; i < roosts; i++) roostPoints.Add(NewRoostPoint());
    }

    private Vector3 NewRoostPoint()
    {
        // Ahead of the player, spread across a wide arc — a roost directly behind
        // them can never be walked into, and one dead ahead every time reads as a
        // pattern.
        Vector3 fwd = player.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 dir = Quaternion.AngleAxis(Random.Range(-75f, 75f), Vector3.up) * fwd;
        Vector3 p = player.position + dir * Random.Range(roostAheadMin, roostAheadMax);
        p.y = GroundAt(p);
        return p;
    }

    private void TickRoosts()
    {
        for (int i = 0; i < roostPoints.Count; i++)
        {
            Vector3 p = roostPoints[i];
            Vector3 flat = p - player.position;
            flat.y = 0f;
            float d2 = flat.sqrMagnitude;

            // Walked past without triggering, or left far behind: re-seed ahead.
            if (d2 > (roostAheadMax + 45f) * (roostAheadMax + 45f))
            {
                roostPoints[i] = NewRoostPoint();
                continue;
            }

            if (d2 > flushRange * flushRange) continue;
            if (playerSpeed < flushMinPlayerSpeed) continue;
            if (Time.time < nextFlushAllowed) continue;

            Vector3 away = flat.sqrMagnitude > 0.01f ? flat.normalized : player.forward;
            FlushAt(p, away);
            roostPoints[i] = NewRoostPoint();
        }
    }

    // Public so a cinematic can call for a flush on cue — birds bursting out of
    // the canopy on a camera move is worth having under direction, not only by
    // accident.
    public void FlushNear(Vector3 worldPos, Vector3 awayDir)
    {
        if (!ready || flocks.Count == 0) return;
        FlushAt(worldPos, awayDir);
    }

    private void FlushAt(Vector3 pos, Vector3 away)
    {
        Flock f = BorrowFarthestFlock();
        if (f == null) return;

        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
        away.Normalize();

        f.t.position = new Vector3(pos.x, GroundAt(pos) + 1.2f, pos.z);
        f.velocity = (away * 0.6f + Vector3.up).normalized * flushClimbSpeed;
        f.t.rotation = Quaternion.LookRotation(f.velocity) * Quaternion.Euler(0f, modelYawOffset, 0f);
        f.mode = Mode.Flushing;
        f.modeUntil = Time.time + flushDuration;

        nextFlushAllowed = Time.time + flushCooldown;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(flushSound)
            && AudioManager.Instance.HasEvent(flushSound))
            AudioManager.Instance.PlaySFX3D(flushSound, f.t.position);
    }

    // Reuse the flock the player is least likely to be watching. Taking the
    // nearest one would make a bird visibly teleport across the sky.
    private Flock BorrowFarthestFlock()
    {
        Flock best = null;
        float bestD = -1f;
        for (int i = 0; i < flocks.Count; i++)
        {
            Flock f = flocks[i];
            if (f == null || f.t == null || f.mode == Mode.Flushing) continue;
            float d = (f.t.position - player.position).sqrMagnitude;
            if (d > bestD) { bestD = d; best = f; }
        }
        return best;
    }

    // ---- helpers -------------------------------------------------------------

    // Ambient birds are scenery with a flight path: they must not block a shot, a
    // projectile or a footstep, and they must not draw a minimap dot.
    private static void StripForAmbience(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        VFXAutoFade.HideFromMinimap(go);
    }

    private static float GroundAt(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 250f, Vector3.down, out RaycastHit hit, 700f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;

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
        return 0f;
    }
}
