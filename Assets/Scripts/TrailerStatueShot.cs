using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trailer, shot 1 (0:00-0:10) — the king's statue breaks open and something
// gets out.
//
// The beat sheet, and why each piece is built the way it is:
//
//   PUSH-IN. The camera creeps toward the statue for the whole shot and never
//   cuts. A slow continuous push is what makes an audience lean in; cutting
//   closer would say "here is another angle" instead of "something is about to
//   happen". A little handheld drift keeps it from reading as a turntable.
//
//   CRACKS. They open one at a time, not all at once, with the light behind
//   them rising between each. A staggered break has a rhythm the viewer can
//   feel building; a single simultaneous shatter is over before it registers.
//
//   RAYS. Shafts spear out of each crack and streak PAST the camera, deliberately
//   not into it: a shaft aimed exactly at the lens is foreshortened to a dot and
//   reads as nothing. Aiming just wide of the lens is what makes them sweep
//   across frame and sell that the light is coming at you.
//
//   "DARK LIGHT". Light that darkens is not a thing a renderer can do, so this
//   is done the way film does it — by contrast. The shafts are a deep saturated
//   violet with a near-white core, and everything around them desaturates and
//   drops as they grow. The light gets brighter while the WORLD gets darker,
//   which is what reads as wrong, and therefore as dread.
//
//   COLLAPSE. The statue is one mesh, so it cannot really shatter. It does not
//   need to: it shakes, sheds real debris chunks and dust, and the shot cuts on
//   the flare at the peak. What the audience is shown is the light winning; the
//   rubble is implied.
//
// Everything runs on UNSCALED time so a stray Time.timeScale can't stall the
// shot, and every generated material is built explicitly — a ParticleSystem or
// LineRenderer created from script has no material and renders magenta.
[DisallowMultipleComponent]
public class TrailerStatueShot : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("The statue. Its renderers' combined bounds drive framing and crack placement.")]
    public Transform statue;
    [Tooltip("Camera to drive. Leave empty to use Camera.main.")]
    public Camera shotCamera;

    [Header("Timing (seconds)")]
    public float holdBeforeFirstCrack = 2.2f;
    public float crackInterval = 0.85f;
    public float burstHold = 1.1f;
    public float outFade = 0.7f;
    [Tooltip("Total shot length. The push-in is spread across all of it.")]
    public float shotDuration = 10f;

    [Header("Camera Move")]
    [Tooltip("Distance from the statue's centre at the start, in metres.")]
    public float startDistance = 14f;
    [Tooltip("Distance at the end. The gap between the two IS the push.")]
    public float endDistance = 5.5f;
    public float startHeight = 3.2f;
    public float endHeight = 2.2f;
    [Tooltip("Degrees the camera drifts around the statue across the shot. Small — this is a push, not an orbit.")]
    public float orbitDrift = 6f;
    public float startFov = 38f;
    public float endFov = 46f;
    [Tooltip("Handheld amplitude in metres. Keep tiny; this is a tripod with a pulse, not a shoulder rig.")]
    public float handheld = 0.035f;

    [Header("Cracks & Rays")]
    [Range(2, 16)] public int crackCount = 7;
    [Tooltip("Deep violet core of the light. Bright enough to bloom.")]
    public Color lightColor = new Color(0.55f, 0.18f, 0.95f, 1f);
    [Tooltip("The hot centre of each shaft.")]
    public Color coreColor = new Color(0.92f, 0.82f, 1f, 1f);
    public float rayLength = 26f;
    public float rayWidth = 0.5f;
    [Tooltip("How far past the lens the shafts are aimed. 0 would point them straight down the barrel, where they vanish.")]
    public float rayLensOffset = 3.2f;
    public float lightIntensity = 60f;

    [Header("Collapse")]
    [Tooltip("Rock meshes flung from the statue at the peak. Assets/Locations/fbx2/LProck*.fbx work well.")]
    public GameObject[] debrisPrefabs;
    [Range(0, 40)] public int debrisCount = 14;
    public float shakeAmplitude = 0.09f;

    [Header("Audio")]
    public string rumbleSound = AudioID.Region_Shockwave;
    public string burstSound = AudioID.Region_Shockwave;

    private readonly List<CrackEmitter> cracks = new List<CrackEmitter>();
    private Transform camT;
    private Bounds statueBounds;
    private Vector3 statueCenter;
    private float baseAzimuth;
    private Material rayMaterial;

    private class CrackEmitter
    {
        public Vector3 worldPos;
        public Vector3 normal;
        public Light glow;
        public LineRenderer[] rays;
        public ParticleSystem dust;
        public float openedAt = -1f;
    }

    private void Start()
    {
        if (statue == null) { Debug.LogWarning("[StatueShot] No statue assigned — nothing to play."); enabled = false; return; }

        shotCamera = shotCamera != null ? shotCamera : Camera.main;
        if (shotCamera == null) { Debug.LogWarning("[StatueShot] No camera."); enabled = false; return; }
        camT = shotCamera.transform;

        statueBounds = ComputeBounds(statue);
        statueCenter = statueBounds.center;

        // Approach from whatever side the camera was already placed on, so a
        // hand-dressed shot keeps its composition instead of snapping somewhere
        // arbitrary.
        Vector3 flat = camT.position - statueCenter;
        flat.y = 0f;
        baseAzimuth = flat.sqrMagnitude > 0.01f
            ? Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg
            : Random.Range(0f, 360f);

        BuildRayMaterial();
        BuildCracks();
        StartCoroutine(PlayShot());
    }

    private static Bounds ComputeBounds(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(root.position, Vector3.one * 4f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // Additive, so shafts brighten whatever they cross instead of flatly
    // covering it. Built here rather than authored so the component drops onto
    // any statue with no prefab work.
    private void BuildRayMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        rayMaterial = new Material(sh);
        if (rayMaterial.HasProperty("_Surface"))
        {
            rayMaterial.SetFloat("_Surface", 1f);              // transparent
            rayMaterial.SetFloat("_Blend", 2f);                // additive
            rayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            rayMaterial.SetInt("_ZWrite", 0);
            rayMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            rayMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        rayMaterial.color = Color.white;   // the gradient carries the colour
    }

    // Crack points are raycast onto the statue's actual surface rather than
    // scattered in its bounding box, so the light comes out of the stone and not
    // out of the air beside it.
    private void BuildCracks()
    {
        float radius = Mathf.Max(statueBounds.extents.x, statueBounds.extents.z) + 4f;

        for (int i = 0; i < crackCount; i++)
        {
            // Spread around the statue but bias to the camera-facing side and to
            // the upper body, where the audience is already looking.
            float ang = baseAzimuth + Random.Range(-95f, 95f);
            float h = Mathf.Lerp(statueBounds.min.y + statueBounds.size.y * 0.35f,
                                 statueBounds.max.y - statueBounds.size.y * 0.08f,
                                 Random.value);

            Vector3 from = new Vector3(
                statueCenter.x + Mathf.Cos(ang * Mathf.Deg2Rad) * radius,
                h,
                statueCenter.z + Mathf.Sin(ang * Mathf.Deg2Rad) * radius);
            Vector3 dir = new Vector3(statueCenter.x - from.x, 0f, statueCenter.z - from.z).normalized;

            Vector3 pos;
            Vector3 nrm;
            if (Physics.Raycast(from, dir, out RaycastHit hit, radius * 2.5f, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform.IsChildOf(statue))
            {
                pos = hit.point + hit.normal * 0.05f;
                nrm = hit.normal;
            }
            else
            {
                // No collider on the statue mesh — fall back to the bounds shell.
                nrm = -dir;
                pos = new Vector3(statueCenter.x, h, statueCenter.z) - dir * (radius * 0.28f);
            }

            cracks.Add(BuildEmitter(pos, nrm, i));
        }
    }

    private CrackEmitter BuildEmitter(Vector3 pos, Vector3 normal, int index)
    {
        var e = new CrackEmitter { worldPos = pos, normal = normal };

        var root = new GameObject($"Crack_{index}");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        var lgo = new GameObject("Glow");
        lgo.transform.SetParent(root.transform, false);
        e.glow = lgo.AddComponent<Light>();
        e.glow.type = LightType.Point;
        e.glow.color = lightColor;
        e.glow.range = 9f;
        e.glow.intensity = 0f;
        e.glow.shadows = LightShadows.None;

        // Two shafts per crack, splayed slightly, so a crack reads as a tear
        // rather than a laser pointer.
        e.rays = new LineRenderer[2];
        for (int r = 0; r < e.rays.Length; r++)
        {
            var rgo = new GameObject($"Ray_{r}");
            rgo.transform.SetParent(root.transform, false);
            var lr = rgo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material = rayMaterial;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.widthCurve = AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f); // narrow at the stone, wide at the lens
            lr.widthMultiplier = 0f;
            e.rays[r] = lr;
        }

        e.dust = BuildDust(root.transform);
        return e;
    }

    private ParticleSystem BuildDust(Transform parent)
    {
        var go = new GameObject("Dust");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 3f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 1.8f;
        main.startSpeed = 2.2f;
        main.startSize = 0.35f;
        main.startColor = new Color(0.55f, 0.5f, 0.6f, 0.55f);
        main.gravityModifier = 0.25f;
        main.useUnscaledTime = true;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.12f;

        // A script-created ParticleSystem has NO material and renders magenta.
        var pr = ps.GetComponent<ParticleSystemRenderer>();
        Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psh == null) psh = Shader.Find("Sprites/Default");
        if (psh != null) pr.material = new Material(psh);
        else pr.enabled = false;

        ps.Stop();
        return ps;
    }

    private IEnumerator PlayShot()
    {
        var polish = TrailerCinematicPolish.GetOrCreate();
        polish.OpenTrailer();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(rumbleSound))
            AudioManager.Instance.PlaySFX3D(rumbleSound, statueCenter);

        float t = 0f;
        int opened = 0;
        float nextCrackAt = holdBeforeFirstCrack;
        bool burst = false;
        float burstAt = holdBeforeFirstCrack + crackInterval * crackCount;

        while (t < shotDuration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            UpdateCamera(t / shotDuration, t);

            if (opened < crackCount && t >= nextCrackAt)
            {
                OpenCrack(cracks[opened], t);
                opened++;
                nextCrackAt += crackInterval;
            }

            // Light rises the whole time; the world falls away underneath it.
            float dread = Mathf.Clamp01((t - holdBeforeFirstCrack) / Mathf.Max(0.01f, burstAt - holdBeforeFirstCrack));
            UpdateRays(t, dread, burst);

            if (!burst && t >= burstAt)
            {
                burst = true;
                StartCoroutine(BurstRoutine());
            }

            yield return null;
        }

        polish.FadeToBlack(outFade);
    }

    private void UpdateCamera(float u01, float t)
    {
        // Smootherstep: no velocity AND no acceleration at either end, so the
        // push never announces its start or its stop.
        float e = u01 * u01 * u01 * (u01 * (u01 * 6f - 15f) + 10f);

        float az = (baseAzimuth + Mathf.Lerp(0f, orbitDrift, e)) * Mathf.Deg2Rad;
        float dist = Mathf.Lerp(startDistance, endDistance, e);
        float h = Mathf.Lerp(startHeight, endHeight, e);

        Vector3 pos = new Vector3(
            statueCenter.x + Mathf.Cos(az) * dist,
            statueBounds.min.y + h,
            statueCenter.z + Mathf.Sin(az) * dist);

        // Handheld: two incommensurate sine pairs, so it never repeats visibly.
        pos += camT.right * (Mathf.Sin(t * 1.31f) * handheld)
             + camT.up * (Mathf.Sin(t * 0.87f + 1.7f) * handheld * 0.7f);

        camT.position = pos;

        Vector3 lookAt = new Vector3(statueCenter.x,
                                     Mathf.Lerp(statueCenter.y, statueBounds.max.y - statueBounds.size.y * 0.2f, e),
                                     statueCenter.z);
        camT.rotation = Quaternion.LookRotation((lookAt - pos).normalized);
        shotCamera.fieldOfView = Mathf.Lerp(startFov, endFov, e);
    }

    private void OpenCrack(CrackEmitter e, float t)
    {
        e.openedAt = t;
        if (e.dust != null) e.dust.Play();
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(rumbleSound))
            AudioManager.Instance.PlaySFX3D(rumbleSound, e.worldPos);
        TrailerCinematicPolish.GetOrCreate().ImpactPunch(0.25f, 0.2f);
    }

    private void UpdateRays(float t, float dread, bool burst)
    {
        Vector3 camPos = camT.position;

        for (int i = 0; i < cracks.Count; i++)
        {
            var e = cracks[i];
            if (e.openedAt < 0f) continue;

            float age = t - e.openedAt;
            float grow = Mathf.Clamp01(age / 1.1f);
            float flare = burst ? 1f + Mathf.Sin(t * 22f) * 0.25f : 1f;

            e.glow.intensity = lightIntensity * grow * (0.35f + dread) * flare;

            for (int r = 0; r < e.rays.Length; r++)
            {
                LineRenderer lr = e.rays[r];

                // Aim just wide of the lens. Straight at it and the shaft
                // collapses to a point on screen.
                float side = (r == 0) ? 1f : -1f;
                Vector3 aim = camPos
                            + camT.right * (rayLensOffset * side * (0.6f + 0.4f * Mathf.Sin(t * 0.9f + i)))
                            + camT.up * (rayLensOffset * 0.35f * Mathf.Sin(t * 1.13f + i * 2.1f))
                            - camT.forward * 4f;   // carry it PAST the camera

                Vector3 dir = (aim - e.worldPos).normalized;
                lr.SetPosition(0, e.worldPos);
                lr.SetPosition(1, e.worldPos + dir * rayLength * grow);

                lr.widthMultiplier = rayWidth * grow * (burst ? 1.9f : 1f);

                Color hot = Color.Lerp(lightColor, coreColor, 0.35f + 0.4f * dread);
                hot.a = Mathf.Clamp01(grow * (0.35f + 0.65f * dread)) * (burst ? 1f : 0.8f);
                lr.startColor = hot;
                Color tail = hot; tail.a = 0f;   // fade out toward the lens
                lr.endColor = tail;
            }
        }
    }

    private IEnumerator BurstRoutine()
    {
        var polish = TrailerCinematicPolish.GetOrCreate();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(burstSound))
            AudioManager.Instance.PlaySFX3D(burstSound, statueCenter);

        polish.ImpactPunch(1f, 0.6f);
        polish.TimeRamp(0.35f, burstHold, 0.05f, 0.4f);

        SpawnDebris();

        // Shake the statue itself rather than only the camera: a camera-only
        // shake says the operator was startled, a shaking subject says the stone
        // is failing.
        Vector3 home = statue.position;
        float st = 0f;
        while (st < burstHold)
        {
            st += Time.unscaledDeltaTime;
            statue.position = home + Random.insideUnitSphere * shakeAmplitude;
            yield return null;
        }
        statue.position = home;
    }

    private void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0 || debrisCount <= 0) return;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (prefab == null) continue;

            Vector3 from = cracks.Count > 0
                ? cracks[Random.Range(0, cracks.Count)].worldPos
                : statueCenter;

            GameObject chunk = Instantiate(prefab, from, Random.rotation);
            chunk.transform.localScale *= Random.Range(0.08f, 0.22f);

            var rb = chunk.GetComponent<Rigidbody>() ?? chunk.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            Vector3 away = (from - statueCenter).normalized + Vector3.up * 0.6f;
            rb.linearVelocity = away * Random.Range(3.5f, 7f) + Random.insideUnitSphere * 1.2f;
            rb.angularVelocity = Random.insideUnitSphere * 6f;

            Destroy(chunk, 6f);
        }
    }
}
