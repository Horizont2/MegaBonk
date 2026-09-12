using UnityEngine;

// Turns a windmill's sails.
//
// ==== WHY IT READS THE WIND INSTEAD OF JUST SPINNING ====
//
// A constant rotation is one line and it looks wrong within about ten seconds:
// the grass is bending in gusts, the trees are swaying, and the one object on
// the skyline whose entire purpose is to show which way the wind blows turns at
// a metronome. The eye catches that disagreement long before it can name it.
//
// DynamicWind already publishes the world's wind as global shader properties so
// every foliage shader sways together. This reads the same numbers from C#, so
// the sails speed up in a gust and coast in the lull along with everything else
// — with no reference to wire up, nothing to find at runtime, and no coupling to
// a component that may not exist in a given scene. When there is no DynamicWind
// the global is zero and the mill turns at its idle speed, which is fine.
//
// The other half is INERTIA. Sails carry real mass, so the speed is damped
// towards the wind rather than set from it; a mill that changes pace instantly
// reads as a spinning texture rather than a machine.
[DisallowMultipleComponent]
public class WindmillSpin : MonoBehaviour
{
    public enum Axle { Auto, X, Y, Z }

    [Header("Axle")]
    [Tooltip("Which local axis the sails turn around. Auto measures the mesh and picks its THINNEST axis — a set of sails is a flat disc, so the thin direction is the axle. Set it by hand only if Auto guesses wrong on an odd model.")]
    public Axle axle = Axle.Auto;
    public bool reverse = false;

    [Header("Speed")]
    [Tooltip("Degrees per second in a strong wind.")]
    public float maxDegreesPerSecond = 55f;
    [Tooltip("Fraction of full speed the sails keep in dead air. Never zero: a stopped mill reads as broken scenery, and the whole point of the prop is motion on the skyline.")]
    [Range(0f, 1f)] public float idleFraction = 0.18f;
    [Tooltip("Wind strength that counts as 'full'. DynamicWind gusts up to about 1.2.")]
    public float windAtFullSpeed = 1.0f;
    [Tooltip("Seconds for the sails to take up a change in the wind. Sails are heavy — snapping to a new speed looks like a video playing faster.")]
    public float inertia = 2.5f;

    [Header("Physics")]
    [Tooltip("Switch off a non-convex MeshCollider on the sails. A static collider that MOVES every frame makes PhysX re-cook the geometry every frame, which is one of the most expensive things a decorative prop can do — and nothing ever needs to collide with blades turning seven metres up. Untick only if something really does have to hit them.")]
    public bool disableSpinningMeshCollider = true;

    [Header("Character")]
    [Tooltip("Small per-mill speed offset, rolled once on start, so two mills in view never turn in lockstep.")]
    [Range(0f, 0.5f)] public float variance = 0.15f;

    private Vector3 _axis = Vector3.forward;
    private float _speed;      // current, degrees/sec
    private float _speedVel;   // SmoothDamp state
    private float _tempo = 1f;

    private void Start()
    {
        _axis = ResolveAxis();
        if (reverse) _axis = -_axis;
        _tempo = 1f + Random.Range(-variance, variance);
        _speed = maxDegreesPerSecond * idleFraction * _tempo;

        if (!disableSpinningMeshCollider) return;
        var mc = GetComponent<MeshCollider>();
        if (mc == null || !mc.enabled || mc.convex) return;
        mc.enabled = false;
        Debug.Log($"[WindmillSpin] Switched off the non-convex MeshCollider on '{name}'. Moving one every frame " +
                  "forces PhysX to re-cook it every frame; the sails do not need to be solid. Untick " +
                  "disableSpinningMeshCollider if something really has to collide with them.", this);
    }

    private void Update()
    {
        // .w carries the strength; see DynamicWind.Update. Absent DynamicWind the
        // vector is zero, which lands on the idle speed rather than a standstill.
        float wind = Shader.GetGlobalVector("_GlobalWindDir").w;
        float target = maxDegreesPerSecond * _tempo
                     * Mathf.Lerp(idleFraction, 1f, Mathf.Clamp01(wind / Mathf.Max(0.01f, windAtFullSpeed)));

        _speed = Mathf.SmoothDamp(_speed, target, ref _speedVel, Mathf.Max(0.05f, inertia));
        transform.Rotate(_axis, _speed * Time.deltaTime, Space.Self);
    }

    // The axle is the direction the sails are FLAT in.
    //
    // Measuring it beats hard-coding one, because whether a given windmill's
    // sails lie in XY or XZ depends entirely on how the artist exported the FBX,
    // and a hard-coded axis silently makes the sails wobble like a coin instead
    // of turning like a wheel. The mesh's own bounds answer it: a disc is thin in
    // exactly one direction, and that direction is the shaft.
    private Vector3 ResolveAxis()
    {
        switch (axle)
        {
            case Axle.X: return Vector3.right;
            case Axle.Y: return Vector3.up;
            case Axle.Z: return Vector3.forward;
        }

        var mf = GetComponent<MeshFilter>();
        Mesh mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null)
        {
            var skin = GetComponent<SkinnedMeshRenderer>();
            if (skin != null) mesh = skin.sharedMesh;
        }
        if (mesh == null) return Vector3.forward;

        Vector3 s = mesh.bounds.size;
        if (s.x <= s.y && s.x <= s.z) return Vector3.right;
        if (s.y <= s.x && s.y <= s.z) return Vector3.up;
        return Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 a = Application.isPlaying ? _axis : ResolveAxis();
        Gizmos.color = new Color(0.4f, 0.9f, 1f);
        Gizmos.DrawLine(transform.position - transform.TransformDirection(a) * 3f,
                        transform.position + transform.TransformDirection(a) * 3f);
    }
}
