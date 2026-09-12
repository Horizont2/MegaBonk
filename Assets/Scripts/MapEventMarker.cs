using System.Collections.Generic;
using UnityEngine;

// "There is one of these here." Drop it on any event prefab.
//
// It holds no icon, no colour and no radius of its own — only which KIND of
// thing this is. Everything about how it looks and how close you must be lives
// in the MapEventIcons asset, so the whole set can be tuned in one window
// against itself. An icon set is only coherent when you can see it all at once,
// and a radius only makes sense next to the other radii.
[DisallowMultipleComponent]
public class MapEventMarker : MonoBehaviour
{
    public MapEventIcons.Kind kind = MapEventIcons.Kind.ChestWayside;

    [Tooltip("Overrides the reveal radius from the icon set for this one instance. -1 uses the set's value, which is almost always what you want — a per-instance radius is how a set stops being consistent.")]
    public float revealRadiusOverride = -1f;

    [Tooltip("Hide the marker once the event is finished — a looted chest, a freed prisoner. Left on, the map keeps pointing at somewhere with nothing in it.")]
    public bool hideWhenDone = true;

    [HideInInspector] public bool done;
    [HideInInspector] public bool seen;

    // A live registry rather than a scene search per frame. These are created
    // during generation and destroyed as they are used, so the list is short and
    // the layer that draws them should never have to go looking.
    public static readonly List<MapEventMarker> All = new List<MapEventMarker>(32);

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    public void MarkDone()
    {
        done = true;
        if (hideWhenDone) All.Remove(this);
    }

    public float RevealRadius(MapEventIcons set)
    {
        if (revealRadiusOverride >= 0f) return revealRadiusOverride;
        var e = set != null ? set.For(kind) : null;
        return e != null ? e.revealRadius : 0f;
    }
}
