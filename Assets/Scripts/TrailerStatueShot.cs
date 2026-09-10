using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trailer, shot 1 (0:00-0:10) — the king's statue breaks open.
//
// ==== WHY THE FIRST VERSION READ AS FAKE ====
//
// It showed an EFFECT with no CAUSE. Light appeared next to a statue that was
// visually untouched, and an audience reads that as two unrelated things
// happening at once rather than as stone failing. Three fixes, in order of how
// much each one matters:
//
//  1. THE STONE HAS TO BREAK ON SCREEN. Fractures now crawl across the surface,
//     branch, and arrest at edges (TrailerStatueCrack). The statue trembles from
//     the first crack onward instead of only at the end, sheds dust continuously,
//     and spits chips at the exact points the cracks are advancing through. The
//     light is now something that gets OUT, rather than something that arrives.
//
//  2. LIGHT SHAFTS ARE ONLY VISIBLE BECAUSE OF PARTICULATE. A shaft in clean air
//     is invisible in reality and looks like a plastic ribbon in a game engine.
//     Every ray now drives motes drifting through it, and that single addition
//     does more for believability than any amount of shader work. They also
//     flicker on Perlin noise, punch out with overshoot instead of fading up,
//     and get cut short by anything they hit — a shaft that passes through a
//     wall is the fastest way to lose an audience.
//
//  3. PERIODIC MOTION READS AS MACHINERY. The old handheld was a pair of sines,
//     and the eye catches the repeat even when the viewer cannot say why. All
//     motion here is Perlin, at incommensurate rates per axis, with amplitude
//     driven by tension so the camera gets less steady as the statue gets worse.
//
// ==== CAMERA ====
//
// A trailer's opening shot has to do two jobs at once: hold on the subject, and
// make the viewer feel the operator is a person. So: one uninterrupted push-in
// (a cut would say "here is another angle" instead of "something is coming"),
// the statue held OFF-CENTRE and the look-target damped so framing floats rather
// than locks, a dutch roll that creeps in as tension rises, and a spring recoil
// on every fracture so the camera flinches with the stone.
//
// Everything runs on unscaled time; every generated material is explicit,
// because a script-created ParticleSystem or LineRenderer has no material and
// renders magenta.
[DisallowMultipleComponent]
public class TrailerStatueShot : MonoBehaviour
{
    [Header("Scene")]
    public Transform statue;
    public Camera shotCamera;

    [Header("Beats (seconds)")]
    [Tooltip("Dead-quiet establish before anything happens. The stillness is what makes the first crack land.")]
    public float establish = 2.6f;
    [Tooltip("From first fracture to full burst.")]
    public float buildDuration = 4.8f;
    public float burstHold = 1.3f;
    public float settle = 1.6f;
    public float outFade = 0.8f;

    [Header("Camera Move")]
    public float startDistance = 15f;
    public float endDistance = 5.2f;
    public float startHeight = 3.4f;
    public float endHeight = 2.4f;
    [Tooltip("Degrees of drift around the statue. Small — this is a push, not an orbit.")]
    public float orbitDrift = 7f;
    public float startFov = 36f;
    public float endFov = 48f;
    [Tooltip("Statue's horizontal position in frame, 0.5 = dead centre. Off-centre framing is what stops it looking like a product turntable.")]
    [Range(0.25f, 0.75f)] public float framingBias = 0.42f;
    [Tooltip("How fast the aim catches up to the camera. Lower = more float.")]
    public float lookDamping = 2.2f;
    [Tooltip("Max dutch roll in degrees at full tension.")]
    public float maxRoll = 2.4f;
    [Tooltip("Handheld amplitude in metres at rest. Grows with tension.")]
    public float handheldBase = 0.018f;
    public float handheldAtPeak = 0.075f;

    [Header("Fracture")]
    [Range(1, 8)] public int seedCracks = 4;
    [Tooltip("Seconds between fracture advances at the start. Cracks accelerate as pressure builds.")]
    public float stepIntervalStart = 0.16f;
    public float stepIntervalEnd = 0.035f;
    public float crackWidthScale = 1f;

    [Header("Light")]
    // Ember, not violet.
    //
    // A violet shaft with a near-white core goes PINK the moment it is blended
    // additively: red and blue saturate while green lags behind, and pink is the
    // one colour that cannot read as menacing. Deep ember red with a hot amber
    // core reads as a furnace behind the stone, and sits against the cold blue
    // moonlight instead of dissolving into it.
    //
    // For the violet corruption used elsewhere in the game, set lightColor to
    // roughly (0.18, 0.10, 0.55) and coreColor to (0.55, 0.70, 1.0) — a COLD
    // blue-violet with a low red channel, which stays spectral rather than pink.
    public Color lightColor = new Color(0.60f, 0.085f, 0.04f, 1f);
    public Color coreColor = new Color(1f, 0.52f, 0.20f, 1f);
    public float lightIntensity = 45f;
    [Tooltip("Shaft length in metres before occlusion trims it.")]
    public float rayLength = 24f;
    public float rayWidth = 0.42f;
    [Tooltip("How far the shafts lean toward the camera when a crack opens, 0 = straight out of the stone. They keep that direction for the rest of the shot; the sweep comes from the camera moving past them.")]
    [Range(0f, 1f)] public float rayLeanToCamera = 0.5f;
    [Tooltip("Keep this low. Fast, deep flicker is what makes shafts look like a disco rig; a menacing light barely moves and only breathes.")]
    [Range(0f, 1f)] public float rayFlicker = 0.10f;
    [Tooltip("Flicker speed. Slow is ominous, fast is a fault in a strip light.")]
    public float flickerSpeed = 1.4f;
    [Tooltip("Motes drifting through each shaft. This is what makes a shaft look volumetric rather than printed.")]
    [Range(0, 60)] public int motesPerRay = 22;

    [Header("Collapse")]
    public GameObject[] debrisPrefabs;
    [Range(0, 60)] public int debrisCount = 22;
    [Tooltip("Statue tremor at full tension, in metres.")]
    public float tremorAtPeak = 0.055f;
    [Tooltip("Hide the statue on the burst flash. A single mesh cannot really shatter, so it is swapped for debris behind the flare.")]
    public bool vanishOnBurst = true;
    [Tooltip("Rubble left standing where the statue was, so the aftermath frame has a subject instead of a hole.")]
    [Range(0, 40)] public int rubbleCount = 16;

    [Header("Framing")]
    [Tooltip("Work out the end distance from the statue's actual size instead of trusting endDistance. A hand-typed distance frames whatever the statue's scale happens to be.")]
    public bool autoFrame = true;
    [Tooltip("How much of the frame height the statue's upper body should fill when the push finishes.")]
    [Range(0.3f, 1.1f)] public float framingHeightFraction = 0.78f;

    [Header("Aftermath")]
    [Tooltip("How hard the blast shoves the camera backwards, in metres.")]
    public float blastShove = 2.8f;
    [Tooltip("How far the camera cranes up afterwards to show what is left.")]
    public float postRise = 2.6f;
    [Tooltip("The column of dark light that stands where the statue was. This is the image the next shot cuts from.")]
    public float beamHeight = 45f;
    public float beamWidth = 1.8f;

    [Header("Audio")]
    public string groanSound = AudioID.Region_Shockwave;
    public string crackSound = AudioID.Enemy_Agro;
    public string burstSound = AudioID.Region_Shockwave;

    // ---- runtime ----
    private Transform camT;
    private Bounds bounds;
    private Vector3 center;
    private float baseAzimuth;
    private Material rayMat, crackMat;
    private readonly List<TrailerStatueCrack> cracks = new List<TrailerStatueCrack>();
    private readonly List<Shaft> shafts = new List<Shaft>();
    private readonly List<Renderer> statueRenderers = new List<Renderer>();
    private ParticleSystem sheetDust, chipBurst;
    private Vector3 statueHome;
    private float tension;              // 0..1, the single value everything reads from
    private Vector3 recoilVel, recoilOffset;
    private float noiseSeed;
    private Vector3 lookTarget;
    private float pushSeconds;      // how long the approach lasts — the dolly stops when the statue does
    private float burstStartedAt = -1f;
    private float resolvedEndDistance;
    private LineRenderer beam;
    private ParticleSystem beamMotes;

    private class Shaft
    {
        public LineRenderer line;
        public Light glow;
        public ParticleSystem motes;
        public Vector3 originLocal, normal;   // local to the statue, so the shaft rides the tremor
        // Fixed at birth and never re-aimed. Shafts that chase the camera every
        // frame swing around the screen like searchlights; real light from a
        // fixed source is still, and it is the CAMERA moving past it that makes
        // it sweep across frame.
        public Vector3 dirLocal;
        public Vector3 Origin(Transform statue) => statue.TransformPoint(originLocal);
        public float bornAt = -1f;
        public float phase;
        public float widthScale;
    }

    private void Start()
    {
        if (statue == null) { Debug.LogWarning("[StatueShot] No statue assigned."); enabled = false; return; }
        shotCamera = shotCamera != null ? shotCamera : Camera.main;
        if (shotCamera == null) { Debug.LogWarning("[StatueShot] No camera."); enabled = false; return; }

        camT = shotCamera.transform;
        noiseSeed = Random.Range(0f, 1000f);
        statue.GetComponentsInChildren(statueRenderers);

        bounds = ComputeBounds(statue);
        center = bounds.center;
        statueHome = statue.position;
        lookTarget = center;

        Vector3 flat = camT.position - center; flat.y = 0f;
        baseAzimuth = flat.sqrMagnitude > 0.01f ? Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg : Random.Range(0f, 360f);

        pushSeconds = establish + buildDuration;
        resolvedEndDistance = ResolveEndDistance();

        BuildMaterials();
        BuildStatueDust();
        StartCoroutine(PlayShot());
    }

    // Where the push should STOP.
    //
    // The first version ended at a hand-typed 5.2 metres, which frames whatever
    // the statue's scale happens to be — at this statue's size that put the lens
    // inside the torso, filling the screen with an unreadable slab of stone. Solve
    // it from the subject instead: to make a world height H fill fraction f of the
    // frame at vertical FOV t, the camera has to sit H / (2f * tan(t/2)) away.
    private float ResolveEndDistance()
    {
        if (!autoFrame) return endDistance;

        float subjectHeight = bounds.size.y * SubjectFraction;
        float d = subjectHeight / (2f * Mathf.Max(0.05f, framingHeightFraction)
                                   * Mathf.Tan(endFov * 0.5f * Mathf.Deg2Rad));
        // Never end up inside the statue, however aggressive the framing asks to be.
        return Mathf.Max(d, bounds.extents.magnitude * 0.75f);
    }

    // The part of the statue the shot is actually about: head and shoulders.
    private const float SubjectFraction = 0.45f;

    private static Bounds ComputeBounds(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(root.position, Vector3.one * 4f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    private void BuildMaterials()
    {
        rayMat = MakeUnlit(additive: true);
        crackMat = MakeUnlit(additive: false);   // the fissure must be able to go DARKER than the stone
    }

    private static Material MakeUnlit(bool additive)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        var m = new Material(sh);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", additive
                ? (int)UnityEngine.Rendering.BlendMode.One
                : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        m.color = Color.white;
        return m;
    }

    // Dust sheeting off the whole statue, and a chip emitter re-aimed at whichever
    // fracture is advancing. Continuous shedding is most of what makes the stone
    // look like it is under load rather than sitting still next to an effect.
    private void BuildStatueDust()
    {
        sheetDust = MakeParticles("SheetDust", statue, new Color(0.52f, 0.48f, 0.56f, 0.30f));
        var m = sheetDust.main;
        m.startLifetime = 2.6f;
        m.startSpeed = 0.35f;
        m.startSize = 0.28f;
        m.gravityModifier = 0.12f;
        m.maxParticles = 220;
        var sh = sheetDust.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = bounds.size * 0.85f;
        sheetDust.transform.position = center;
        var em = sheetDust.emission;
        em.rateOverTime = 0f;    // driven by tension
        sheetDust.Play();

        chipBurst = MakeParticles("Chips", transform, new Color(0.42f, 0.38f, 0.44f, 0.95f));
        var cm = chipBurst.main;
        cm.startLifetime = 1.4f;
        cm.startSpeed = 2.6f;
        cm.startSize = 0.06f;
        cm.gravityModifier = 1.1f;
        cm.maxParticles = 260;
        var ce = chipBurst.emission;
        ce.rateOverTime = 0f;
        var cs = chipBurst.shape;
        cs.shapeType = ParticleSystemShapeType.Cone;
        cs.angle = 32f;
        cs.radius = 0.05f;
        chipBurst.Play();
    }

    private ParticleSystem MakeParticles(string name, Transform parent, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startColor = tint;
        main.useUnscaledTime = true;

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0f;

        var pr = ps.GetComponent<ParticleSystemRenderer>();
        Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psh == null) psh = Shader.Find("Sprites/Default");
        if (psh != null) pr.material = new Material(psh);
        else pr.enabled = false;

        return ps;
    }

    // ======================= the shot =======================

    private IEnumerator PlayShot()
    {
        var polish = TrailerCinematicPolish.GetOrCreate();
        polish.OpenTrailer();

        float total = establish + buildDuration + burstHold + settle;
        float t = 0f;
        bool burst = false;
        float nextCrackStep = establish;
        int seeded = 0;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(groanSound))
            AudioManager.Instance.PlaySFX3D(groanSound, center);

        while (t < total)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            // Tension is the one dial. Everything — tremor, dust, light, camera
            // steadiness, roll, fracture speed — reads from it, so the whole shot
            // escalates together instead of as separate effects on separate clocks.
            tension = Mathf.Clamp01((t - establish) / Mathf.Max(0.01f, buildDuration));
            if (burst) tension = 1f;

            UpdateCamera(t, total, dt);
            UpdateStatue(dt);
            UpdateShafts(t);
            UpdateBeam(t);

            if (!burst && t >= establish && t < establish + buildDuration && t >= nextCrackStep)
            {
                // Seed the first cracks in the opening moments, then let them run.
                if (seeded < seedCracks && (seeded == 0 || Random.value < 0.5f))
                {
                    SeedCrack();
                    seeded++;
                }
                StepCracks();

                // Fractures accelerate as pressure rises: the gaps between
                // events shorten, which is what an audience hears as "worsening".
                nextCrackStep = t + Mathf.Lerp(stepIntervalStart, stepIntervalEnd, tension);
            }

            if (!burst && t >= establish + buildDuration)
            {
                burst = true;
                burstStartedAt = t;
                StartCoroutine(BurstRoutine());
            }

            yield return null;
        }

        polish.FadeToBlack(outFade);
    }

    // ======================= camera =======================

    private void UpdateCamera(float t, float total, float dt)
    {
        // The dolly runs only while there is something to approach. It used to be
        // spread over the WHOLE shot, so for the last three seconds — after the
        // statue had already gone — the camera was still creeping toward an empty
        // patch of air. That is the "it ends up somewhere odd filming nothing".
        float u = Mathf.Clamp01(t / Mathf.Max(0.01f, pushSeconds));
        // Smootherstep: zero velocity AND zero acceleration at both ends, so the
        // push never announces its start or its stop.
        float e = u * u * u * (u * (u * 6f - 15f) + 10f);

        float az = (baseAzimuth + Mathf.Lerp(0f, orbitDrift, e)) * Mathf.Deg2Rad;
        float dist = Mathf.Lerp(startDistance, resolvedEndDistance, e);
        float h = Mathf.Lerp(startHeight, endHeight, e);

        // --- aftermath -----------------------------------------------------
        // Once the stone goes, the shot stops being about approaching and starts
        // being about what is left. The camera is shoved back by the blast, never
        // fully recovers, and cranes up to put the column of light in frame — the
        // image the next shot cuts away from.
        float post = burstStartedAt >= 0f ? t - burstStartedAt : -1f;
        float shove = 0f, rise = 0f, fovKick = 0f;
        if (post >= 0f)
        {
            // Fast punch out, slow partial recovery: a camera knocked back and
            // steadied by hand, not one on a return spring.
            float knock = Mathf.Exp(-post * 3.2f);
            shove = blastShove * (0.45f + 0.55f * knock);
            rise = postRise * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((post - 0.25f) / 1.6f));
            fovKick = 9f * Mathf.Exp(-post * 2.4f);
        }

        Vector3 pos = new Vector3(center.x + Mathf.Cos(az) * (dist + shove),
                                  bounds.min.y + h + rise,
                                  center.z + Mathf.Sin(az) * (dist + shove));

        // Perlin handheld, incommensurate per axis so it never visibly repeats,
        // and louder as the statue gets worse.
        float amp = Mathf.Lerp(handheldBase, handheldAtPeak, tension);
        Vector3 shake = new Vector3(
            (Mathf.PerlinNoise(noiseSeed + t * 1.7f, 0f) - 0.5f),
            (Mathf.PerlinNoise(0f, noiseSeed + t * 2.3f) - 0.5f),
            (Mathf.PerlinNoise(noiseSeed + t * 1.1f, noiseSeed) - 0.5f)) * (amp * 2f);

        // Spring-damped recoil from each fracture. A flinch that decays is read
        // as a reaction; an instant offset is read as a glitch.
        recoilVel = Vector3.Lerp(recoilVel, Vector3.zero, 1f - Mathf.Exp(-9f * dt));
        recoilOffset += recoilVel * dt;
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, 1f - Mathf.Exp(-7f * dt));

        camT.position = pos + camT.right * shake.x + camT.up * shake.y + camT.forward * shake.z + recoilOffset;

        // Damped aim. During the push it settles on the head and shoulders — the
        // part of the statue the shot is about. Afterwards it climbs the beam,
        // which both gives the camera somewhere to be and says the thing that got
        // out went upward.
        float subjectY = bounds.max.y - bounds.size.y * SubjectFraction * 0.5f;
        Vector3 desired = new Vector3(center.x, Mathf.Lerp(center.y, subjectY, e), center.z);
        if (post >= 0f)
        {
            float climb = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((post - 0.4f) / 1.8f));
            desired.y = Mathf.Lerp(subjectY, bounds.min.y + beamHeight * 0.28f, climb);
        }
        lookTarget = Vector3.Lerp(lookTarget, desired, 1f - Mathf.Exp(-lookDamping * dt));

        Quaternion look = Quaternion.LookRotation((lookTarget - camT.position).normalized);
        // Bias the subject off-centre by yawing slightly off the aim.
        float yawBias = (0.5f - framingBias) * shotCamera.fieldOfView;
        // Dutch roll creeps in with tension — rarely noticed consciously, always felt.
        float roll = maxRoll * tension * (0.6f + 0.4f * (Mathf.PerlinNoise(noiseSeed + t * 0.6f, 4f) - 0.5f) * 2f);
        camT.rotation = look * Quaternion.Euler(0f, yawBias, roll);

        shotCamera.fieldOfView = Mathf.Lerp(startFov, endFov, e) + fovKick;
    }

    private void Kick(Vector3 fromPoint, float strength)
    {
        Vector3 dir = (camT.position - fromPoint).normalized;
        recoilVel += (dir + Random.insideUnitSphere * 0.4f) * strength;
    }

    // ======================= statue =======================

    private void UpdateStatue(float dt)
    {
        // Perlin tremor, not random jitter: random reads as noise, Perlin reads
        // as strain — the stone straining rather than the transform vibrating.
        float a = tremorAtPeak * tension * tension;
        float tt = Time.unscaledTime;
        Vector3 tremor = new Vector3(
            Mathf.PerlinNoise(tt * 13f, noiseSeed) - 0.5f,
            Mathf.PerlinNoise(noiseSeed, tt * 17f) - 0.5f,
            Mathf.PerlinNoise(tt * 11f, tt * 7f) - 0.5f) * (a * 2f);
        statue.position = statueHome + tremor;

        if (sheetDust != null)
        {
            var em = sheetDust.emission;
            em.rateOverTime = Mathf.Lerp(0f, 90f, tension * tension);
        }
    }

    private void SeedCrack()
    {
        if (!FindSurfacePoint(out Vector3 pos, out Vector3 nrm)) return;
        SpawnCrack(pos, nrm, 0);

        var shaft = BuildShaft(pos, nrm);
        shafts.Add(shaft);

        Kick(pos, 0.55f);
        TrailerCinematicPolish.GetOrCreate().ImpactPunch(0.3f, 0.22f);
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(crackSound))
            AudioManager.Instance.PlaySFX3D(crackSound, pos);
    }

    private void SpawnCrack(Vector3 pos, Vector3 nrm, int generation)
    {
        var go = new GameObject($"Crack_{cracks.Count}");
        var c = go.AddComponent<TrailerStatueCrack>();
        c.generation = generation;
        c.glowColor = lightColor;
        c.Init(statue, pos, nrm, crackMat, OnCrackStep);
        cracks.Add(c);
    }

    private void StepCracks()
    {
        int live = 0;
        for (int i = 0; i < cracks.Count; i++)
        {
            var c = cracks[i];
            if (c == null || c.Finished) continue;
            live++;

            if (c.Advance() && c.ShouldBranch())
            {
                c.GetBranchSeed(out Vector3 bp, out Vector3 bn);
                SpawnCrack(bp, bn, c.generation + 1);
            }
            c.SetHeat(tension, crackWidthScale);
        }

        // Everything has arrested but the burst has not arrived — open a new
        // fracture so the stone never goes quiet mid-build.
        if (live == 0 && tension < 0.95f) SeedCrack();
    }

    // Chips fly from wherever the fracture is actually advancing, so the debris
    // is tied to a visible cause instead of puffing from the statue generally.
    private void OnCrackStep(Vector3 pos, Vector3 nrm)
    {
        if (chipBurst == null) return;
        chipBurst.transform.position = pos;
        chipBurst.transform.rotation = Quaternion.LookRotation(nrm);
        chipBurst.Emit(Random.Range(1, 4));
    }

    private bool FindSurfacePoint(out Vector3 pos, out Vector3 nrm)
    {
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 4f;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            // Bias toward the camera-facing side and the upper body — where the
            // audience is already looking.
            float ang = baseAzimuth + Random.Range(-90f, 90f);
            float h = Mathf.Lerp(bounds.min.y + bounds.size.y * 0.30f,
                                 bounds.max.y - bounds.size.y * 0.06f, Random.value);

            Vector3 from = new Vector3(center.x + Mathf.Cos(ang * Mathf.Deg2Rad) * radius, h,
                                       center.z + Mathf.Sin(ang * Mathf.Deg2Rad) * radius);
            Vector3 dir = new Vector3(center.x - from.x, 0f, center.z - from.z).normalized;

            if (Physics.Raycast(from, dir, out RaycastHit hit, radius * 2.5f, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform.IsChildOf(statue))
            {
                pos = hit.point + hit.normal * 0.02f;
                nrm = hit.normal;
                return true;
            }
        }
        pos = center; nrm = Vector3.up;
        return false;
    }

    // ======================= shafts =======================

    private Shaft BuildShaft(Vector3 pos, Vector3 nrm)
    {
        var root = new GameObject($"Shaft_{shafts.Count}");
        root.transform.SetParent(statue, true);
        root.transform.position = pos;

        // Point it out of the crack, leaned toward the camera's side so it is
        // actually visible, then leave it alone for the rest of the shot.
        Vector3 toCam = (camT.position - pos).normalized;
        Vector3 worldDir = Vector3.Slerp(nrm.normalized, toCam, rayLeanToCamera).normalized;
        worldDir = Quaternion.AngleAxis(Random.Range(-14f, 14f), camT.up) * worldDir;
        worldDir = Quaternion.AngleAxis(Random.Range(-9f, 9f), camT.right) * worldDir;

        var s = new Shaft { originLocal = statue.InverseTransformPoint(pos), normal = nrm,
                            dirLocal = statue.InverseTransformDirection(worldDir),
                            bornAt = Time.unscaledTime, phase = Random.Range(0f, 10f) };
        // Per-shaft width variation. Identical shafts are a tell; nothing in
        // nature emits a matched set.
        s.widthScale = Random.Range(0.65f, 1.35f);

        var lgo = new GameObject("Glow");
        lgo.transform.SetParent(root.transform, false);
        s.glow = lgo.AddComponent<Light>();
        s.glow.type = LightType.Point;
        s.glow.color = lightColor;
        s.glow.range = 8f;
        s.glow.intensity = 0f;
        s.glow.shadows = LightShadows.None;

        var rgo = new GameObject("Ray");
        rgo.transform.SetParent(root.transform, false);
        s.line = rgo.AddComponent<LineRenderer>();
        s.line.useWorldSpace = true;
        s.line.positionCount = 2;
        s.line.material = rayMat;
        s.line.numCapVertices = 3;
        s.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        s.line.receiveShadows = false;
        // Narrow at the stone, swelling toward the lens, then tapering — a shaft
        // of even width is the shape of a ribbon, not of light.
        s.line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.12f), new Keyframe(0.55f, 1f), new Keyframe(1f, 0.55f));
        s.line.widthMultiplier = 0f;

        s.motes = MakeParticles("Motes", root.transform, coreColor);
        var mm = s.motes.main;
        mm.startLifetime = 2.4f;
        mm.startSpeed = 1.6f;
        mm.startSize = 0.055f;
        mm.gravityModifier = -0.02f;      // drift upward, the way lit dust does
        mm.maxParticles = Mathf.Max(4, motesPerRay * 2);
        var ms = s.motes.shape;
        ms.shapeType = ParticleSystemShapeType.Cone;
        ms.angle = 7f;
        ms.radius = 0.06f;
        s.motes.Play();

        return s;
    }

    private void UpdateShafts(float t)
    {
        // The shafts come out of cracks in a statue that is about to stop
        // existing. Once it goes they have no source, so they hand over to the
        // vertical beam rather than hanging in the air pouring out of nothing.
        float handover = burstStartedAt < 0f
            ? 1f
            : Mathf.Clamp01(1f - (t - burstStartedAt - 0.35f) / 0.9f);

        for (int i = 0; i < shafts.Count; i++)
        {
            Shaft s = shafts[i];
            float age = Time.unscaledTime - s.bornAt;

            // Punch out with overshoot, then settle. Light forcing its way
            // through stone arrives; it does not fade up like a dimmer.
            float grow = 1f - Mathf.Exp(-age * 5f);
            grow *= 1f + 0.22f * Mathf.Exp(-age * 6f) * Mathf.Sin(age * 26f);
            grow = Mathf.Clamp01(grow);

            // A slow, shallow breath. Enough that the light is not dead, nowhere
            // near enough to strobe.
            float flick = 1f + (Mathf.PerlinNoise(s.phase, t * flickerSpeed) - 0.5f) * 2f * rayFlicker;

            s.glow.intensity = lightIntensity * grow * (0.3f + tension) * flick * handover;
            s.glow.color = Color.Lerp(lightColor, coreColor, tension * 0.5f);

            Vector3 origin = s.Origin(statue);
            // Direction was decided when the crack opened and does not change.
            // The sweep across frame comes from the camera travelling past a
            // stationary shaft, which is how it works in life and the only way it
            // stops looking like a lighting rig.
            Vector3 dir = statue.TransformDirection(s.dirLocal).normalized;
            float len = rayLength * grow;

            // Occlusion. A shaft that passes through a wall destroys the shot
            // faster than any amount of shader quality can save it.
            if (Physics.Raycast(origin + dir * 0.15f, dir, out RaycastHit blk, len, ~0, QueryTriggerInteraction.Ignore)
                && !blk.transform.IsChildOf(statue))
                len = Mathf.Max(0.4f, blk.distance);

            s.line.SetPosition(0, origin);
            s.line.SetPosition(1, origin + dir * len);
            // Intensity breathes; WIDTH does not. A shaft whose thickness pulses
            // reads as a bad effect rather than as light.
            s.line.widthMultiplier = rayWidth * s.widthScale * grow * (0.6f + 0.6f * tension) * handover;

            Color hot = Color.Lerp(lightColor, coreColor, 0.3f + 0.5f * tension);
            hot.a = grow * (0.30f + 0.70f * tension) * handover;
            s.line.startColor = hot;
            Color tail = hot; tail.a = 0f;
            s.line.endColor = tail;

            // Motes ride the beam. This is the single biggest contributor to a
            // shaft looking volumetric instead of printed on the screen.
            if (s.motes != null)
            {
                s.motes.transform.position = origin;
                s.motes.transform.rotation = Quaternion.LookRotation(dir);
                var em = s.motes.emission;
                em.rateOverTime = motesPerRay * grow * (0.25f + tension) * handover;
            }
        }
    }

    // ======================= burst =======================

    private IEnumerator BurstRoutine()
    {
        var polish = TrailerCinematicPolish.GetOrCreate();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(burstSound))
            AudioManager.Instance.PlaySFX3D(burstSound, center);

        polish.ImpactPunch(1f, 0.7f);
        polish.TimeRamp(0.32f, burstHold, 0.04f, 0.45f);
        Kick(center, 2.4f);

        // Widen every fracture at once — the stone gives up as one.
        for (int i = 0; i < cracks.Count; i++)
            if (cracks[i] != null) cracks[i].SetHeat(1f, crackWidthScale * 3.2f);

        SpawnDebris();
        SpawnRubble();
        BuildBeam();
        if (chipBurst != null) chipBurst.Emit(120);

        // Hide the mesh under the flare. A single mesh cannot really shatter, so
        // the swap happens at the brightest frame, where the eye cannot follow it.
        if (vanishOnBurst)
        {
            yield return new WaitForSecondsRealtime(0.10f);
            for (int i = 0; i < statueRenderers.Count; i++)
                if (statueRenderers[i] != null) statueRenderers[i].enabled = false;
            if (sheetDust != null)
            {
                var em = sheetDust.emission;
                em.rateOverTime = 400f;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);
        if (sheetDust != null)
        {
            var em = sheetDust.emission;
            em.rateOverTime = 60f;     // let it hang and settle in the light
        }
    }

    // The column of dark light standing where the statue was.
    //
    // Without it the aftermath frame is a hole: the statue is gone, the shafts
    // have nothing to come from, and the camera is pointed at dust. The beam gives
    // the shot a subject for its last seconds and states plainly that whatever was
    // sealed in the stone is now loose — which is the sentence this shot exists to
    // say, and the image the next one can cut away from.
    private void BuildBeam()
    {
        var go = new GameObject("Beam");
        go.transform.SetParent(transform, false);

        beam = go.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.material = rayMat;
        beam.numCapVertices = 4;
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beam.receiveShadows = false;
        // Fat at the base, tapering as it climbs — a column that keeps one width
        // reads as a cylinder, not as light escaping under pressure.
        beam.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.35f, 0.75f), new Keyframe(1f, 0.28f));
        beam.widthMultiplier = 0f;

        beamMotes = MakeParticles("BeamMotes", go.transform, coreColor);
        var mm = beamMotes.main;
        mm.startLifetime = 3.2f;
        mm.startSpeed = 5.5f;
        mm.startSize = 0.09f;
        mm.gravityModifier = -0.05f;
        mm.maxParticles = 300;
        var ms = beamMotes.shape;
        ms.shapeType = ParticleSystemShapeType.Cone;
        ms.angle = 4f;
        ms.radius = beamWidth * 0.35f;
        beamMotes.transform.position = new Vector3(center.x, bounds.min.y, center.z);
        beamMotes.transform.rotation = Quaternion.LookRotation(Vector3.up);
        beamMotes.Play();
    }

    private void UpdateBeam(float t)
    {
        if (beam == null || burstStartedAt < 0f) return;

        float age = t - burstStartedAt;
        // Punches up hard, then keeps climbing slowly. Something breaking out
        // does not ease in.
        float grow = 1f - Mathf.Exp(-age * 6f);
        float flick = 1f + (Mathf.PerlinNoise(91f, t * flickerSpeed * 0.7f) - 0.5f) * 2f * rayFlicker * 0.6f;

        Vector3 basePos = new Vector3(center.x, bounds.min.y, center.z);
        beam.SetPosition(0, basePos);
        beam.SetPosition(1, basePos + Vector3.up * beamHeight * grow);
        beam.widthMultiplier = beamWidth * grow * flick;

        Color hot = Color.Lerp(lightColor, coreColor, 0.55f);
        hot.a = grow;
        beam.startColor = hot;
        Color top = lightColor; top.a = 0f;
        beam.endColor = top;

        if (beamMotes != null)
        {
            var em = beamMotes.emission;
            em.rateOverTime = 110f * grow;
        }
    }

    // Debris that stays. The flying chunks are gone in a couple of seconds, and
    // an aftermath with nothing on the ground reads as the statue having been
    // deleted rather than destroyed.
    private void SpawnRubble()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0 || rubbleCount <= 0) return;

        float spread = Mathf.Max(0.8f, bounds.extents.x * 0.9f);
        for (int i = 0; i < rubbleCount; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (prefab == null) continue;

            Vector2 off = Random.insideUnitCircle * spread;
            Vector3 pos = new Vector3(center.x + off.x, bounds.min.y + Random.Range(0.1f, 1.2f), center.z + off.y);

            GameObject chunk = Instantiate(prefab, pos, Random.rotation);
            chunk.transform.localScale *= Random.Range(0.12f, 0.42f);

            var rb = chunk.GetComponent<Rigidbody>() ?? chunk.AddComponent<Rigidbody>();
            rb.mass = 1.4f;
            rb.linearVelocity = Random.insideUnitSphere * 1.2f;
            rb.angularVelocity = Random.insideUnitSphere * 2.5f;
            // No Destroy: this pile is the set dressing for the rest of the shot.
        }
    }

    private void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0 || debrisCount <= 0) return;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (prefab == null) continue;

            Vector3 from = center;
            if (cracks.Count > 0)
            {
                var pick = cracks[Random.Range(0, cracks.Count)];
                if (pick != null) from = pick.Tip;
            }

            GameObject chunk = Instantiate(prefab, from, Random.rotation);
            chunk.transform.localScale *= Random.Range(0.07f, 0.26f);

            var rb = chunk.GetComponent<Rigidbody>() ?? chunk.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            Vector3 away = (from - center).normalized + Vector3.up * Random.Range(0.3f, 0.9f);
            rb.linearVelocity = away * Random.Range(4f, 9f) + Random.insideUnitSphere * 1.5f;
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            Destroy(chunk, 7f);
        }
    }

    private void OnDestroy()
    {
        if (rayMat != null) Destroy(rayMat);
        if (crackMat != null) Destroy(crackMat);
    }
}
