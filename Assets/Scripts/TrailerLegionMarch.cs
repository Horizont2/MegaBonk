using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trailer, shot 2 — the legion on the march.
//
// ==== WHAT MAKES AN ARMY FRIGHTENING ====
//
// Not the number of models. Three things, none of which is "more skeletons":
//
//  1. NO VISIBLE END. An army you can count has a size, and a size is something
//     you could survive. The column here is a RECYCLING BELT: ranks that pass
//     behind the camera are moved back to the horizon, so a fixed budget of a
//     hundred-odd units reads as a march with no beginning and no end. It also
//     means the shot can run for as long as it needs to without costing more.
//
//  2. SOMETHING HUMAN-SIZED IN FRAME. A column alone has no scale — it could be
//     toys. The shot puts the camera down IN THE GRASS first, at boot height, so
//     the first thing the audience measures the army against is the ground they
//     are standing on.
//
//  3. INDIFFERENCE. They do not look at the camera, break step, or react. The
//     most unsettling thing an army can do is not notice you.
//
// ==== WHY THE CAMERA MOVES THE WAY IT DOES ====
//
// Fear and grandeur are opposite framings and cannot be done at once, so they are
// done in order:
//
//   FEAR is low and close and wide. From the grass, on a wide lens, the ranks
//   tower and the world is bigger than the viewer.
//
//   GRANDEUR is high and far and LONG. The lens goes telephoto as the camera
//   cranes up, and that is the whole trick: a long lens compresses depth, so the
//   ranks stack into each other and the column reads as denser and longer than it
//   is. The same army on a wide lens at the same height would look sparse.
//
// And the move is machine-steady throughout — no handheld. Shot 1 shook because a
// person was frightened; this one does not, because nothing here is frightened.
// The army is inevitable, and the camera agrees with it.
[DisallowMultipleComponent]
public class TrailerLegionMarch : MonoBehaviour
{
    [Header("Sequencing")]
    [Tooltip("OFF when this shot is chained after another — the sequencer starts it on cue instead of it firing the moment its rig switches on.")]
    public bool autoPlay = true;

    // Read by TrailerShotChain to know when to move on. A shot that cannot say
    // when it is done can only be followed by a guessed delay, and a guessed
    // delay drifts the moment any beat is retuned.
    public bool IsFinished { get; private set; }

    // Set once the column exists. The sequencer builds this shot while the
    // PREVIOUS one is still playing, so the hand-over has nothing left to wait
    // for. Building it after the cut meant several seconds of black screen —
    // technically hidden, which is not the same as not being there.
    public bool IsReady { get; private set; }
    private bool _preparing;
    // The column exists during the PREVIOUS shot, so it must not march, animate or
    // sample ground until this shot is actually on screen.
    private bool _playing;

    [Header("Who marches")]
    public GameObject[] rankPrefabs;
    public GameObject[] bossPrefabs;

    [Header("Formation")]
    [Tooltip("Ranks kept alive at once. Because ranks recycle, this is a BUDGET, not the length of the army — raise it only if the tail is visibly short before the fog takes it.")]
    // Deliberately small to start with.
    //
    // The environment, the camera move and the formation's shape all need to be
    // right before the count matters, and a heavy scene makes every one of those
    // slower to judge. Get the shot working at 32 units, then raise these — the
    // recycling belt means the column reads as endless at any count.
    [Range(4, 60)] public int ranksAlive = 8;
    [Range(2, 24)] public int unitsPerRank = 4;
    public float rankSpacing = 3.2f;
    public float fileSpacing = 2.1f;
    [Tooltip("A boss walks in place of the centre of every Nth rank, and the rank opens up around it.")]
    [Range(2, 12)] public int bossEveryNRanks = 5;

    [Header("March")]
    public float marchSpeed = 2.1f;
    [Tooltip("Direction of travel in world space. The camera rig is built around this.")]
    public Vector3 marchDirection = Vector3.forward;
    [Tooltip("Metres of sideways wander per unit. Tiny — a formation is meant to look drilled, just not stamped from a die.")]
    public float rankJitter = 0.35f;
    [Tooltip("Height of the walking bob. Too much and they bounce like toys.")]
    public float gaitBob = 0.09f;

    [Header("Performance")]
    [Tooltip("Beyond this distance a unit's Animator is switched off. At range nobody can tell, and Animators are the entire cost of a crowd.")]
    public float animateWithinDistance = 38f;
    [Tooltip("Ground is sampled this many times a second per unit, not every frame.")]
    public float groundSampleRate = 6f;

    [Header("Shot timing (seconds)")]
    [Tooltip("Beat A — on the ground at boot height, aimed at their LEGS. We do not get a face yet.")]
    public float bootsBeat = 2.6f;
    [Tooltip("Beat B — the tilt up. The audience is shown what has been walking past them.")]
    public float tiltBeat = 1.8f;
    [Tooltip("Beat C — the rise. One unbroken crane, no cut.")]
    public float riseBeat = 3.8f;
    [Tooltip("Beat D — high, long lens, held on a column with no end.")]
    public float holdBeat = 3.4f;
    public float outFade = 1.1f;

    [Header("Camera")]
    public Camera shotCamera;
    [Tooltip("Clearance from the EDGE of the column, not from its axis — the rig works this out from the formation's width, so widening the ranks can never put the lens inside them.")]
    public float flankClearance = 3.5f;
    public float lowHeight = 0.55f;
    public float highHeight = 26f;
    [Tooltip("Wide lens for the low shot: it makes the ranks tower.")]
    public float lowFov = 58f;
    [Tooltip("Long lens for the reveal. This compression is what makes the column look endless.")]
    public float highFov = 26f;
    [Tooltip("How far the camera drifts AHEAD of the oncoming column as it rises, so more of the line fits in frame.")]
    public float craneBack = 34f;

    [Header("Foreground")]
    [Tooltip("Props planted right in front of the lens for the ranks to pass BEHIND. Nothing states depth as cheaply or as strongly as something the subject occludes.")]
    public GameObject[] foregroundProps;
    [Range(0, 6)] public int foregroundCount = 3;

    [Header("Atmosphere")]
    public Color dustColor = new Color(0.46f, 0.43f, 0.40f, 0.30f);
    [Tooltip("Ash and dust kicked up by the column. A clean-footed army looks weightless.")]
    public bool spawnMarchDust = true;

    [Header("Audio")]
    public string marchSound = AudioID.Trailer_MarchLoop;
    public string hornSound = AudioID.Trailer_WarHorn;
    public string boneSound = AudioID.Trailer_BoneRattle;
    public string windSound = AudioID.Trailer_WindDesolate;
    public string crowSound = AudioID.Trailer_Crows;
    public string bossStepSound = AudioID.Trailer_BossStep;
    [Tooltip("Seconds between boss footfalls. A slower, heavier tread than the ranks is what gives the big ones weight.")]
    public float bossStepInterval = 1.35f;

    // ---- runtime ----
    private class Unit
    {
        public Transform t;
        public Animator anim;
        public float phase;       // gait offset, so the crowd is not in lockstep
        public float lateral;     // fixed wander within the file
        public float groundY;
        public float nextGroundAt;
        public bool animating = true;
        public bool isBoss;
    }

    private readonly List<Unit> units = new List<Unit>(256);
    private Vector3 dir, right;
    private float columnLength;
    // Metres past the lens a rank travels before it is sent back to the tail.
    // Only has to clear the frame.
    private const float recycleBehindCamera = 22f;
    private Transform camT;
    private ParticleSystem dust;
    // One stripped copy of each prefab, made once and cloned for every unit.
    // Walking a deep prefab for components and destroying them is the expensive
    // part of building a crowd; doing it per unit meant paying it 270 times
    // instead of four.
    private readonly Dictionary<GameObject, GameObject> templates = new Dictionary<GameObject, GameObject>();
    private Transform templateRoot;
    private float shotTime;

    private bool _initialised;

    // Called from Start AND from Prepare, because the sequencer prepares this shot
    // the moment its rig is switched on — which is BEFORE Unity runs Start. Built
    // without this, the column used a zero march direction: every position landed
    // on top of the last one and LookRotation threw on a zero vector, once per
    // unit. That is the black screen and the console flood in one.
    private bool EnsureInit()
    {
        if (_initialised) return true;

        if (rankPrefabs == null || rankPrefabs.Length == 0)
        { Debug.LogWarning("[Legion] No rank prefabs assigned."); enabled = false; return false; }

        shotCamera = shotCamera != null ? shotCamera : Camera.main;
        if (shotCamera == null) { Debug.LogWarning("[Legion] No camera."); enabled = false; return false; }
        camT = shotCamera.transform;

        dir = marchDirection.sqrMagnitude < 0.001f ? Vector3.forward : marchDirection.normalized;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
        dir.Normalize();
        right = Vector3.Cross(Vector3.up, dir).normalized;

        columnLength = ranksAlive * rankSpacing;
        _initialised = true;
        return true;
    }

    private void Start()
    {
        if (!EnsureInit()) return;

        PurgeForeignEnemies();
        // Spawners can start again and managers can re-create themselves, so this
        // is not a one-time clean-up. Two seconds is plenty; the scan is not free.
        InvokeRepeating(nameof(PurgeForeignEnemies), 2f, 2f);
        if (spawnMarchDust) BuildDust();
        PlantForeground();
        if (autoPlay) Play();
        else Prepare();      // chained: get the column ready while shot 1 plays
    }

    // Anything with a live EnemyAI that is not part of this column is not part of
    // this shot.
    //
    // Doing this in the editor at setup time was not enough: the scene is not
    // necessarily saved, managers re-create themselves, and a spawner switched off
    // in edit mode can be switched back on by whatever owns it. The skeletons
    // scattering across the frame with health bars over their heads were never the
    // column — they were the game running underneath it. So it is enforced at
    // runtime, and repeatedly, because a spawner that fires once will fire again.
    private void PurgeForeignEnemies()
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mb == null || !mb.enabled) continue;
            switch (mb.GetType().Name)
            {
                case "EnemySpawner":
                case "WorldEncounterDirector":
                case "RegionAlertDirector":
                case "EnemyEncounterGroup":
                case "RegionTotem":
                    mb.enabled = false;
                    break;
            }
        }

        foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (ai == null) continue;
            // Column units had their EnemyAI destroyed at spawn, so anything still
            // carrying one came from somewhere else.
            if (ai.transform.IsChildOf(transform)) continue;
            Destroy(ai.gameObject);
        }
    }

    // ======================= the column =======================

    // Built across frames, not in one.
    //
    // 270 prefabs instantiated in a single frame — each walked for components,
    // stripped, and given an Animator evaluation to desynchronise its gait — is
    // several seconds of main-thread work. That is not a hitch, it is a freeze,
    // and from outside it is indistinguishable from the engine hanging. It also
    // happens while the screen is still black at the head of the shot, so
    // spreading it out costs nothing anybody can see.
    private IEnumerator BuildColumnRoutine()
    {
        const int RanksPerFrame = 2;

        for (int r = 0; r < ranksAlive; r++)
        {
            if (r > 0 && r % RanksPerFrame == 0) yield return null;

            bool bossRank = bossPrefabs != null && bossPrefabs.Length > 0 && (r % bossEveryNRanks == 0);
            float z = -r * rankSpacing;

            for (int i = 0; i < unitsPerRank; i++)
            {
                // Centre the file around the column's axis.
                float lane = (i - (unitsPerRank - 1) * 0.5f) * fileSpacing;

                bool isBoss = false;
                GameObject prefab;
                if (bossRank && i == unitsPerRank / 2)
                {
                    prefab = bossPrefabs[Random.Range(0, bossPrefabs.Length)];
                    isBoss = true;
                }
                else
                {
                    prefab = rankPrefabs[Random.Range(0, rankPrefabs.Length)];
                }
                if (prefab == null) continue;

                // The rank OPENS AROUND a boss rather than overlapping it. Ranks
                // that part are what tells the audience the big one outranks the
                // rest without a single line of dialogue.
                if (bossRank && !isBoss)
                {
                    float side = Mathf.Sign(lane == 0f ? 1f : lane);
                    lane += side * fileSpacing * 0.9f;
                }

                Vector3 pos = transform.position + dir * z + right * lane;
                var go = Instantiate(TemplateFor(prefab), pos, Quaternion.LookRotation(dir), transform);
                go.SetActive(true);
                units.Add(MakeUnit(go, isBoss));
            }
        }

        ReportFormation();
    }

    // If the column ever comes out as a heap again, this says so in one line
    // instead of requiring a scene to be picked apart by hand.
    private void ReportFormation()
    {
        if (units.Count == 0) { Debug.LogWarning("[Legion] No units were built."); return; }
        Bounds b = new Bounds(units[0].t.position, Vector3.zero);
        for (int i = 1; i < units.Count; i++) b.Encapsulate(units[i].t.position);
        Debug.Log($"[Legion] {units.Count} units over {b.size.x:F1} m wide x {b.size.z:F1} m deep " +
                  $"(expected ~{(unitsPerRank - 1) * fileSpacing:F1} x {columnLength:F1}). March dir {dir}.");
    }

    // A prefab with everything that could move, tick, spawn or draw UI already
    // removed. Kept inactive so nothing on it ever runs; clones of it are the
    // marching units.
    private GameObject TemplateFor(GameObject prefab)
    {
        if (templates.TryGetValue(prefab, out GameObject cached) && cached != null) return cached;

        if (templateRoot == null)
        {
            var holder = new GameObject("Templates");
            holder.transform.SetParent(transform, false);
            holder.SetActive(false);          // nothing inside ever wakes up
            templateRoot = holder.transform;
        }

        GameObject t = Instantiate(prefab, templateRoot);
        Strip(t);
        templates[prefab] = t;
        return t;
    }

    // Strip EVERY behaviour except the Animator.
    //
    // Disabling the components I happened to think of is not good enough: a
    // prefab this deep carries AI, health, loot, audio, VFX and UI drivers, and
    // any one left running will move a unit, re-enable a health bar or spawn
    // something. This crowd exists to be POSED BY THIS SCRIPT and nothing else,
    // so the rule is a whitelist.
    // DestroyImmediate, not Destroy, and deliberately so: Destroy is deferred to
    // the end of the frame, and the template is cloned in the SAME frame it is
    // built — so every unit would inherit the components that were merely queued
    // for removal. This is the one case where the immediate form is the correct
    // one rather than the lazy one.
    private static void Strip(GameObject go)
    {
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb is Animator) continue;
            mb.enabled = false;
            DestroyImmediate(mb);
        }

        // World-space UI: health bars and the like. Destroying the object rather
        // than disabling the Canvas, so nothing can switch it back on.
        foreach (var cv in go.GetComponentsInChildren<Canvas>(true))
            if (cv != null) DestroyImmediate(cv.gameObject);

        // Minimap markers. They live on the MinimapOnly layer and are invisible to
        // the gameplay camera, but the trailer camera has no culling mask set, so
        // it renders them — which is why coloured pips floated over the ranks.
        int minimapLayer = LayerMask.NameToLayer("MinimapOnly");
        if (minimapLayer >= 0)
        {
            var marked = new List<GameObject>();
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
                if (tr != null && tr.gameObject.layer == minimapLayer) marked.Add(tr.gameObject);
            foreach (var m in marked) if (m != null) DestroyImmediate(m);
        }

        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var cc in go.GetComponentsInChildren<CharacterController>(true)) cc.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; rb.useGravity = false; }
    }

    private Unit MakeUnit(GameObject go, bool isBoss)
    {
        var u = new Unit
        {
            t = go.transform,
            anim = go.GetComponentInChildren<Animator>(),
            phase = Random.Range(0f, 100f),
            lateral = Random.Range(-rankJitter, rankJitter),
            isBoss = isBoss,
            nextGroundAt = Random.Range(0f, 1f / Mathf.Max(1f, groundSampleRate)),
        };

        if (u.anim != null)
        {
            u.anim.cullingMode = AnimatorCullingMode.CullCompletely;
            u.anim.applyRootMotion = false;
            u.anim.SetBoolSafe("isMoving", true);
            // Desynchronise the walk cycle. A crowd in perfect lockstep reads as
            // one object copied, which is the clearest tell of a fake army.
            //
            // Done by jumping to a random point in the current state, NOT by
            // Animator.Update(): that forces a full evaluation of over a second of
            // animation, per unit, and 270 of those in a build is most of why the
            // shot appeared to hang.
            u.anim.Play(0, 0, Random.value);
            u.anim.speed = Random.Range(0.94f, 1.06f);
        }

        // Bigger than 'a bit'. At the distances this shot works at, a boss only
        // reads as outranking the ranks if it is unmistakably taller than them.
        if (isBoss) go.transform.localScale *= 1.7f;
        return u;
    }

    private void Update()
    {
        if (!_playing) return;

        float dt = Time.unscaledDeltaTime;
        float step = marchSpeed * dt;
        Vector3 camPos = camT != null ? camT.position : transform.position;
        float animSqr = animateWithinDistance * animateWithinDistance;
        float now = Time.unscaledTime;

        for (int i = 0; i < units.Count; i++)
        {
            Unit u = units[i];
            if (u.t == null) continue;

            Vector3 p = u.t.position + dir * step;

            // Recycle against the CAMERA, not the column's origin. Tied to the
            // origin, ranks vanish at a fixed world position — which is on screen
            // as soon as the camera moves, and a rank popping out of existence
            // mid-frame ends the illusion instantly. Measured from the lens, the
            // swap is always safely behind it.
            if (Vector3.Dot(p - camPos, dir) > recycleBehindCamera) p -= dir * columnLength;

            // Gait: a small vertical bob and a slow lateral sway, each on its own
            // phase. Without it the formation slides like a decal.
            float g = (now + u.phase) * (u.isBoss ? 3.1f : 4.4f);
            float bob = Mathf.Abs(Mathf.Sin(g)) * (u.isBoss ? gaitBob * 1.6f : gaitBob);
            float sway = Mathf.Sin(g * 0.5f) * 0.06f;

            if (now >= u.nextGroundAt)
            {
                u.nextGroundAt = now + 1f / Mathf.Max(1f, groundSampleRate);
                u.groundY = SampleGround(p);
            }

            u.t.position = new Vector3(p.x + right.x * (u.lateral + sway),
                                       u.groundY + bob,
                                       p.z + right.z * (u.lateral + sway));
            // A heavy, slightly rolling tread rather than a rigid facing.
            u.t.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, Mathf.Sin(g * 0.5f) * 2.5f, Mathf.Sin(g) * 1.4f);

            // Animators are the entire cost of a crowd, and past a certain range
            // nobody can tell a walk cycle from a pose.
            if (u.anim != null)
            {
                bool want = (u.t.position - camPos).sqrMagnitude < animSqr;
                if (want != u.animating) { u.anim.enabled = want; u.animating = want; }
            }
        }

        if (dust != null)
        {
            dust.transform.position = transform.position;
            var em = dust.emission;
            em.rateOverTime = 55f;
        }
    }

    private float SampleGround(Vector3 p)
    {
        Terrain[] all = Terrain.activeTerrains;
        if (all != null)
        {
            for (int i = 0; i < all.Length; i++)
            {
                Terrain t = all[i];
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position;
                Vector3 sz = t.terrainData.size;
                if (p.x >= o.x && p.x <= o.x + sz.x && p.z >= o.z && p.z <= o.z + sz.z)
                    return t.SampleHeight(p) + o.y;
            }
        }
        return transform.position.y;
    }

    private void BuildDust()
    {
        var go = new GameObject("MarchDust");
        go.transform.SetParent(transform, false);
        dust = go.AddComponent<ParticleSystem>();

        var main = dust.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 6f;
        main.startSpeed = 0.5f;
        main.startSize = 3.5f;
        main.startColor = dustColor;
        main.gravityModifier = -0.01f;
        main.maxParticles = 400;
        main.useUnscaledTime = true;

        var sh = dust.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        // A slab hugging the column: dust belongs where the feet are, low and wide.
        sh.scale = new Vector3(unitsPerRank * fileSpacing * 1.4f, 1.2f, columnLength);

        var em = dust.emission;
        em.rateOverTime = 0f;

        var sol = dust.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(1f, 1.6f)));

        var col = dust.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var pr = dust.GetComponent<ParticleSystemRenderer>();
        Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psh == null) psh = Shader.Find("Sprites/Default");
        if (psh != null)
        {
            var mat = new Material(psh);
            // A particle material with no texture draws hard-edged quads — the
            // reason effects look like flying blocks.
            var tex = TrailerSoftSprite.Get();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            // A URP particle material defaults to OPAQUE. With a soft sprite in an
            // opaque material the alpha is simply ignored and every particle draws
            // as a flat grey card — the "grey squares near the enemies".
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            pr.material = mat;
        }
        else pr.enabled = false;

        dust.Play();
    }

    // Something between the lens and the army.
    //
    // Depth on screen is not created by distance, it is created by OCCLUSION.
    // Ranks passing behind a near object are read as being further away than
    // ranks in clear air at the same distance, and it costs one prop. Placed on
    // the camera's side of the column and low, so the boots beat has something
    // right against the lens and the crane leaves it behind as it climbs.
    private void PlantForeground()
    {
        if (foregroundProps == null || foregroundProps.Length == 0 || foregroundCount <= 0) return;

        float halfWidth = (unitsPerRank - 1) * 0.5f * fileSpacing;
        float side = halfWidth + flankClearance;

        for (int i = 0; i < foregroundCount; i++)
        {
            GameObject prefab = foregroundProps[Random.Range(0, foregroundProps.Length)];
            if (prefab == null) continue;

            // Just inside the camera's stand-off, spread down the line so the
            // crane passes them rather than clearing them all at once.
            Vector3 p = transform.position
                      + right * (side * Random.Range(0.55f, 0.85f))
                      + dir * Random.Range(-6f, 14f);
            p.y = SampleGround(p);

            var go = Instantiate(prefab, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            go.transform.localScale *= Random.Range(0.9f, 1.4f);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
    }

    // ======================= camera =======================

    public void Play()
    {
        if (IsFinished) return;
        if (!EnsureInit()) { IsFinished = true; return; }
        StartCoroutine(PlayShot());
    }

    // Build the column without playing the shot. Safe to call more than once.
    public void Prepare()
    {
        if (IsReady || _preparing) return;
        if (!EnsureInit()) return;
        _preparing = true;
        StartCoroutine(PrepareRoutine());
    }

    private IEnumerator PrepareRoutine()
    {
        TrailerLogGuard.Arm();
        float t0 = Time.realtimeSinceStartup;
        yield return BuildColumnRoutine();
        IsReady = true;
        Debug.Log($"[Legion] Column of {units.Count} built in {Time.realtimeSinceStartup - t0:F2}s.");
    }

    private IEnumerator PlayShot()
    {
        TrailerLogGuard.Arm();

        // A stray zero or slow timeScale left by the previous shot would stop
        // anything here that is not on unscaled time.
        if (Time.timeScale < 0.99f) Time.timeScale = 1f;

        // Normally the sequencer had this built during the previous shot and this
        // returns immediately. Standalone, it builds here.
        if (!IsReady) { Prepare(); while (!IsReady) yield return null; }

        // Frame the opening BEFORE the fade lifts, or the first visible frame is
        // whatever the camera happened to be pointing at.
        _playing = true;
        UpdateCamera(0f);

        var polish = TrailerCinematicPolish.GetOrCreate();
        polish.OpenTrailer();
        TrailerAudio.SilenceStaleBeds();

        // Layered, because one massed-footsteps file on its own sounds like rain.
        // Bone over the tread is what makes it skeletons; wind under both is what
        // makes it a landscape rather than a sound effect.
        if (AudioManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(windSound)) AudioManager.Instance.PlaySFX3D(windSound, transform.position);
            if (!string.IsNullOrEmpty(crowSound)) AudioManager.Instance.PlaySFX3D(crowSound, transform.position);
            if (!string.IsNullOrEmpty(hornSound)) AudioManager.Instance.PlaySFX3D(hornSound, transform.position);
            if (!string.IsNullOrEmpty(marchSound)) AudioManager.Instance.PlaySFX3D(marchSound, transform.position);
            if (!string.IsNullOrEmpty(boneSound)) AudioManager.Instance.PlaySFX3D(boneSound, transform.position);
        }
        StartCoroutine(BossFootfalls());

        float total = bootsBeat + tiltBeat + riseBeat + holdBeat;
        while (shotTime < total)
        {
            shotTime += Time.unscaledDeltaTime;
            UpdateCamera(shotTime);
            yield return null;
        }

        polish.FadeToBlack(outFade);
        IsFinished = true;
    }

    // The bosses get their own, slower footfall. A heavy step at a different
    // tempo from the ranks is what tells the ear something bigger is walking,
    // even when it is too far away to see clearly.
    private IEnumerator BossFootfalls()
    {
        if (string.IsNullOrEmpty(bossStepSound)) yield break;

        var wait = new WaitForSecondsRealtime(Mathf.Max(0.2f, bossStepInterval));
        while (true)
        {
            yield return wait;
            if (AudioManager.Instance == null) continue;

            // Nearest boss to the lens only. Playing one per boss would turn a
            // tread into a stampede.
            float best = float.MaxValue;
            Vector3 at = transform.position;
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].isBoss || units[i].t == null) continue;
                float d = (units[i].t.position - camT.position).sqrMagnitude;
                if (d < best) { best = d; at = units[i].t.position; }
            }
            AudioManager.Instance.PlaySFX3D(bossStepSound, at);
        }
    }

    private void UpdateCamera(float t)
    {
        // One continuous move through three framings. Cutting between them would
        // give the audience a chance to reset; a single rise makes them watch the
        // army get bigger without being allowed to look away.
        // Four beats, and the ORDER is the whole point.
        //
        //  BOOTS. The camera is in the grass, aimed at their legs. Withholding the
        //  faces is what makes them threatening: the audience is given the scale
        //  of the thing before they are given its identity, and something you have
        //  measured but not seen is worse than something you have seen.
        //
        //  TILT. Now look up. The reveal is of WHAT has been walking past, and it
        //  lands because the previous beat refused it.
        //
        //  RISE. One unbroken crane. A cut here would let the audience reset.
        //
        //  HOLD. Stay on it. Trailers usually cut a beat too early; an army with no
        //  end needs time on screen for "no end" to register.
        float tilt = t < bootsBeat ? 0f
                   : Mathf.Clamp01((t - bootsBeat) / Mathf.Max(0.01f, tiltBeat));
        tilt = tilt * tilt * (3f - 2f * tilt);

        float rise;
        float riseStart = bootsBeat + tiltBeat;
        if (t < riseStart) rise = 0f;
        else if (t < riseStart + riseBeat) rise = Mathf.Clamp01((t - riseStart) / riseBeat);
        else rise = 1f;

        // Smootherstep, so the crane has no detectable start or stop.
        float e = rise * rise * rise * (rise * (rise * 6f - 15f) + 10f);

        // Stand clear of the formation's actual edge rather than a hand-typed
        // distance from its axis. A wider rank would otherwise walk through the
        // lens, and nobody would think to look at flankOffset to find out why.
        float halfWidth = (unitsPerRank - 1) * 0.5f * fileSpacing;
        float side = (halfWidth + flankClearance) * Mathf.Lerp(1f, 2.4f, e);

        // Drift AHEAD of the oncoming column as the camera climbs, so the line
        // has somewhere to fit as the lens gets longer.
        float ahead = Mathf.Lerp(0f, craneBack, e);
        float height = Mathf.Lerp(lowHeight, highHeight, e);

        Vector3 basePos = transform.position + right * side + dir * ahead;
        camT.position = new Vector3(basePos.x, SampleGround(basePos) + height, basePos.z);

        // Look BACK down the line, at the ranks coming on. The army walks at the
        // camera and past it: an army marching away is a departure, and a
        // departure is not frightening. Looking further down the column as the
        // camera rises makes the reveal one of DEPTH rather than of more ground.
        Vector3 aim = transform.position - dir * Mathf.Lerp(6f, columnLength * 0.55f, e);
        // Boots first: aim BELOW the knee, then tilt up over beat B, then let the
        // crane take it. Height above ground is what decides whether we are
        // looking at legs or at faces.
        float aimHeight = Mathf.Lerp(Mathf.Lerp(0.35f, 2.1f, tilt), 2.6f, e);
        aim.y = SampleGround(aim) + aimHeight;
        camT.rotation = Quaternion.LookRotation((aim - camT.position).normalized);

        // The lens goes LONG as it rises. This is the shot: telephoto compression
        // stacks the ranks into one another so the column reads far denser and
        // longer than the unit count. The identical army on a wide lens from the
        // same height would look thin.
        shotCamera.fieldOfView = Mathf.Lerp(lowFov, highFov, e);
    }
}
