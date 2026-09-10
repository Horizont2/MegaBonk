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
    public float outFade = 0.4f;

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
    [Tooltip("How far the shafts push toward the hot core colour. Additive blending SUMS overlapping shafts, so anything high here saturates every channel and the light turns white — which is exactly what stops it looking dangerous. Keep it low and let only the crack mouths burn.")]
    [Range(0f, 1f)] public float rayHeat = 0.30f;
    [Tooltip("Peak opacity of a single shaft. Low, because they stack: six faint shafts crossing read far darker and more solid than six bright ones, which just blow out to white.")]
    [Range(0.05f, 1f)] public float rayOpacity = 0.42f;
    [Tooltip("Flicker speed. Slow is ominous, fast is a fault in a strip light.")]
    public float flickerSpeed = 1.4f;
    [Tooltip("Motes drifting through each shaft. This is what makes a shaft look volumetric rather than printed.")]
    [Range(0, 60)] public int motesPerRay = 22;

    [Header("Collapse")]
    public GameObject[] debrisPrefabs;
    [Range(0, 60)] public int debrisCount = 22;
    [Tooltip("Statue tremor at full tension, in metres.")]
    public float tremorAtPeak = 0.055f;
    [Tooltip("Hide the statue mid-burst. Left OFF now that the shot ends on the flare — there is no aftermath frame to hide an intact statue in, so the swap would only pop.")]
    public bool vanishOnBurst = false;
    [Header("Framing")]
    [Tooltip("Work out the end distance from the statue's actual size instead of trusting endDistance. A hand-typed distance frames whatever the statue's scale happens to be.")]
    public bool autoFrame = true;
    [Tooltip("How much of the frame height the statue's upper body should fill when the push finishes.")]
    [Range(0.3f, 1.1f)] public float framingHeightFraction = 0.78f;
    [Tooltip("Metres of clearance kept between the lens and the statue's widest point. The framing maths solves for composition and knows nothing about how wide the stone is, so without this it will happily park the camera inside it.")]
    public float clearance = 2.5f;

    [Header("Ending — the shaft takes the lens")]
    // The shot ends ON the burst, not after it.
    //
    // Everything that came after — the camera shoved back, craning up onto rubble
    // and a column of light — was a second, weaker shot glued to the end of a good
    // one, and it is why the camera kept finishing somewhere odd looking at
    // nothing. A trailer's opening beat should hand over at its peak. So the last
    // fracture fires a shaft straight down the barrel: it floods the lens, burns
    // out to white, and collapses to black. The blackout IS the cut.
    [Tooltip("Seconds from the burst to full black. This whole window is the transition to the next shot.")]
    public float pierceDuration = 1.15f;
    [Tooltip("How hard the blast shoves the camera back as the shaft hits, in metres.")]
    public float blastShove = 1.6f;

    [Header("Audio")]
    // Own sounds, not borrowed combat ones. A shockwave standing in for cracking
    // stone is the kind of thing an audience cannot name but does notice.
    public string dreadBed = AudioID.Trailer_Dread;
    public string groanSound = AudioID.Trailer_StoneStress;
    public string crackSound = AudioID.Trailer_StoneCrack;
    public string burstSound = AudioID.Trailer_StoneBurst;
    public string rubbleSound = AudioID.Trailer_Rubble;
    public string riserSound = AudioID.Trailer_Riser;

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
        return Mathf.Max(d, MinOrbitRadius);
    }

    // The part of the statue the shot is actually about: head and shoulders.
    private const float SubjectFraction = 0.45f;

    // The closest the lens may ever get to the statue's axis.
    //
    // The framing maths solves for how the subject SITS in frame and knows
    // nothing about how wide the statue is, so on a broad or heavily-scaled
    // statue it happily asks for a distance that is inside the stone. Measured
    // off the actual horizontal footprint so it holds whatever the statue is, and
    // applied to the LIVE distance every frame rather than only to the end value
    // — otherwise the push could still clip a shoulder on the way in.
    private float MinOrbitRadius =>
        new Vector2(bounds.extents.x, bounds.extents.z).magnitude + clearance;

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

    // A particle material with no texture draws each particle as a hard-edged
    // QUAD. That is the whole reason the dust and chips looked like flying
    // Minecraft blocks — nothing to do with the meshes, everything to do with a
    // missing sprite. Generated once here so the component stays drop-in.
    private static Texture2D s_softDot;
    private static Texture2D SoftDot()
    {
        if (s_softDot != null) return s_softDot;

        const int N = 64;
        s_softDot = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[N * N];
        float c = (N - 1) * 0.5f;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
            // Squared falloff: a linear ramp still shows a visible disc edge.
            float a = Mathf.Clamp01(1f - d);
            a *= a;
            px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        s_softDot.SetPixels32(px);
        s_softDot.Apply(true);
        return s_softDot;
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

        // Shrink and fade out rather than blinking out of existence at the end
        // of the lifetime, which is the other half of the "cheap particles" look.
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Tumble, so chips read as fragments rather than as sprites sliding.
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var pr = ps.GetComponent<ParticleSystemRenderer>();
        Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psh == null) psh = Shader.Find("Sprites/Default");
        if (psh != null)
        {
            var mat = new Material(psh);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", SoftDot());
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", SoftDot());
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

        return ps;
    }

    // ======================= the shot =======================

    private IEnumerator PlayShot()
    {
        var polish = TrailerCinematicPolish.GetOrCreate();
        polish.OpenTrailer();
        TrailerAudio.SilenceStaleBeds();

        // The dread bed runs under the whole shot. Without something holding the
        // low end, the silences between cracks read as the audio having stopped
        // rather than as the shot holding its breath.
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(dreadBed))
            AudioManager.Instance.PlaySFX3D(dreadBed, center);

        float total = establish + buildDuration + pierceDuration;
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
            UpdatePierce(t);

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
        float dist = Mathf.Max(Mathf.Lerp(startDistance, resolvedEndDistance, e), MinOrbitRadius);
        float h = Mathf.Lerp(startHeight, endHeight, e);

        // The blast shoves the lens back for the fraction of a second before the
        // light takes the frame. There is no "afterwards" any more — the shot
        // hands over at its peak.
        float post = burstStartedAt >= 0f ? t - burstStartedAt : -1f;
        float shove = 0f, fovKick = 0f;
        if (post >= 0f)
        {
            float knock = Mathf.Exp(-post * 3.2f);
            shove = blastShove * (0.45f + 0.55f * knock);
            fovKick = 7f * Mathf.Exp(-post * 2.6f);
        }

        Vector3 pos = new Vector3(center.x + Mathf.Cos(az) * (dist + shove),
                                  bounds.min.y + h,
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

        // Damped aim, settling on the head and shoulders — the part of the statue
        // the shot is actually about.
        float subjectY = bounds.max.y - bounds.size.y * SubjectFraction * 0.5f;
        Vector3 desired = new Vector3(center.x, Mathf.Lerp(center.y, subjectY, e), center.z);
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
        // lens flare rather than hanging in the air pouring out of nothing.
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

            s.glow.intensity = lightIntensity * grow * (0.25f + 0.75f * tension) * flick * handover;
            s.glow.color = Color.Lerp(lightColor, coreColor, rayHeat * tension);

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

            // Hot only at the mouth, and only a little. The far end stays the deep
            // ember, so a shaft reads as light escaping from something burning
            // rather than as a white bar drawn across the frame.
            Color mouth = Color.Lerp(lightColor, coreColor, rayHeat * (0.5f + 0.5f * tension));
            mouth.a = grow * rayOpacity * (0.45f + 0.55f * tension) * handover;
            s.line.startColor = mouth;

            Color tail = lightColor; tail.a = 0f;
            s.line.endColor = tail;

            // Motes ride the shaft. This is the single biggest contributor to a
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

        // Riser first: the ear needs a moment of rising pitch BEFORE the hit, or
        // the burst lands as a bang rather than as an arrival.
        if (AudioManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(riserSound)) AudioManager.Instance.PlaySFX3D(riserSound, center);
            if (!string.IsNullOrEmpty(rubbleSound)) AudioManager.Instance.PlaySFX3D(rubbleSound, center);
        }

        polish.ImpactPunch(1f, 0.7f);
        polish.TimeRamp(0.32f, pierceDuration * 0.6f, 0.04f, 0.45f);
        Kick(center, 2.4f);

        // Widen every fracture at once — the stone gives up as one.
        for (int i = 0; i < cracks.Count; i++)
            if (cracks[i] != null) cracks[i].SetHeat(1f, crackWidthScale * 3.2f);

        SpawnDebris();
        BuildPierce();
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

    // ===================== the ending =====================
    //
    // The last fracture fires a shaft STRAIGHT DOWN THE BARREL.
    //
    // Everywhere else in this shot a shaft aimed at the lens would be wrong — it
    // foreshortens to a dot and reads as nothing. Here that is the point: pointed
    // at the camera it stops being a shaft and becomes a flare that opens out of a
    // single burning point until it owns the frame. It floods, burns to white,
    // then goes black, and the blackout is the cut to the next shot.
    //
    // Drawn as two QUADS PARENTED TO THE CAMERA rather than as UI. The first
    // version used a screen-space Canvas and never appeared on screen; rather
    // than keep guessing at canvas nesting and sort order, this renders through
    // the camera's own frustum, where there is nothing left to get wrong.
    private Transform pierceFlare;     // soft disc: the light arriving
    private Transform pierceBlack;     // solid: the cut
    private Material pierceFlareMat, pierceBlackMat;
    private Vector3 pierceOrigin;

    private void BuildPierce()
    {
        // Open from the crack nearest the middle of frame. The flare has to grow
        // out of somewhere the audience is already looking, or it reads as an
        // unrelated wipe instead of as this light arriving.
        pierceOrigin = center;
        float best = float.MaxValue;
        for (int i = 0; i < shafts.Count; i++)
        {
            Vector3 p = shafts[i].Origin(statue);
            Vector3 v = shotCamera.WorldToViewportPoint(p);
            if (v.z <= 0f) continue;
            float d = (new Vector2(v.x, v.y) - new Vector2(0.5f, 0.5f)).sqrMagnitude;
            if (d < best) { best = d; pierceOrigin = p; }
        }

        pierceFlareMat = MakeUnlit(additive: false);
        if (pierceFlareMat.HasProperty("_BaseMap")) pierceFlareMat.SetTexture("_BaseMap", SoftDot());
        if (pierceFlareMat.HasProperty("_MainTex")) pierceFlareMat.SetTexture("_MainTex", SoftDot());
        pierceFlare = MakeCameraQuad("PierceFlare", pierceFlareMat, shotCamera.nearClipPlane * 3f);

        // Untextured, so it is a flat opaque field rather than a soft disc — the
        // blackout has to actually reach the corners of the frame.
        pierceBlackMat = MakeUnlit(additive: false);
        // Nearer than the flare: the cut has to land ON TOP of the white.
        pierceBlack = MakeCameraQuad("PierceBlack", pierceBlackMat, shotCamera.nearClipPlane * 2f);
        pierceBlack.gameObject.SetActive(false);
    }

    // A quad riding just in front of the lens. Sized to the frustum at that
    // distance, so it covers exactly what the camera can see.
    private Transform MakeCameraQuad(string name, Material mat, float distance)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        var col = q.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        q.transform.SetParent(camT, false);
        q.transform.localPosition = new Vector3(0f, 0f, Mathf.Max(distance, shotCamera.nearClipPlane * 1.2f));
        q.transform.localRotation = Quaternion.identity;
        return q.transform;
    }

    private void SizeToFrustum(Transform quad, float widthScale)
    {
        float d = quad.localPosition.z;
        float h = 2f * d * Mathf.Tan(shotCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * shotCamera.aspect;
        quad.localScale = new Vector3(w * widthScale, h * widthScale, 1f);
    }

    private void UpdatePierce(float t)
    {
        if (pierceFlare == null || burstStartedAt < 0f) return;

        float u = Mathf.Clamp01((t - burstStartedAt) / Mathf.Max(0.05f, pierceDuration));

        // Follow the burning point on screen so the flare grows OUT OF the crack
        // rather than out of the middle of the display. Offset the quad sideways
        // by where that point sits in the frustum.
        Vector3 vp = shotCamera.WorldToViewportPoint(pierceOrigin);
        float dz = pierceFlare.localPosition.z;
        float fh = 2f * dz * Mathf.Tan(shotCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float fw = fh * shotCamera.aspect;
        float ox = vp.z > 0f ? (vp.x - 0.5f) * fw : 0f;
        float oy = vp.z > 0f ? (vp.y - 0.5f) * fh : 0f;
        // Slide back to centre as it takes over — by the end it is the frame.
        float recentre = Mathf.SmoothStep(0f, 1f, u);
        pierceFlare.localPosition = new Vector3(ox * (1f - recentre), oy * (1f - recentre), dz);

        // Accelerating growth: light forcing its way through stone does not open
        // at a constant rate, it gives way.
        SizeToFrustum(pierceFlare, 4.5f * (u * u));

        Color c;
        if (u < 0.62f)
        {
            c = Color.Lerp(lightColor, coreColor, u / 0.62f);
            c.a = Mathf.Clamp01(u / 0.45f);
        }
        else
        {
            c = Color.Lerp(coreColor, Color.white, Mathf.Clamp01((u - 0.62f) / 0.20f));
            c.a = 1f;
        }
        SetQuadColor(pierceFlareMat, c);

        // The cut. Comes in over the last stretch, on top of the white.
        if (u > 0.80f)
        {
            if (!pierceBlack.gameObject.activeSelf) pierceBlack.gameObject.SetActive(true);
            SizeToFrustum(pierceBlack, 1.05f);
            SetQuadColor(pierceBlackMat, new Color(0f, 0f, 0f, Mathf.Clamp01((u - 0.80f) / 0.20f)));
        }
    }

    private static void SetQuadColor(Material m, Color c)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
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

            // Imported rock meshes carry no collider, so without this every chunk
            // falls straight through the ground instead of tumbling and settling.
            if (chunk.GetComponentInChildren<Collider>() == null)
            {
                var mf = chunk.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                }
            }

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
        if (pierceFlareMat != null) Destroy(pierceFlareMat);
        if (pierceBlackMat != null) Destroy(pierceBlackMat);
    }
}
