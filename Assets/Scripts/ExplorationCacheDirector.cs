using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Decides which of the region's chests are worth crossing the map for.
//
// ==== WHY IT PROMOTES RATHER THAN PLACES ====
//
// Camp_POI already carries a LootChest and the generator already scatters it, so
// the region is full of chests before this runs. Adding a second placement pass
// would mean a second set of prefab references to wire, a second set of rules
// about where things may sit, and two systems that can disagree about how
// crowded the map is.
//
// Instead this takes what is already there and promotes a HANDFUL of them into
// marked caches. Everything else stays an ordinary chest — which matters more
// than it sounds: if every chest had a beacon over it the beacons would mean
// nothing, and the map would read as a checklist rather than a place with
// something hidden in it. Most chests should be things you stumble on. A few
// should be things you go after.
//
// Distance from the player's start does the tiering. A cache out on the far edge
// is worth more than one behind the first ridge, which is the only honest way to
// pay someone for the walk.
[DisallowMultipleComponent]
public class ExplorationCacheDirector : MonoBehaviour
{
    [Header("How many are worth marking")]
    [Tooltip("Upper bound on beacon caches. Small on purpose — their value comes from being rare.")]
    public int maxCaches = 9;
    [Tooltip("Never promote a chest nearer the player's start than this. A landmark you can see from the spawn point is not a discovery.")]
    public float minDistanceFromStart = 70f;
    [Tooltip("Keep marked caches this far apart so their lights do not cluster into one glow.")]
    public float minSpacing = 90f;

    [Header("Payout")]
    [Tooltip("Richness at the closest promoted cache.")]
    public float nearRichness = 1f;
    [Tooltip("Richness at the furthest. The walk is the price; this is what pays it.")]
    public float farRichness = 2.2f;

    public static int Found { get; private set; }
    public static int Total { get; private set; }

    private static readonly List<ExplorationCache> s_live = new List<ExplorationCache>(16);

    private void Start() => StartCoroutine(PromoteWhenWorldExists());

    private IEnumerator PromoteWhenWorldExists()
    {
        // Chests arrive with the POI pass, which is part of generation. Promoting
        // before that finishes would find an empty map and quietly do nothing —
        // the failure mode here is silence, so it waits for a definite signal
        // rather than a guessed number of frames.
        float deadline = Time.time + 90f;
        while (!WorldGenerator.IsGenerationDone && Time.time < deadline) yield return null;
        yield return null;

        Promote();
    }

    private void Promote()
    {
        s_live.Clear();
        Found = 0;
        Total = 0;

        var chests = FindObjectsByType<LootChest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (chests == null || chests.Length == 0)
        {
            Debug.Log("[Caches] No chests in the region to promote. Nothing to explore for.");
            return;
        }

        Vector3 start = Vector3.zero;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) start = pc.transform.position;

        // Furthest first: the best caches should be the ones that cost the most
        // to reach, and taking them in this order means the spacing rule spends
        // its budget on the far ones rather than filling up near the spawn.
        var candidates = new List<LootChest>();
        foreach (var c in chests)
        {
            if (c == null || c.IsOpened) continue;
            if (c.GetComponent<ExplorationCache>() != null) continue;
            if ((c.transform.position - start).sqrMagnitude < minDistanceFromStart * minDistanceFromStart) continue;
            candidates.Add(c);
        }
        candidates.Sort((a, b) =>
            (b.transform.position - start).sqrMagnitude.CompareTo((a.transform.position - start).sqrMagnitude));

        if (candidates.Count == 0)
        {
            Debug.Log($"[Caches] {chests.Length} chest(s) found but all are within {minDistanceFromStart}m of the " +
                      "player's start, so none were promoted. Lower minDistanceFromStart if the map is small.");
            return;
        }

        float nearest = Mathf.Sqrt((candidates[candidates.Count - 1].transform.position - start).sqrMagnitude);
        float furthest = Mathf.Sqrt((candidates[0].transform.position - start).sqrMagnitude);

        var taken = new List<Vector3>(maxCaches);
        int kindCursor = Random.Range(0, 3);

        foreach (var chest in candidates)
        {
            if (taken.Count >= maxCaches) break;

            bool tooClose = false;
            foreach (var t in taken)
                if ((chest.transform.position - t).sqrMagnitude < minSpacing * minSpacing) { tooClose = true; break; }
            if (tooClose) continue;

            var cache = chest.gameObject.AddComponent<ExplorationCache>();

            // Rotate through the three kinds rather than rolling each one, so a
            // region cannot hand out five Armoury caches and no supplies. The
            // starting offset is random, so which kind is nearest still varies.
            cache.kind = (ExplorationCache.Kind)(kindCursor++ % 3);

            float d = Vector3.Distance(chest.transform.position, start);
            float t01 = furthest > nearest + 1f ? Mathf.InverseLerp(nearest, furthest, d) : 0.5f;
            cache.richness = Mathf.Lerp(nearRichness, farRichness, t01);

            // The nearest couple stay quiet. A player's first cache should teach
            // them that the light means treasure, not that it means an ambush.
            cache.noisyToOpen = t01 > 0.35f;

            taken.Add(chest.transform.position);
            s_live.Add(cache);
        }

        Total = s_live.Count;
        Debug.Log($"[Caches] Promoted {Total} of {chests.Length} chest(s) into marked caches " +
                  $"({nearest:F0}m to {furthest:F0}m from start).");
    }

    public static void NoteFound(ExplorationCache cache)
    {
        if (cache == null || !s_live.Contains(cache)) return;
        s_live.Remove(cache);
        Found++;

        // Acknowledge it. A discovery nobody counts is a discovery the player
        // stops making — and this is the cheapest motivator that exists.
        ToastManager.Show(LocalizationManager.Tr("CACHE_FOUND", Found, Total),
                          ToastManager.ToastKind.Achievement);
    }

    // Statics outlive a scene load, so without this the next region opens
    // claiming the player has already found everything in it.
    public static void Reset()
    {
        s_live.Clear();
        Found = 0;
        Total = 0;
    }

    // Self-installing, like the rest of the region's ambient systems, so no
    // scene has to carry a component for it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!WorldEncounterDirector.IsAnyRegionMode()) return;
        if (FindFirstObjectByType<ExplorationCacheDirector>() != null) return;
        Reset();
        new GameObject("[ExplorationCaches]").AddComponent<ExplorationCacheDirector>();
    }
}
