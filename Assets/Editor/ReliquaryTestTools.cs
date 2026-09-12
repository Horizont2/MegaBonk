using System.Text;
using UnityEditor;
using UnityEngine;

// Ways to see the exploration systems work without waiting on the dice.
//
// A reliquary is deliberately rare, a barrow rarer, and an armour drop rarer
// still — which is right for play and useless for checking whether any of it
// functions. These put each piece on screen on demand.
public static class ReliquaryTestTools
{
    [MenuItem("Tools/Exploration/Test/Spawn Wayside At Player", priority = 100)]
    private static void SpawnWayside() => Spawn(Reliquary.Grade.Wayside);

    [MenuItem("Tools/Exploration/Test/Spawn Shrine At Player", priority = 101)]
    private static void SpawnShrine() => Spawn(Reliquary.Grade.Shrine);

    [MenuItem("Tools/Exploration/Test/Spawn Barrow At Player", priority = 102)]
    private static void SpawnBarrow() => Spawn(Reliquary.Grade.Barrow);

    [MenuItem("Tools/Exploration/Test/Spawn Wayside At Player", true)]
    [MenuItem("Tools/Exploration/Test/Spawn Shrine At Player", true)]
    [MenuItem("Tools/Exploration/Test/Spawn Barrow At Player", true)]
    private static bool SpawnValidate() => Application.isPlaying;

    private static void Spawn(Reliquary.Grade grade)
    {
        var set = ReliquarySet.Load();
        if (set == null || !set.IsUsable)
        {
            Debug.LogError("[Reliquary/Test] No usable ReliquarySet. Run Tools > Exploration > Build Reliquary Set.");
            return;
        }

        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Debug.LogError("[Reliquary/Test] No player in the scene."); return; }

        // Well clear of the player, so the guardians do not wake in their face
        // and the site can actually be walked up to the way a real one is.
        Vector3 fwd = pc.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        Vector3 site = pc.transform.position + fwd.normalized * 18f;
        site.y = Ground(site);

        // Instantiates THE PREFAB — the same asset the generator places and the
        // same one a hand-built location would contain. The old version assembled
        // a chest here by hand, which meant this tool tested a code path that
        // shipped nowhere: it could look perfect while the real thing was broken,
        // and it did, for several rounds of "the chest is gigantic".
        var prefab = set.SiteFor((int)grade);
        if (prefab == null)
        {
            Debug.LogError("[Reliquary/Test] No site prefabs in the set. Run " +
                           "Tools > Exploration > Build Reliquary Prefabs first.");
            return;
        }

        var go = Object.Instantiate(prefab, site, Quaternion.identity);
        go.name = $"Reliquary_{grade}_TEST";
        var rel = go.GetComponent<Reliquary>();
        if (rel == null) { Debug.LogError("[Reliquary/Test] The prefab has no Reliquary component."); return; }
        rel.grade = grade;
        rel.richness = 1.5f;
        rel.buildDecor = true;   // dropped on bare ground, so it dresses itself

        Debug.Log($"[Reliquary/Test] {grade} placed 18m ahead of the player at {site}. " +
                  "Walk to it — the guardians wake on approach; once they are down, [E] lights the seal.");
    }

    // ---------------------------------------------------------------------

    [MenuItem("Tools/Exploration/Test/Preview Reward Reveal", priority = 110)]
    private static void PreviewReveal()
    {
        var index = WeaponIndex.Load();
        if (index == null || index.armour == null || index.armour.Length == 0)
        {
            Debug.LogError("[Reliquary/Test] No WeaponIndex. Run Tools > Shop > Build Weapon Index.");
            return;
        }

        // Something with a real icon, so the preview shows what a drop shows.
        ArmorData pick = null;
        foreach (var a in index.armour) { if (a != null && a.icon != null) { pick = a; break; } }
        if (pick == null) { Debug.LogError("[Reliquary/Test] No armour in the index has an icon."); return; }

        int tier = ArmourLootTable.TierOf(pick);
        RewardReveal.Show(pick.icon, pick.armorName,
            LocalizationManager.Tr("REVEAL_ARMOUR_SUB",
                                   LocalizationManager.Tr(ArmourLootTable.TierNameKey(tier)),
                                   pick.category.ToString(), pick.basePower),
            ArmourLootTable.TierColour(tier));
    }

    [MenuItem("Tools/Exploration/Test/Preview Reward Reveal", true)]
    private static bool PreviewValidate() => Application.isPlaying;

    // ---------------------------------------------------------------------

    // Rolls the loot table many times and prints the distribution, so the odds
    // can be checked against intent instead of guessed at from a few drops.
    [MenuItem("Tools/Exploration/Test/Simulate 1000 Armour Rolls", priority = 120)]
    private static void Simulate()
    {
        var index = WeaponIndex.Load();
        if (index == null || index.armour == null || index.armour.Length == 0)
        {
            Debug.LogError("[Reliquary/Test] No WeaponIndex. Run Tools > Shop > Build Weapon Index.");
            return;
        }

        var sb = new StringBuilder("[Reliquary/Test] 1000 rolls per grade, against the CURRENT save " +
                                   $"(lifetime found: {ArmourLootTable.LifetimeFound}, regions conquered: " +
                                   $"{PlayerPrefs.GetInt("TotalConqueredRegions", 0)}, so the taper is applied). " +
                                   $"CEILING RIGHT NOW: T{ArmourLootTable.MaxTierAllowed()} — nothing above it can " +
                                   "be rolled at all, whatever the weights say.\n");

        foreach (Reliquary.Grade grade in System.Enum.GetValues(typeof(Reliquary.Grade)))
        {
            var counts = new int[8];
            int nulls = 0;
            for (int i = 0; i < 1000; i++)
            {
                var a = ArmourLootTable.Roll(grade);
                if (a == null) { nulls++; continue; }
                counts[Mathf.Clamp(ArmourLootTable.TierOf(a), 0, 7)]++;
            }

            sb.Append($"  {grade,-8} armour chance {ArmourLootTable.ArmourChance(grade):P0}  ->  ");
            for (int t = 2; t <= 6; t++) sb.Append($"T{t}:{counts[t] / 10f:F1}%  ");
            if (nulls > 0) sb.Append($"(nothing left: {nulls / 10f:F1}%)");
            sb.AppendLine();
        }

        int owned = 0;
        foreach (var a in index.armour)
            if (a != null && a.price > 0 && PlayerPrefs.GetInt("ArmorUnlocked_" + a.armorID, 0) == 1) owned++;
        sb.AppendLine($"  Player owns {owned} of {index.armour.Length} indexed pieces.");

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Exploration/Test/Reset Armour Find History", priority = 121)]
    private static void ResetHistory()
    {
        if (!EditorUtility.DisplayDialog("Reset armour find history",
            "Clears the lifetime count that tapers armour drop odds. Does NOT un-own any armour.",
            "Reset", "Cancel")) return;
        PlayerPrefs.DeleteKey("CacheArmourFound");
        PlayerPrefs.Save();
        Debug.Log("[Reliquary/Test] Armour find history cleared — drop odds back to full.");
    }

    private static float Ground(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 120f, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        var t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
    }
}
