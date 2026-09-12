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
    public static float ArmourChance(Reliquary.Grade grade)
    {
        float baseChance = grade switch
        {
            Reliquary.Grade.Barrow => 0.85f,   // the one you fought a warband for
            Reliquary.Grade.Shrine => 0.35f,
            _ => 0.12f,                        // a wayside find is mostly supplies
        };
        return baseChance * Mathf.Lerp(1f, FloorMultiplier, Progress);
    }

    // Pick a piece, or null when there is nothing left worth giving.
    //
    // Three rules, in this order:
    //
    //   1. ROLL A RARITY. Weighted so the good tiers are rare — the roll is over
    //      the FULL table, not just tiers with stock, so the intended
    //      distribution is a fixed thing rather than something that silently
    //      inflates as the player empties the bottom of the shop.
    //
    //   2. ALREADY OWN IT? TAKE ANOTHER OF THE SAME RARITY. Only unowned pieces
    //      are ever in the buckets, so this falls out for free.
    //
    //   3. WHOLE RARITY EXHAUSTED? DROP TO THE WEAKEST STILL AVAILABLE. It walks
    //      DOWN first and only goes up when there is nothing below. A player who
    //      has completed the low tiers should be handed the next weakest thing
    //      they are missing, not bumped up into legendaries because the common
    //      shelf happens to be empty — that would turn finishing a tier into a
    //      reward, which is the opposite of what the taper is for.
    public static ArmorData Roll(Reliquary.Grade grade)
    {
        bool legendary = grade == Reliquary.Grade.Barrow;
        var pool = WeaponIndex.UnownedArmour();
        if (pool.Count == 0) return null;

        var byTier = new Dictionary<int, List<ArmorData>>();
        foreach (var a in pool)
        {
            int tier = TierOf(a);
            if (tier < 2) continue;   // T1 is free; the player owns it already
            if (!byTier.TryGetValue(tier, out var list)) byTier[tier] = list = new List<ArmorData>();
            list.Add(a);
        }
        if (byTier.Count == 0) return null;

        int wanted = RollTier(legendary ? LegendaryWeights : NormalWeights, TaperTierBias);
        var bucket = NearestStock(byTier, wanted);
        return bucket == null ? null : PickWithinTier(bucket);
    }

    // The intended rarity, before availability is considered.
    private static int RollTier(float[] weights, float bias)
    {
        float total = 0f;
        var scored = new float[weights.Length];
        for (int tier = 2; tier < weights.Length; tier++)
        {
            float w = weights[tier];
            // The taper divides high tiers harder than low ones, so a well
            // supplied player drifts toward the bottom of the table rather than
            // off the top of it.
            if (tier >= 4) w /= Mathf.Pow(bias, tier - 3);
            scored[tier] = w;
            total += w;
        }
        if (total <= 0f) return 2;

        float roll = Random.value * total;
        for (int tier = 2; tier < scored.Length; tier++)
        {
            roll -= scored[tier];
            if (roll <= 0f) return tier;
        }
        return 2;
    }

    // Rule 3: the wanted tier, else the nearest one BELOW it, else the lowest above.
    private static List<ArmorData> NearestStock(Dictionary<int, List<ArmorData>> byTier, int wanted)
    {
        if (byTier.TryGetValue(wanted, out var exact)) return exact;
        for (int t = wanted - 1; t >= 2; t--)
            if (byTier.TryGetValue(t, out var lower)) return lower;
        for (int t = wanted + 1; t <= 6; t++)
            if (byTier.TryGetValue(t, out var higher)) return higher;
        return null;
    }

    // Within one rarity, the weaker piece is likelier.
    //
    // The tiers carry most of the difference, but colour variants inside a tier
    // are not always identical, and "rarer AND stronger is less likely" should
    // hold at every level rather than only between tiers.
    private static ArmorData PickWithinTier(List<ArmorData> list)
    {
        if (list.Count == 1) return list[0];

        float total = 0f;
        var scores = new float[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            float power = Power(list[i]);
            // Inverse, softened: a flat 1/power makes a marginally better piece
            // vanishingly rare, which is not the intent inside a single rarity.
            scores[i] = 1f / Mathf.Max(1f, Mathf.Pow(power, 0.6f));
            total += scores[i];
        }
        if (total <= 0f) return list[Random.Range(0, list.Count)];

        float roll = Random.value * total;
        for (int i = 0; i < list.Count; i++)
        {
            roll -= scores[i];
            if (roll <= 0f) return list[i];
        }
        return list[list.Count - 1];
    }

    // One number for "how good is this", folding in the three stats that matter
    // rather than reading power alone — a piece can be worth more than its power
    // rating suggests if it carries the health or the mitigation.
    public static float Power(ArmorData a) =>
        a == null ? 1f : a.basePower + a.baseHealthBonus * 0.5f + a.baseDamageReduction * 400f;

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
