using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Decides whether a region has a reliquary in it, and where.
//
// ==== SCARCITY IS THE FEATURE ====
//
// The numbers below are small on purpose, and they are the part of this system
// most worth defending. A reliquary is not a collectible to be swept up; it is
// supposed to be the thing you spot on the ridge and change your route for. Put
// six in a region and within two regions the player is walking a circuit, the
// silhouette stops meaning anything, and the armour economy is gone in an
// afternoon.
//
// So most regions have one. Some have none. A legendary site is genuinely rare,
// and even when one exists it still has to be found.
//
// It also PLACES rather than promoting an existing chest. The old version hung
// itself off Camp_POI, which meant the reward sites were wherever the generator
// happened to drop a camp — no control over spacing, over distance from the
// start, or over whether the site could be seen at all.
[DisallowMultipleComponent]
public class ReliquaryDirector : MonoBehaviour
{
    [Header("How many exist at all")]
    [Tooltip("Chance this region contains any reliquary. Below 1 on purpose: a region with nothing in it is what makes the next one's silhouette worth noticing.")]
    [Range(0f, 1f)] public float regionHasOneChance = 0.75f;
    [Tooltip("Upper bound when the roll succeeds. Two is a lot already.")]
    public int maxPerRegion = 2;
    [Tooltip("Chance a placed site is a guarded Shrine rather than a plain wayside find.")]
    [Range(0f, 1f)] public float shrineChance = 0.40f;
    [Tooltip("Chance a placed site is a Barrow — four guardians, a nine-second hold and a wave. The rarest thing in the system; treat any increase as an economy change, not a tuning tweak.")]
    [Range(0f, 0.4f)] public float barrowChance = 0.12f;

    [Header("Where")]
    [Tooltip("Never nearer the player's start than this — a landmark visible from spawn is not a discovery.")]
    public float minDistanceFromStart = 110f;
    public float minSpacing = 140f;
    [Tooltip("Clear ground needed around the site so the banners are not standing inside a tree.")]
    public float clearRadius = 7f;
    public float edgeMargin = 90f;

    public static int Placed { get; private set; }
    public static int Opened { get; private set; }

    private static readonly List<Reliquary> s_live = new List<Reliquary>(4);

    private void Start() => StartCoroutine(PlaceWhenWorldExists());

    private IEnumerator PlaceWhenWorldExists()
    {
        // Sites are chosen against the finished terrain and the finished tree
        // cover, so this waits for a definite signal rather than a guessed number
        // of frames — the failure mode here is silence.
        float deadline = Time.time + 90f;
        while (!WorldGenerator.IsGenerationDone && Time.time < deadline) yield return null;
        yield return null;

        Place();
    }

    private void Place()
    {
        s_live.Clear();
        Placed = 0;
        Opened = 0;

        var set = ReliquarySet.Load();
        if (set == null || !set.IsUsable)
        {
            Debug.LogWarning("[Reliquary] Set missing or has no chest prefab — nothing placed.");
            return;
        }

        if (Random.value > regionHasOneChance)
        {
            Debug.Log("[Reliquary] This region has none. That is by design — see ReliquaryDirector.");
            return;
        }

        Vector3 start = Vector3.zero;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) start = pc.transform.position;

        int want = Random.Range(1, Mathf.Max(1, maxPerRegion) + 1);
        var taken = new List<Vector3>(want);

        for (int attempt = 0; attempt < 400 && taken.Count < want; attempt++)
        {
            if (!TryFindSite(start, taken, out Vector3 site)) continue;

            var go = new GameObject(taken.Count == 0 ? "Reliquary" : $"Reliquary_{taken.Count}");
            go.transform.position = site;

            var rel = go.AddComponent<Reliquary>();
            float roll = Random.value;
            rel.grade = roll < barrowChance ? Reliquary.Grade.Barrow
                      : roll < barrowChance + shrineChance ? Reliquary.Grade.Shrine
                      : Reliquary.Grade.Wayside;
            // Distance from the start is the only honest way to pay for a walk.
            float d = Vector3.Distance(site, start);
            rel.richness = Mathf.Lerp(1f, 2.1f, Mathf.InverseLerp(minDistanceFromStart, minDistanceFromStart + 260f, d));

            var chestGo = Instantiate(set.chestPrefab, site, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), go.transform);
            var chest = chestGo.GetComponent<LootChest>();
            if (chest == null)
            {
                Debug.LogWarning("[Reliquary] The set's chest prefab has no LootChest on it — the site would be " +
                                 "decoration with nothing to open. Skipping.");
                Destroy(go);
                continue;
            }
            rel.Bind(chest);
            rel.Raise(set);

            taken.Add(site);
            s_live.Add(rel);
        }

        Placed = s_live.Count;
        if (Placed == 0)
            Debug.LogWarning($"[Reliquary] Wanted {want} but found no site with {clearRadius}m of clear ground " +
                             $"at least {minDistanceFromStart}m from the player. Lower clearRadius on a dense map.");
        else
            Debug.Log($"[Reliquary] Placed {Placed} " +
                      $"({s_live.FindAll(r => r.grade == Reliquary.Grade.Barrow).Count} barrow, " +
                      $"{s_live.FindAll(r => r.grade == Reliquary.Grade.Shrine).Count} shrine). " +
                      $"Lifetime armour granted: {ArmourLootTable.LifetimeFound}.");
    }

    private bool TryFindSite(Vector3 start, List<Vector3> taken, out Vector3 site)
    {
        site = Vector3.zero;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null) return false;
        Vector3 o = terrain.transform.position;
        Vector3 s = terrain.terrainData.size;

        Vector3 p = new Vector3(
            Random.Range(o.x + edgeMargin, o.x + s.x - edgeMargin),
            0f,
            Random.Range(o.z + edgeMargin, o.z + s.z - edgeMargin));
        p.y = terrain.SampleHeight(p) + o.y;

        if ((p - start).sqrMagnitude < minDistanceFromStart * minDistanceFromStart) return false;
        foreach (var t in taken)
            if ((p - t).sqrMagnitude < minSpacing * minSpacing) return false;

        // Room for the banners, and flat enough that a shrine does not end up
        // half-buried in a slope.
        if (Physics.CheckSphere(p + Vector3.up * 1.5f, clearRadius, ~0, QueryTriggerInteraction.Ignore)) return false;

        float h1 = terrain.SampleHeight(p + new Vector3(clearRadius, 0f, 0f)) + o.y;
        float h2 = terrain.SampleHeight(p + new Vector3(-clearRadius, 0f, 0f)) + o.y;
        float h3 = terrain.SampleHeight(p + new Vector3(0f, 0f, clearRadius)) + o.y;
        float h4 = terrain.SampleHeight(p + new Vector3(0f, 0f, -clearRadius)) + o.y;
        float spread = Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4)) - Mathf.Min(Mathf.Min(h1, h2), Mathf.Min(h3, h4));
        if (spread > 3.2f) return false;

        site = p;
        return true;
    }

    public static void NoteOpened(Reliquary rel)
    {
        if (rel == null || !s_live.Contains(rel)) return;
        s_live.Remove(rel);
        Opened++;
    }

    // Statics outlive a scene load; without this the next region believes its
    // reliquaries are already spent.
    public static void Reset()
    {
        s_live.Clear();
        Placed = 0;
        Opened = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!WorldEncounterDirector.IsAnyRegionMode()) return;
        if (FindFirstObjectByType<ReliquaryDirector>() != null) return;
        Reset();
        new GameObject("[Reliquaries]").AddComponent<ReliquaryDirector>();
    }
}
