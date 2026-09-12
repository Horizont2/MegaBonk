using System.Collections.Generic;
using UnityEngine;

// Every marker the map is allowed to show, and how close you must be to earn it.
//
// ==== WHY A RADIUS PER EVENT AND NOT ONE GLOBAL NUMBER ====
//
// A map that shows everything is a checklist, and a map that shows nothing means
// the rarest content in the game is never found. A Barrow appears in roughly one
// region in five and its light carries about thirty metres — the arithmetic says
// a player would meet one every few hours, by luck. That is not scarcity, it is
// absence.
//
// The reveal radius is the dial between those two failures, and it has to be per
// event because the events are not the same KIND of thing:
//
//   A prisoner in a cage is a favour the world is asking of you. It should be
//   findable — a wide radius — because missing one costs the player a companion
//   they would have enjoyed and teaches them nothing.
//
//   A wayside chest is a small bonus. A short radius: you find it because you
//   were already walking that way, which is what makes it feel incidental.
//
//   A Barrow is the thing you change your route for. It wants a WIDE radius but
//   a vague one — the player should learn "there is something over there" from
//   far off and still have to go and look. Distance is the cost of the reward.
//
// The icons themselves are assigned in one place (Tools > World > Map Event
// Icons) rather than scattered across a dozen prefabs, because the whole point
// of an icon set is that the icons agree with each other, and you cannot judge
// that while looking at them one prefab at a time.
[CreateAssetMenu(fileName = "MapEventIcons", menuName = "World/Map Event Icons")]
public class MapEventIcons : ScriptableObject
{
    public const string ResourceName = "MapEventIcons";

    // Kinds, not prefabs. A kind is a promise to the player about what they will
    // find; two different cage prefabs are still one promise.
    public enum Kind
    {
        CagedAlly,
        ChestWayside,
        ChestShrine,
        ChestBarrow,
        Altar,
        Extraction,
        Camp,
        Watchtower,
        ResourceNode,
    }

    [System.Serializable]
    public class Entry
    {
        public Kind kind;
        public Sprite icon;

        [Tooltip("Metres at which the marker appears. Think of it as 'how far away should the player learn this exists'. Zero hides the marker entirely — a legitimate choice for anything that should only ever be found by looking.")]
        public float revealRadius = 120f;

        [Tooltip("Tint. Keep these consistent with the world: the chest beacons are green/blue/gold by grade, so their markers should be too, or the map is teaching a second colour language.")]
        public Color tint = Color.white;

        [Tooltip("Pixel size on the minimap at the reference resolution.")]
        public float size = 26f;

        [Tooltip("Fade the marker in over the last few metres of the reveal radius instead of popping it on. A marker that appears instantly reads as a bug the first few times.")]
        public float fadeBand = 40f;

        [Tooltip("Keep showing it once seen, even after walking back out of range. Right for anything the player might want to return to; wrong for anything that is only interesting while you are near it.")]
        public bool rememberOnceSeen = true;
    }

    // Defaults chosen so the asset is useful the moment it is created, and so the
    // reasoning above is visible as numbers rather than only as prose.
    public List<Entry> entries = new List<Entry>
    {
        new Entry { kind = Kind.CagedAlly,     revealRadius = 260f, tint = new Color(1f, 0.85f, 0.45f), size = 28f },
        new Entry { kind = Kind.ChestWayside,  revealRadius = 140f,  tint = new Color(0.42f, 1f, 0.52f), size = 22f },
        new Entry { kind = Kind.ChestShrine,   revealRadius = 220f, tint = new Color(0.34f, 0.62f, 1f), size = 26f },
        new Entry { kind = Kind.ChestBarrow,   revealRadius = 380f, tint = new Color(1f, 0.82f, 0.32f), size = 32f },
        new Entry { kind = Kind.Altar,         revealRadius = 300f, tint = new Color(0.85f, 0.45f, 1f), size = 28f },
        new Entry { kind = Kind.Extraction,    revealRadius = 600f, tint = new Color(0.7f, 0.9f, 1f),   size = 30f },
        new Entry { kind = Kind.Camp,          revealRadius = 190f, tint = new Color(1f, 0.55f, 0.35f), size = 24f },
        new Entry { kind = Kind.Watchtower,    revealRadius = 270f, tint = new Color(0.9f, 0.4f, 0.4f), size = 26f },
        new Entry { kind = Kind.ResourceNode,  revealRadius = 0f,   tint = Color.white,                 size = 18f },
    };

    private Dictionary<Kind, Entry> _byKind;

    public Entry For(Kind kind)
    {
        if (_byKind == null)
        {
            _byKind = new Dictionary<Kind, Entry>(entries.Count);
            foreach (var e in entries) if (e != null) _byKind[e.kind] = e;
        }
        return _byKind.TryGetValue(kind, out var found) ? found : null;
    }

    private static MapEventIcons _cached;
    private static bool _searched;

    public static MapEventIcons Load()
    {
        if (_searched) return _cached;
        _searched = true;
        _cached = Resources.Load<MapEventIcons>(ResourceName);
        if (_cached == null)
            Debug.LogWarning($"[MapIcons] No '{ResourceName}' in a Resources folder — no event markers will show. " +
                             "Create it with Tools > World > Map Event Icons.");
        return _cached;
    }

    public static void ClearCache() { _cached = null; _searched = false; }
}
