using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TRAILER SHOT 2 — the cursed world.
//
// ==== WHAT THIS SHOT IS FOR ====
//
// Shot 1 said: the kingdom's authority shattered, and something dark came out of
// it. This one says exactly one thing after it — AND HERE IS WHAT IT DID TO THE
// LAND. Not "here is a big world", not "here is combat". The curse is not an
// event, it is a condition: it is everywhere, it has been here a long time, and
// things live in it that used to be people.
//
// ==== WHY THE FIRST VERSION DID NOT WORK ====
//
// It had four beats — PLACE, THREAT, HERO, OBJECTIVE — and that is the structure
// of a mission briefing. Here is where you are, here are the enemies, here is you,
// here is your goal. The camera pointed at each subject in turn and the audience
// was told things. Nothing was felt, because being shown a diagram is not an
// emotion.
//
// It also had the player in it. While the player is on screen the shot is about
// the player. The world beat is stronger if the player does not exist yet — then
// their arrival in the next shot means something.
//
// ==== HOW THIS ONE IS DIRECTED ====
//
// One principle: THE CAMERA DOES NOT PRESENT, IT PASSES.
//
// The audience's eye should find things BEFORE the camera reacts to them. A camera
// that swings confidently onto every prop tells you an operator chose them and the
// place was arranged for you. A camera that drifts past things, and pans lazily
// and too late, tells you the place was already full and you caught four of them.
// That is what `lookLag` is for — see AimDamping.
//
// The spatial arc is a single unbroken descent from above the canopy to the forest
// floor, which gives the emotional arc for free: distant and abstract becomes
// close and threatening, and the camera has one reason to keep moving the whole
// way rather than wandering.
//
//   A. THE LID (4.5s) — high over the canopy in fog, long lens, crawling forward.
//      Treetops break the fog like rocks in surf. NOTHING moves but the fog.
//      Says: the land is drowned in something.
//
//   B. THE DESCENT (5.0s) — the camera tips down and falls between the trees.
//      Light changes from pale fog above to green-black underworld below; branches
//      pass close to the lens, which is where the sense of scale comes from. Birds
//      break out of the canopy as it drops.
//      Says: there is a world under there, and it is worse.
//
//   C. THE INHABITANTS (6.0s) — ground level, gliding between trunks, looking
//      slightly UP so the dead loom. It passes a camp of skeletons standing at a
//      fire, and a patrol crossing without noticing. A trunk wipes the lens black
//      for a third of a second, and what is on the other side is closer.
//      Says: the place is inhabited, and not by anything that will help.
//
//   D. IT NOTICES (3.5s) — one figure turns its whole body to the lens. The camera,
//      which has moved for fifteen seconds, STOPS — after that long in motion a
//      stop is enormous. Push in. Hold. Out.
//      Says: this is not scenery. It is looking back.
//
// D is the part the old version had no equivalent of. A mood shot without a
// reversal is a screensaver; the reversal is what converts "look at this cursed
// place" into "this cursed place is looking at you", which is the handoff into
// whatever comes next.
//
// ==== WHY IT IS FILMED IN A LIVE REGION ====
//
// Because the catastrophic hangs all came from the other approach. Building a
// dedicated set meant ACTIVATING a Terrain mid-sequence, and activating a terrain
// makes Unity rebuild its tree and detail render data synchronously — a multi
// second stall that is indistinguishable from a crash, landing exactly on the cut,
// every single time. Here the terrain is already active and already generated.
// Nothing is activated, nothing is generated, nothing is loaded. The only things
// created are a handful of puppets and props, and they are created before the
// camera starts rolling.
[DisallowMultipleComponent]
public class RegionFlythrough : MonoBehaviour
{
    [Header("Trigger")]
    // F6, not F9. F9 is already QuickLoad (SaveSlotManager), which is why pressing
    // it restored a save and played the resource sound instead of starting
    // anything. F5, F8 and F10 are taken as well.
    [Tooltip("Press this during a region assault to play the shot. EDITOR ONLY — the key does not exist in a build.")]
    public KeyCode hotkey = KeyCode.F6;
    [Tooltip("Keys that abort it and hand control straight back.")]
    public KeyCode[] cancelKeys = { KeyCode.Escape, KeyCode.Space };

    [Header("Beat lengths (seconds)")]
    public float lid = 4.5f;
    public float descent = 5.0f;
    public float inhabitants = 6.0f;
    public float notices = 3.5f;

    [Header("Framing")]
    [Tooltip("Metres above the canopy the shot opens at.")]
    public float openAboveCanopy = 11f;
    [Tooltip("How far back along the travel line the opening sits.")]
    public float openBack = 58f;
    [Tooltip("Eye height for the ground glide. Below a standing figure's eyeline on purpose — looking slightly up makes the dead loom.")]
    public float glideHeight = 2.1f;
    [Tooltip("Never let the lens get closer than this to the ground, whatever the spline asks for.")]
    public float groundClearance = 1.3f;
    [Tooltip("Fallback canopy height if no trees can be measured.")]
    public float canopyFallback = 14f;

    [Header("Lens")]
    public float openFov = 32f;      // long: compresses the forest, makes it read as endless
    public float underFov = 58f;     // wide: trunks splay, the woods close in
    public float endFov = 44f;       // pushing in on the turn

    [Header("Feel")]
    [Tooltip("How fast the aim catches up once it HAS started moving.")]
    public float lookDamping = 2.4f;
    [Tooltip("Seconds the camera keeps looking at the old subject after a new beat begins. This is the whole 'it noticed late' effect — at 0 the camera reads as a machine that was told where to point.")]
    public float lookLag = 0.55f;
    [Tooltip("Handheld amplitude in metres. Tiny — this is a crane, not a shoulder.")]
    public float handheld = 0.05f;
    [Tooltip("Degrees of bank rolled in during the descent.")]
    public float descentRoll = 3.5f;

    [Header("Atmosphere")]
    public bool overrideAtmosphere = true;
    public Color fogColour = new Color(0.30f, 0.34f, 0.30f);
    public float fogDensity = 0.014f;
    [Range(0f, 1f)] public float sunDim = 0.35f;
    public Color sunTint = new Color(0.62f, 0.68f, 0.62f);

    [Header("Dressing")]
    public bool dressTheWoods = true;

    [Header("Diagnostics")]
    public bool logShotPlan = true;

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
        if (cam == null)
        {
            Debug.LogWarning("[Shot2] Cannot start: no camera tagged MainCamera.");
            return;
        }
        StartCoroutine(Run(cam));
    }

    // ========================= the shot =========================

    private IEnumerator Run(Camera cam)
    {
        IsPlaying = true;

        if (!FindDensestWoods(out Vector3 anchor, out Vector3 travel, out int sampleCount))
        {
            Debug.LogWarning("[Shot2] Could not find any woods to film. This shot needs a generated region " +
                             "with trees in it — run it during a region assault, after loading has finished.");
            IsPlaying = false;
            yield break;
        }

        float canopy = MeasureCanopy(anchor);
        var borrowed = new Borrowed();
        TakeOver(cam, borrowed);

        TrailerForestDressing dressing = dressTheWoods ? DressWoods(anchor, travel) : null;

        Vector3[] path = BuildPath(anchor, travel, canopy, out Vector3[] aims);

        if (logShotPlan)
        {
            Debug.Log($"[Shot2] Filming {sampleCount} trees around {anchor}, travelling {travel}, " +
                      $"canopy ~{canopy:F1}m. Total {TotalLength():F1}s. Press {hotkey}, Esc or Space to cancel.");
        }

        // Everything that is going to exist, exists. Two frames for the puppets'
        // animators to bind and for any instantiation hitch to land BEFORE the
        // camera starts rolling rather than inside the take.
        yield return null;
        yield return null;

        float total = TotalLength();
        float t = 0f;
        float seed = Random.Range(0f, 100f);
        int beat = -1;
        float beatChangedAt = 0f;
        Vector3 aim = aims[0];
        bool cancelled = false;
        bool birdsFlushed = false;
        bool sentryTurned = false;

        while (t < total)
        {
            if (WantsCancel()) { cancelled = true; break; }

            t += Time.unscaledDeltaTime;
            int nowBeat = BeatAt(t);
            if (nowBeat != beat) { beat = nowBeat; beatChangedAt = t; }

            // ---- position -------------------------------------------------
            float u = Mathf.Clamp01(t / total);
            // Smootherstep the whole traverse so the shot starts and stops without
            // a jerk even though the spline is evenly parameterised.
            float e = u * u * u * (u * (u * 6f - 15f) + 10f);

            Vector3 pos = Spline(path, e);

            // Beat D stops dead. After fifteen seconds of continuous motion a stop
            // is the single biggest gesture available, and it costs nothing.
            if (beat == 3)
            {
                float k = Mathf.Clamp01((t - (lid + descent + inhabitants)) / Mathf.Max(0.01f, notices * 0.35f));
                pos = Vector3.Lerp(pos, path[path.Length - 1], k * k);
            }

            pos.y = Mathf.Max(pos.y, GroundAt(pos) + groundClearance);

            // A crane has a pulse, not a tremor. Perlin, so it never repeats.
            pos += cam.transform.right * ((Mathf.PerlinNoise(seed + t * 0.7f, 0f) - 0.5f) * handheld * 2f)
                 + cam.transform.up * ((Mathf.PerlinNoise(0f, seed + t * 0.9f) - 0.5f) * handheld * 2f);

            cam.transform.position = pos;

            // ---- aim ------------------------------------------------------
            Vector3 want = aims[Mathf.Clamp(beat, 0, aims.Length - 1)];
            if (beat == 3 && dressing != null && dressing.Sentry != null)
                want = dressing.Sentry.transform.position + Vector3.up * 1.5f;

            aim = Vector3.Lerp(aim, want, 1f - Mathf.Exp(-AimDamping(t - beatChangedAt) * Time.unscaledDeltaTime));
            Vector3 toAim = aim - cam.transform.position;
            if (toAim.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(toAim.normalized);
                cam.transform.rotation = look * Quaternion.Euler(0f, 0f, RollAt(t));
            }

            cam.fieldOfView = FovAt(t);

            // ---- cues -----------------------------------------------------
            // Birds break out of the canopy as the camera drops through it. Called
            // rather than left to chance, because the one moment it has to happen
            // is this one.
            if (!birdsFlushed && t >= lid + 0.35f)
            {
                birdsFlushed = true;
                if (AmbientBirdLife.Instance != null)
                    AmbientBirdLife.Instance.FlushNear(anchor + travel * 8f, travel);
            }

            // The reversal, a beat into D so it lands while the camera is already
            // slowing rather than at the instant of the cut.
            if (!sentryTurned && t >= lid + descent + inhabitants + notices * 0.25f)
            {
                sentryTurned = true;
                if (dressing != null && dressing.Sentry != null)
                {
                    dressing.Sentry.TurnTo(cam.transform.position, 0.45f);
                    PlayOnce(AudioID.Enemy_Agro, dressing.Sentry.transform.position);
                }
            }

            yield return null;
        }

        if (dressing != null) { dressing.TearDown(); Destroy(dressing.gameObject); }
        HandBack(cam, borrowed);

        IsPlaying = false;
        Debug.Log(cancelled ? "[Shot2] Cancelled — control returned." : "[Shot2] Finished.");
    }

    private float TotalLength() => lid + descent + inhabitants + notices;

    private int BeatAt(float t)
    {
        if (t < lid) return 0;
        if (t < lid + descent) return 1;
        if (t < lid + descent + inhabitants) return 2;
        return 3;
    }

    // The late pan, and the reason the shot reads as observed rather than
    // presented. For the first `lookLag` seconds of a beat the aim barely
    // responds; then it catches up. The camera appears to notice the new subject
    // a moment after the audience already has.
    private float AimDamping(float sinceBeatChange)
    {
        float k = Mathf.Clamp01(sinceBeatChange / Mathf.Max(0.01f, lookLag));
        return lookDamping * Mathf.Lerp(0.12f, 1f, k * k);
    }

    private float FovAt(float t)
    {
        if (t < lid) return openFov;
        if (t < lid + descent)
            return Mathf.Lerp(openFov, underFov, Mathf.Clamp01((t - lid) / Mathf.Max(0.01f, descent)));
        if (t < lid + descent + inhabitants) return underFov;
        float k = Mathf.Clamp01((t - lid - descent - inhabitants) / Mathf.Max(0.01f, notices));
        return Mathf.Lerp(underFov, endFov, k * k);
    }

    // Bank rolls in over the descent and back out on the floor. Only there: a roll
    // held through a level glide reads as a broken horizon, not as flight.
    private float RollAt(float t)
    {
        if (t < lid || t > lid + descent + 1.2f) return 0f;
        float k = Mathf.Clamp01((t - lid) / Mathf.Max(0.01f, descent));
        return Mathf.Sin(k * Mathf.PI) * descentRoll;
    }

    private bool WantsCancel()
    {
        if (Input.GetKeyDown(hotkey)) return true;
        if (cancelKeys == null) return false;
        for (int i = 0; i < cancelKeys.Length; i++)
            if (Input.GetKeyDown(cancelKeys[i])) return true;
        return false;
    }

    // ========================= the path =========================

    private Vector3[] BuildPath(Vector3 anchor, Vector3 travel, float canopy, out Vector3[] aims)
    {
        Vector3 side = Vector3.Cross(Vector3.up, travel);

        // Every height is measured from the ground UNDER that waypoint, not from
        // the anchor's ground level. Over rolling terrain those differ by several
        // metres, and the difference is the shot either skimming the canopy or
        // flying through it.
        Vector3 p0 = anchor - travel * openBack;
        p0.y = GroundAt(p0) + canopy + openAboveCanopy;

        Vector3 p1 = anchor - travel * 22f;
        p1.y = GroundAt(p1) + canopy * 0.62f;

        Vector3 p2 = anchor + travel * 8f;
        p2.y = GroundAt(p2) + glideHeight + 0.6f;

        // Thread the glide past an actual trunk so one wipes the lens. Occlusion
        // is what creates depth on screen — far more than distance does — and a
        // frame that goes briefly black and comes back different is a free scare.
        Vector3 p3 = OcclusionPoint(anchor + travel * 28f, travel, side);
        p3.y = GroundAt(p3) + glideHeight;

        Vector3 p4 = anchor + travel * 52f;
        p4.y = GroundAt(p4) + glideHeight;

        Vector3 p5 = anchor + travel * 66f;
        p5.y = GroundAt(p5) + glideHeight - 0.15f;

        // A: down the forest, still high — the audience is reading terrain.
        Vector3 a0 = anchor + travel * 34f;
        a0.y = GroundAt(a0) + canopy * 0.35f;
        // B: into the dark under the canopy.
        Vector3 a1 = anchor + travel * 24f;
        a1.y = GroundAt(a1) + 2.2f;
        // C: ahead along the floor, biased a little toward the camp so the lazy
        //    pan drifts that way without ever committing to it.
        Vector3 a2 = anchor + travel * 74f + side * 5f;
        a2.y = GroundAt(a2) + 1.8f;
        // D: replaced at runtime by the sentry's live position.
        Vector3 a3 = anchor + travel * 80f;
        a3.y = GroundAt(a3) + 1.5f;

        aims = new[] { a0, a1, a2, a3 };

        return new[] { p0, p1, p2, p3, p4, p5 };
    }

    // Find a trunk near the glide line and return a point just beside it.
    private Vector3 OcclusionPoint(Vector3 near, Vector3 travel, Vector3 side)
    {
        Transform trees = FindTreesContainer();
        if (trees == null) return near;

        Transform best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < trees.childCount; i++)
        {
            Transform c = trees.GetChild(i);
            float d = (c.position - near).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        if (best == null || bestD > 400f) return near;   // nothing within 20 m

        // Pass on whichever side keeps the camera closer to its original line.
        Vector3 a = best.position + side * 1.35f;
        Vector3 b = best.position - side * 1.35f;
        return (a - near).sqrMagnitude <= (b - near).sqrMagnitude ? a : b;
    }

    // ========================= finding the woods =========================

    // Where the forest is thickest.
    //
    // Cursed husks are the first choice and not merely a convenience: they only
    // spawn in story regions, at just over half the trees, and they are the visual
    // subject of this whole shot. The thickest patch of CURSED wood is a better
    // place to film "the curse took this land" than the thickest patch of any wood.
    //
    // Live GameObjects, not terrainData.treeInstances — this project has
    // useTerrainTreePainting off, so trees are real objects under TreesContainer
    // and the terrain's instance array holds only bushes.
    private bool FindDensestWoods(out Vector3 anchor, out Vector3 travel, out int sampleCount)
    {
        anchor = Vector3.zero;
        travel = Vector3.forward;
        sampleCount = 0;

        var points = new List<Vector3>(2048);

        foreach (var ct in CursedTree.Active)
            if (ct != null) points.Add(ct.transform.position);

        bool usingCursed = points.Count >= 24;
        if (!usingCursed)
        {
            points.Clear();
            Transform trees = FindTreesContainer();
            if (trees != null)
                for (int i = 0; i < trees.childCount; i++) points.Add(trees.GetChild(i).position);
        }
        if (points.Count < 8) return false;

        // Bin into cells and take the hottest one that is not against the map edge
        // — a shot that flies off the end of the terrain has nothing to film.
        const float CELL = 38f;
        var bins = new Dictionary<(int, int), List<Vector3>>();
        foreach (var p in points)
        {
            var key = (Mathf.FloorToInt(p.x / CELL), Mathf.FloorToInt(p.z / CELL));
            if (!bins.TryGetValue(key, out var list)) bins[key] = list = new List<Vector3>(32);
            list.Add(p);
        }

        List<Vector3> bestCell = null;
        foreach (var kv in bins)
        {
            if (bestCell != null && kv.Value.Count <= bestCell.Count) continue;
            Vector3 centre = Centroid(kv.Value);
            if (TooCloseToEdge(centre, 110f)) continue;
            bestCell = kv.Value;
        }
        if (bestCell == null) return false;

        anchor = Centroid(bestCell);
        anchor.y = GroundAt(anchor);
        sampleCount = usingCursed ? bestCell.Count : points.Count;

        // Fly ALONG the forest, not out of it: score twelve headings by how much
        // wood sits in a corridor ahead, and take the best.
        float bestScore = -1f;
        for (int a = 0; a < 12; a++)
        {
            Vector3 dir = Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward;
            if (TooCloseToEdge(anchor + dir * 95f, 40f)) continue;

            float score = 0f;
            foreach (var p in points)
            {
                Vector3 rel = p - anchor;
                rel.y = 0f;
                float along = Vector3.Dot(rel, dir);
                if (along < 5f || along > 95f) continue;
                float across = Mathf.Abs(Vector3.Dot(rel, Vector3.Cross(Vector3.up, dir)));
                if (across > 20f) continue;
                score += 1f;
            }
            if (score > bestScore) { bestScore = score; travel = dir; }
        }

        return bestScore >= 0f;
    }

    private static Vector3 Centroid(List<Vector3> pts)
    {
        Vector3 s = Vector3.zero;
        for (int i = 0; i < pts.Count; i++) s += pts[i];
        return pts.Count > 0 ? s / pts.Count : Vector3.zero;
    }

    private static bool TooCloseToEdge(Vector3 p, float margin)
    {
        Terrain[] all = Terrain.activeTerrains;
        if (all == null || all.Length == 0) return false;
        foreach (var t in all)
        {
            if (t == null || t.terrainData == null) continue;
            Vector3 o = t.transform.position;
            Vector3 s = t.terrainData.size;
            if (p.x < o.x || p.x > o.x + s.x || p.z < o.z || p.z > o.z + s.z) continue;
            return p.x - o.x < margin || o.x + s.x - p.x < margin
                || p.z - o.z < margin || o.z + s.z - p.z < margin;
        }
        return true;   // outside every terrain
    }

    private static Transform FindTreesContainer()
    {
        var go = GameObject.Find("TreesContainer");
        return go != null ? go.transform : null;
    }

    // Tallest tree near the anchor, so the opening clears the canopy rather than
    // starting inside it.
    private float MeasureCanopy(Vector3 anchor)
    {
        Transform trees = FindTreesContainer();
        if (trees == null) return canopyFallback;

        float ground = GroundAt(anchor);
        float tallest = 0f;
        int checkedCount = 0;
        for (int i = 0; i < trees.childCount && checkedCount < 60; i++)
        {
            Transform c = trees.GetChild(i);
            if ((c.position - anchor).sqrMagnitude > 90f * 90f) continue;
            checkedCount++;

            var rends = c.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                float h = r.bounds.max.y - ground;
                if (h > tallest) tallest = h;
            }
        }
        return tallest > 3f ? tallest : canopyFallback;
    }

    // ========================= dressing =========================

    private TrailerForestDressing DressWoods(Vector3 anchor, Vector3 travel)
    {
        // Read the prefabs off the region's own encounter director, so the shot is
        // dressed with exactly what the game uses. A second set of references here
        // would be a second thing to keep in step with it.
        GameObject[] enemies = null;
        GameObject campfire = null;
        var director = FindFirstObjectByType<WorldEncounterDirector>(FindObjectsInactive.Include);
        if (director != null)
        {
            enemies = director.enemyPrefabs;
            campfire = director.campfirePrefab;
        }

        var go = new GameObject("[Shot2 Dressing]");
        var dressing = go.AddComponent<TrailerForestDressing>();
        dressing.Build(anchor, travel, enemies, campfire, LoadRemains());
        return dressing;
    }

    // Bones and wreckage. Editor-only asset loading, which is legitimate because
    // the whole shot is an editor capture tool — and it means no prefab references
    // have to be wired onto a component that is created from nothing at runtime.
    private static GameObject[] LoadRemains()
    {
#if UNITY_EDITOR
        string[] paths =
        {
            "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Skull.prefab",
            "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/SkullBones.prefab",
            "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Horse_Cart_Used_02.prefab",
        };
        var found = new List<GameObject>(paths.Length);
        foreach (var p in paths)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) found.Add(go);
        }
        // Missing props are not worth failing over: the fire, the skeletons, the
        // cursed trees and the fog carry the shot on their own.
        if (found.Count == 0)
            Debug.Log("[Shot2] No remains prefabs found — dressing the woods without them.");
        return found.ToArray();
#else
        return null;
#endif
    }

    // ========================= borrow and hand back =========================

    // Everything the shot takes, remembered before it takes it. Restoring from a
    // snapshot is the only way an interrupted shot can hand back exactly what it
    // borrowed — and a cinematic that leaves the player stuck in a camera, blind,
    // or standing in permanent fog is worse than no cinematic.
    private class Borrowed
    {
        public Vector3 camPos;
        public Quaternion camRot;
        public float camFov;
        public int camMask;
        public bool camFollowCinematic;
        public CameraFollow camFollow;

        public PlayerController player;
        public bool playerBlocked;
        public readonly List<Renderer> hiddenPlayer = new List<Renderer>();

        public bool enemyFreeze;
        public bool enemyVocals;
        public readonly List<EnemyAI> silenced = new List<EnemyAI>();

        public DayNightCycle dnc;
        public bool dncWasEnabled;
        public bool fog;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity, fogStart, fogEnd;
        public Color ambient;
        public Light sun;
        public float sunIntensity;
        public Color sunColor;

        public int damagePopups;
        public bool cinematicLatch;
        public float throttle;
    }

    private void TakeOver(Camera cam, Borrowed b)
    {
        b.camPos = cam.transform.position;
        b.camRot = cam.transform.rotation;
        b.camFov = cam.fieldOfView;
        b.camMask = cam.cullingMask;
        b.camFollow = cam.GetComponent<CameraFollow>();
        if (b.camFollow != null)
        {
            b.camFollowCinematic = b.camFollow.isCinematicMode;
            b.camFollow.isCinematicMode = true;
        }

        // Minimap markers are a render layer, so the cheapest and most complete way
        // to remove every one of them at once is to stop the camera drawing that
        // layer at all.
        int minimap = LayerMask.NameToLayer("MinimapOnly");
        if (minimap >= 0) cam.cullingMask &= ~(1 << minimap);

        b.cinematicLatch = RegionManager.CinematicActive;
        RegionManager.CinematicActive = true;   // suppresses pause and level-up menus

        b.player = FindFirstObjectByType<PlayerController>();
        if (b.player != null)
        {
            b.playerBlocked = b.player.isControlBlocked;
            b.player.isControlBlocked = true;
            b.player.isCinematicInvincible = true;
            // The player is not in this shot. Hiding the model rather than moving
            // it means nothing about their actual position or state changes.
            foreach (var r in b.player.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                r.enabled = false;
                b.hiddenPlayer.Add(r);
            }
        }

        // The region's own enemies are frozen and hidden. Live AI wandering through
        // a composed frame is what made every earlier attempt look like a bug reel.
        b.enemyFreeze = EnemyAI.GlobalFreeze;
        b.enemyVocals = EnemyAI.SuppressCombatVocals;
        EnemyAI.GlobalFreeze = true;
        EnemyAI.SuppressCombatVocals = true;
        foreach (var e in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (e == null) continue;
            e.suppressWorldHealthBar = true;
            if (e.healthCanvas != null) e.healthCanvas.SetActive(false);
            b.silenced.Add(e);
        }

        b.damagePopups = PlayerPrefs.GetInt("Settings_DamagePopups", 1);
        PlayerPrefs.SetInt("Settings_DamagePopups", 0);

        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.HideLevelObjective();
            GlobalHUD.Instance.HidePrompt();
            GlobalHUD.Instance.SetGameplayPanelsActive(false);
            GlobalHUD.Instance.SetCinematicDoF(true);
        }

        if (overrideAtmosphere) TakeAtmosphere(b);

        // Stop the radial spawner adding anyone mid-take.
        //
        // Deliberately NOT TrailerSceneSanity.ClearTheField, which is built for a
        // dedicated trailer scene: it disables WorldGenerator, RegionTotem,
        // WorldEncounterDirector and the rest and never turns them back on. In a
        // dedicated scene that is correct. Here it would end the shot by leaving
        // the player in a live region whose totems no longer respond, which is a
        // far worse bug than an extra skeleton wandering into frame.
        b.throttle = EnemySpawner.AmbientThrottle;
        EnemySpawner.AmbientThrottle = 0f;
    }

    private void TakeAtmosphere(Borrowed b)
    {
        // DayNightCycle rewrites RenderSettings.fogColor EVERY FRAME from its own
        // gradients, so setting fog while it runs does nothing at all. Switching
        // the component off for the take is the only way the override holds.
        b.dnc = FindFirstObjectByType<DayNightCycle>();
        if (b.dnc != null)
        {
            b.dncWasEnabled = b.dnc.enabled;
            b.dnc.enabled = false;
        }

        b.fog = RenderSettings.fog;
        b.fogMode = RenderSettings.fogMode;
        b.fogColor = RenderSettings.fogColor;
        b.fogDensity = RenderSettings.fogDensity;
        b.fogStart = RenderSettings.fogStartDistance;
        b.fogEnd = RenderSettings.fogEndDistance;
        b.ambient = RenderSettings.ambientLight;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColour;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientLight = fogColour * 0.55f;

        b.sun = RenderSettings.sun != null ? RenderSettings.sun : FindSun();
        if (b.sun != null)
        {
            b.sunIntensity = b.sun.intensity;
            b.sunColor = b.sun.color;
            b.sun.intensity *= sunDim;
            b.sun.color = sunTint;
        }
    }

    private void HandBack(Camera cam, Borrowed b)
    {
        cam.fieldOfView = b.camFov;
        cam.transform.position = b.camPos;
        cam.transform.rotation = b.camRot;
        cam.cullingMask = b.camMask;
        if (b.camFollow != null) b.camFollow.isCinematicMode = b.camFollowCinematic;

        if (b.player != null)
        {
            b.player.isControlBlocked = b.playerBlocked;
            b.player.isCinematicInvincible = false;
        }
        foreach (var r in b.hiddenPlayer) if (r != null) r.enabled = true;

        EnemyAI.GlobalFreeze = b.enemyFreeze;
        EnemyAI.SuppressCombatVocals = b.enemyVocals;
        foreach (var e in b.silenced) if (e != null) e.suppressWorldHealthBar = false;

        PlayerPrefs.SetInt("Settings_DamagePopups", b.damagePopups);
        RegionManager.CinematicActive = b.cinematicLatch;
        EnemySpawner.AmbientThrottle = b.throttle;

        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.SetGameplayPanelsActive(true);
            GlobalHUD.Instance.SetCinematicDoF(false);
        }

        if (b.dnc != null) b.dnc.enabled = b.dncWasEnabled;
        if (overrideAtmosphere)
        {
            RenderSettings.fog = b.fog;
            RenderSettings.fogMode = b.fogMode;
            RenderSettings.fogColor = b.fogColor;
            RenderSettings.fogDensity = b.fogDensity;
            RenderSettings.fogStartDistance = b.fogStart;
            RenderSettings.fogEndDistance = b.fogEnd;
            RenderSettings.ambientLight = b.ambient;
            if (b.sun != null) { b.sun.intensity = b.sunIntensity; b.sun.color = b.sunColor; }
        }
    }

    private static Light FindSun()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Directional && l.enabled) return l;
        return null;
    }

    private static void PlayOnce(string id, Vector3 at)
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(id)) return;
        if (!AudioManager.Instance.HasEvent(id)) return;
        AudioManager.Instance.PlaySFX3D(id, at);
    }

    // ========================= maths =========================

    // Catmull-Rom through the waypoints. A camera that changes direction sharply at
    // a waypoint reads as a machine following instructions; a spline reads as an
    // operator who knew where they were going.
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
