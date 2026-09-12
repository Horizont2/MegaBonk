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
    [Tooltip("Metres a site must sit above the water line. Keeps shrines out of lakes AND off the shoreline, where the banners would stand in the shallows.")]
    public float waterClearance = 2.5f;
    [Tooltip("Level the ground under the site. Without it a shrine on any slope has half its stones buried and the chest floating.")]
    public bool flattenGround = true;

    public static int Placed { get; private set; }
    public static int Opened { get; private set; }

    private static readonly List<Reliquary> s_live = new List<Reliquary>(4);
    private static readonly Collider[] s_clearance = new Collider[32];

    private void Start() => StartCoroutine(PlaceWhenWorldExists());

    private IEnumerator PlaceWhenWorldExists()
    {
        // One frame for MissionInitializer.Start to publish the region, then ask.
        yield return null;
        if (!WorldEncounterDirector.IsAnyRegionMode())
        {
            Debug.Log("[Reliquary] Not a region mission — no reliquaries here.");
            Destroy(gameObject);
            yield break;
        }

        // Sites are chosen against the finished terrain and the finished tree
        // cover, so this waits for a definite signal rather than a guessed number
        // of frames — the failure mode here is silence.
        float deadline = Time.time + 90f;
        while (!WorldGenerator.IsGenerationDone && Time.time < deadline) yield return null;
        if (!WorldGenerator.IsGenerationDone)
            Debug.LogWarning("[Reliquary] World generation never reported done — placing against whatever terrain exists.");
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

        // Per-region overrides win where a designer set them. The values here are
        // the house default, not the law: an early forest and the Throne Room
        // should not be seeded the same way, and forcing that decision through
        // one global number is how a system stops being tunable.
        RegionData region = GameManager.Instance != null ? GameManager.Instance.currentRegion : null;
        if (region == null) region = MissionInitializer.PendingMissionRegion;

        float chance = region != null && region.reliquaryChance >= 0f ? region.reliquaryChance : regionHasOneChance;
        int cap = region != null && region.maxReliquaries >= 0 ? region.maxReliquaries : maxPerRegion;
        float pShrine = region != null && region.shrineChance >= 0f ? region.shrineChance : shrineChance;
        float pBarrow = region != null && region.barrowChance >= 0f ? region.barrowChance : barrowChance;

        if (Random.value > chance)
        {
            Debug.Log($"[Reliquary] This region has none (chance {chance:P0}). That is by design — see ReliquaryDirector.");
            return;
        }

        Vector3 start = Vector3.zero;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) start = pc.transform.position;

        int want = Random.Range(1, Mathf.Max(1, cap) + 1);
        var taken = new List<Vector3>(want);

        for (int attempt = 0; attempt < 400 && taken.Count < want; attempt++)
        {
            if (!TryFindSite(start, taken, out Vector3 site)) continue;

            float roll = Random.value;
            var rollGrade = roll < pBarrow ? Reliquary.Grade.Barrow
                          : roll < pBarrow + pShrine ? Reliquary.Grade.Shrine
                          : Reliquary.Grade.Wayside;

            // Level the ground FIRST, before a single prop is placed.
            //
            // Everything the site builds samples the terrain to sit on it — the
            // banners, the stones, the chest, the guardians. Flattening
            // afterwards would move the ground out from under all of them and
            // leave the whole shrine buried or hovering. The site is also
            // re-sampled after this so the chest sits on the new level, not the
            // old slope.
            if (flattenGround) site = LevelGround(site, rollGrade);

            // Named by grade so a hierarchy search for "Reliquary" both finds
            // them and says what each one is without clicking it.
            var go = new GameObject($"Reliquary_{rollGrade}_{taken.Count}");
            go.transform.position = site;

            var rel = go.AddComponent<Reliquary>();
            rel.grade = rollGrade;

            // Distance from the start is the only honest way to pay for a walk.
            float d = Vector3.Distance(site, start);
            rel.richness = Mathf.Lerp(1f, 2.1f, Mathf.InverseLerp(minDistanceFromStart, minDistanceFromStart + 260f, d));

            var chest = BuildChest(set, rel.grade, site, go.transform);
            if (chest == null) { Destroy(go); continue; }
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
        {
            var where = new System.Text.StringBuilder();
            foreach (var r in s_live)
                where.Append($"\n    {r.grade} at {r.transform.position}  ({Vector3.Distance(r.transform.position, start):F0}m from start)");
            Debug.Log($"[Reliquary] Placed {Placed} " +
                      $"({s_live.FindAll(r => r.grade == Reliquary.Grade.Barrow).Count} barrow, " +
                      $"{s_live.FindAll(r => r.grade == Reliquary.Grade.Shrine).Count} shrine). " +
                      $"Lifetime armour granted: {ArmourLootTable.LifetimeFound}." + where);
        }
    }

    // Flatten a pad for the site and hand back the settled ground position.
    //
    // Uses the generator's own FlattenTerrainRobust rather than a second
    // implementation — the roads and the hand-built locations already level
    // ground with it, and two routines that flatten "almost the same way" is how
    // a seam appears where they meet. A barrow needs a wider pad than a wayside
    // find because its ring of props is wider; the falloff blends it back into
    // the hillside so the pad does not read as a plateau someone stamped out.
    private Vector3 LevelGround(Vector3 site, Reliquary.Grade grade)
    {
        var gen = FindFirstObjectByType<WorldGenerator>();
        if (gen == null) return site;

        float radius = grade switch { Reliquary.Grade.Barrow => 9f, Reliquary.Grade.Shrine => 7f, _ => 5f };
        gen.FlattenTerrainRobust(site, radius, radius * 1.8f, site.y);

        // Re-sample: SetHeights has just moved the surface, and every prop about
        // to be placed will raycast against the NEW one.
        Terrain t = Terrain.activeTerrain;
        if (t != null) site.y = t.SampleHeight(site) + t.transform.position.y;
        return site;
    }

    // Wraps a chest MODEL in the interactive parts.
    //
    // The three tiers from the Fantasy Polygon Chest pack are art only — no
    // LootChest, no collider, just a mesh and an animator whose single parameter
    // is the "Open" trigger LootChest already sends. So the root is assembled
    // here rather than there being three near-identical prefabs to author and
    // keep in step, and the grade the player walked to is legible from the chest
    // itself before they are close enough to read anything else.
    private static LootChest BuildChest(ReliquarySet set, Reliquary.Grade grade, Vector3 site, Transform parent)
    {
        var model = set.ChestFor((int)grade);
        if (model == null) return null;

        var root = new GameObject("Chest");
        root.transform.SetParent(parent, false);
        root.transform.position = site;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Instantiate(model, root.transform, false);

        var chest = root.AddComponent<LootChest>();
        chest.possibleLoot = set.chestLoot;
        // A barrow chest is worth more in raw pickups too, not only in the armour
        // roll — the pile has to look like it was worth nine seconds.
        switch (grade)
        {
            case Reliquary.Grade.Barrow: chest.minLootItems = 6; chest.maxLootItems = 12; break;
            case Reliquary.Grade.Shrine: chest.minLootItems = 4; chest.maxLootItems = 8; break;
            default:                     chest.minLootItems = 2; chest.maxLootItems = 5; break;
        }
        // LootChest resolves its own animator from the children in Start, so the
        // model's controller is picked up without wiring.
        return chest;
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

        // NOT IN WATER. Rivers and lakes are carved into the same heightmap this
        // samples, so a site chosen on height alone lands happily on a lake bed —
        // and a shrine at the bottom of a river is both unreachable and absurd.
        // The margin keeps it off the shoreline too, where the banners would
        // stand in the shallows.
        var gen = FindFirstObjectByType<WorldGenerator>();
        if (gen != null && p.y < gen.AbsoluteWaterHeight + waterClearance) return false;

        // Room for the banners, and flat enough that a shrine does not end up
        // half-buried in a slope.
        //
        // TerrainColliders are skipped explicitly, and that is not a detail: a
        // sphere of this radius sitting a metre above the ground ALWAYS
        // intersects the terrain it is standing on, so a plain CheckSphere
        // rejected every candidate site and no reliquary was ever placed
        // anywhere. The test is "is anything built or grown here", not "is there
        // ground here" — there had better be ground here.
        int n = Physics.OverlapSphereNonAlloc(p + Vector3.up * 1.5f, clearRadius, s_clearance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            if (s_clearance[i] == null) continue;
            if (s_clearance[i] is TerrainCollider) continue;
            return false;
        }

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
        // NO region check here, and that is the whole reason nothing was ever
        // placed.
        //
        // AfterSceneLoad runs BEFORE Start() on the scene's own objects, so at
        // this moment GameManager.Instance is usually still null and
        // MissionInitializer has not published PendingMissionRegion yet.
        // IsAnyRegionMode therefore answered "not a region" on every single load
        // and the director was never even created — no component, no log, no
        // symptom to chase.
        //
        // WorldEncounterDirector has always dodged this by waiting a frame before
        // asking. The check now lives in the coroutine below, after that wait.
        if (FindFirstObjectByType<ReliquaryDirector>() != null) return;
        Reset();
        new GameObject("[Reliquaries]").AddComponent<ReliquaryDirector>();
    }
}
