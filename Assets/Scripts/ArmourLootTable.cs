using System.Collections.Generic;
using UnityEngine;

// What a reliquary is allowed to give away, and how often.
//
// ==== THE ECONOMY PROBLEM THIS EXISTS TO SOLVE ====
//
// Armour is shop stock. A T6 chestplate costs 2600 diamonds; a conquered region
// pays 40-100. So ONE careless legendary drop hands the player roughly thirty
// regions of income, and the shop — which is most of the game's progression —
// stops being a thing anyone interacts with.
//
// Three separate brakes, because any one of them alone fails to a lucky streak:
//
//   TIER WEIGHTING. Steep, not gentle. A T2 find is common and a T6 find is
//   something you tell someone about. The weights below are deliberately not a
//   smooth curve — the top two tiers are outliers, not the end of a ramp.
//
//   A GLOBAL TAPER. Every piece ever found from a reliquary makes the next one
//   less likely and biases it lower. Without this, a player who explores
//   thoroughly gets the whole armoury by region eight; with it, exploring
//   remains worth doing and stops being a substitute for the economy.
//
//   NO DOUBLE-DIPPING. A reliquary that gives armour gives no diamonds. Handing
//   out both the item and the money for the item is how a reward system quietly
//   becomes the only reward system.
//
// T1 is never dropped: it is priced at 0, which means the player already owns it.
public static class ArmourLootTable
{
    // Lifetime count of pieces granted by reliquaries. Persisted, because the
    // whole point is that it survives the run that got lucky.
    private const string PP_FOUND = "CacheArmourFound";

    public static int LifetimeFound => PlayerPrefs.GetInt(PP_FOUND, 0);

    // Relative weights by tier. Sum is arbitrary; only the ratios matter.
    private static readonly float[] NormalWeights   = { 0f, 0f, 46f, 30f, 16f, 6.5f, 1.5f };
    private static readonly float[] LegendaryWeights = { 0f, 0f, 4f, 12f, 26f, 34f, 24f };

    // After this many lifetime finds the odds are at their floor. Chosen so a
    // player who explores everything still has a reason to open the next one,
    // but is no longer being handed the top of the shop.
    private const int TaperOver = 14;
    private const float FloorMultiplier = 0.25f;

    // How much the taper drags the roll DOWN the tier list as well as making it
    // rarer — late finds skew common even when they happen.
    private static float TaperTierBias => Mathf.Lerp(1f, 2.4f, Progress);
    private static float Progress => Mathf.Clamp01(LifetimeFound / (float)TaperOver);

    // The chance a reliquary contains armour at all, before anything is rolled.
    // Everything else it gives — supplies, crystals — is the common case.
    public static float ArmourChance(bool legendary)
    {
        float baseChance = legendary ? 0.85f : 0.30f;
        return baseChance * Mathf.Lerp(1f, FloorMultiplier, Progress);
    }

    // Pick a piece, or null when there is nothing left worth giving.
    public static ArmorData Roll(bool legendary)
    {
        var pool = WeaponIndex.UnownedArmour();
        if (pool.Count == 0) return null;

        // Bucket what is actually still available. Rolling a tier the player has
        // already cleared out and then giving up would make the drop rate
        // silently collapse as they complete the set.
        var byTier = new Dictionary<int, List<ArmorData>>();
        foreach (var a in pool)
        {
            int tier = TierOf(a);
            if (tier < 2) continue;
            if (!byTier.TryGetValue(tier, out var list)) byTier[tier] = list = new List<ArmorData>();
            list.Add(a);
        }
        if (byTier.Count == 0) return null;

        float[] weights = legendary ? LegendaryWeights : NormalWeights;
        float bias = TaperTierBias;

        float total = 0f;
        var tiers = new List<int>(byTier.Keys);
        var scores = new List<float>(tiers.Count);
        foreach (int tier in tiers)
        {
            // The bias divides high tiers harder than low ones, so a well-supplied
            // player drifts toward the bottom of the table rather than off it.
            float w = weights[Mathf.Clamp(tier, 0, weights.Length - 1)];
            if (tier >= 4) w /= Mathf.Pow(bias, tier - 3);
            scores.Add(w);
            total += w;
        }
        if (total <= 0f) return null;

        float roll = Random.value * total;
        for (int i = 0; i < tiers.Count; i++)
        {
            roll -= scores[i];
            if (roll > 0f) continue;
            var list = byTier[tiers[i]];
            return list[Random.Range(0, list.Count)];
        }
        return null;
    }

    public static void NoteGranted()
    {
        PlayerPrefs.SetInt(PP_FOUND, LifetimeFound + 1);
        PlayerPrefs.Save();
    }

    // Tier lives in the asset name — "Chest_T4_C2" — because that is where the
    // library already encodes it and ArmorData has no tier field. Falling back on
    // price keeps it working for any piece named differently.
    public static int TierOf(ArmorData a)
    {
        if (a == null) return 0;

        string n = a.name;
        int t = n.IndexOf("_T");
        if (t >= 0 && t + 2 < n.Length && char.IsDigit(n[t + 2]))
            return n[t + 2] - '0';

        // Price bands, straight off the authored library: T2 ~300, T3 700,
        // T4 ~1200, T5 ~1900, T6 2600.
        if (a.price <= 0) return 1;
        if (a.price < 500) return 2;
        if (a.price < 1000) return 3;
        if (a.price < 1600) return 4;
        if (a.price < 2300) return 5;
        return 6;
    }

    public static Color TierColour(int tier) => tier switch
    {
        6 => new Color(1.00f, 0.45f, 0.15f),   // legendary — ember
        5 => new Color(0.85f, 0.45f, 1.00f),   // epic — violet
        4 => new Color(0.35f, 0.65f, 1.00f),   // rare — blue
        3 => new Color(0.40f, 0.95f, 0.50f),   // uncommon — green
        _ => new Color(0.85f, 0.85f, 0.85f),   // common — bone
    };

    public static string TierNameKey(int tier) => tier switch
    {
        6 => "RARITY_LEGENDARY",
        5 => "RARITY_EPIC",
        4 => "RARITY_RARE",
        3 => "RARITY_UNCOMMON",
        _ => "RARITY_COMMON",
    };
}
