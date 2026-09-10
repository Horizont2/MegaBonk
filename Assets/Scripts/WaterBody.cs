using System.Collections.Generic;
using UnityEngine;

// Marks a water surface so gameplay can ask "is this position under water, and
// how deep?".
//
// There is more than one kind of water in the game: the generated world plane,
// and the lakes a self-contained location brings with it (the castle's sit about
// 2m above its own ground and follow it wherever it is placed). Neither has a
// collider — the generated plane's is deliberately destroyed — so nothing can be
// detected by physics. Registering the surfaces instead keeps one answer for
// both, and costs a loop over a handful of entries.
[DisallowMultipleComponent]
public class WaterBody : MonoBehaviour
{
    [Tooltip("Offset from this object's own Y to the actual water surface. 0 for a flat plane whose pivot is at the surface.")]
    public float surfaceOffset = 0f;

    [Tooltip("Extra metres added around the XZ bounds, so the shoreline test isn't razor thin.")]
    public float edgePadding = 0.5f;

    private static readonly List<WaterBody> s_all = new List<WaterBody>(8);
    private Renderer _renderer;

    private void OnEnable()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (!s_all.Contains(this)) s_all.Add(this);
    }

    private void OnDisable() { s_all.Remove(this); }

    public float SurfaceY => transform.position.y + surfaceOffset;

    private bool ContainsXZ(Vector3 pos)
    {
        if (_renderer == null) return false;
        Bounds b = _renderer.bounds;
        return pos.x >= b.min.x - edgePadding && pos.x <= b.max.x + edgePadding &&
               pos.z >= b.min.z - edgePadding && pos.z <= b.max.z + edgePadding;
    }

    // Highest water surface covering this XZ position. Highest, not first, so a
    // location's perched lake wins over the world sea below it.
    public static bool TrySurfaceAt(Vector3 pos, out float surfaceY)
    {
        surfaceY = 0f;
        bool found = false;
        for (int i = s_all.Count - 1; i >= 0; i--)
        {
            var w = s_all[i];
            if (w == null) { s_all.RemoveAt(i); continue; }
            if (!w.ContainsXZ(pos)) continue;
            float y = w.SurfaceY;
            if (!found || y > surfaceY) { surfaceY = y; found = true; }
        }
        return found;
    }

    // Convenience for anything that just wants to add this to an existing water
    // object at runtime (the generator, the location placer).
    public static WaterBody Attach(GameObject go, float offset = 0f)
    {
        if (go == null) return null;
        // Not '??' — that bypasses UnityEngine.Object's == overload, so a
        // destroyed component reads as non-null and the next access throws.
        var w = go.GetComponent<WaterBody>();
        if (w == null) w = go.AddComponent<WaterBody>();
        w.surfaceOffset = offset;
        return w;
    }

    // Find water nobody registered.
    //
    // Only the generated plane and a location's own lake are registered
    // explicitly. Any lake placed by hand in a scene has no WaterBody at all, so
    // TrySurfaceAt returns nothing, the swim state never engages, and the player
    // walks to the bottom exactly as if the water were not there. Rather than
    // require every lake to be tagged by hand, recognise water by its material.
    private static readonly string[] WaterWords = { "water", "lake", "river", "ocean", "sea", "pond" };

    public static int AutoRegisterSceneWater()
    {
        int added = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            if (r.GetComponent<WaterBody>() != null) continue;

            if (!LooksLikeWater(r)) continue;
            Attach(r.gameObject);
            added++;
        }
        if (added > 0) Debug.Log($"[Water] Auto-registered {added} unmarked water surface(s) — without this the player walks along the bottom instead of swimming.");
        return added;
    }

    private static bool LooksLikeWater(Renderer r)
    {
        if (Matches(r.gameObject.name)) return true;
        foreach (var m in r.sharedMaterials)
        {
            if (m == null) continue;
            if (Matches(m.name)) return true;
            if (m.shader != null && Matches(m.shader.name)) return true;
        }
        return false;
    }

    private static bool Matches(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        string n = s.ToLowerInvariant();
        foreach (var w in WaterWords) if (n.Contains(w)) return true;
        return false;
    }
}
