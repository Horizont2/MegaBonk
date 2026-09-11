using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Collects the props a reliquary is built from into a Resources asset.
// See ReliquarySet for why this indirection exists.
public static class BuildReliquarySetTool
{
    private const string Path = "Assets/Resources/" + ReliquarySet.ResourceName + ".asset";

    // Paths rather than a search, because these are specific chosen props and a
    // type search would sweep up every rock in the project.
    private const string Chest = "Assets/Prefabs/Chest.prefab";

    private static readonly string[] Banners =
    {
        "Assets/RPGPP_LT/Prefabs/Props/Banners/rpgpp_lt_banner_01a.prefab",
        "Assets/RPGPP_LT/Prefabs/Props/Banners/rpgpp_lt_banner_01b.prefab",
    };

    private static readonly string[] RuneStones =
    {
        "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RuneRock_01.prefab",
        "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RuneRock_02.prefab",
    };

    private static readonly string[] Remains =
    {
        "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Skull.prefab",
        "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/SkullBones.prefab",
    };

    private const string Arch = "Assets/EmaceArt/NecroPOLY Dark Corners/Prefabs/Assets/Ruins/EA_Arch_Wall04_Ruin_01b_PRE.prefab";
    private const string Lantern = "Assets/EmaceArt/NecroPOLY Dark Corners/Prefabs/Assets/Props/EA_Exterior_Lantern_Solid_01a_PRE.prefab";

    // The region's own enemies, so a guarded site is guarded by the things that
    // live there rather than by a separate cast nobody recognises.
    private static readonly string[] Guardians =
    {
        "Assets/Prefabs/Skeleton_Warrior.prefab",
        "Assets/Prefabs/Skeleton_Rogue.prefab",
        "Assets/Prefabs/Skeleton_Minion.prefab",
    };

    [MenuItem("Tools/Exploration/Build Reliquary Set")]
    public static void Build()
    {
        var missing = new List<string>();

        GameObject One(string p)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) missing.Add(p);
            return go;
        }
        GameObject[] Many(string[] ps)
        {
            var list = new List<GameObject>(ps.Length);
            foreach (var p in ps) { var g = One(p); if (g != null) list.Add(g); }
            return list.ToArray();
        }

        Directory.CreateDirectory("Assets/Resources");
        var set = AssetDatabase.LoadAssetAtPath<ReliquarySet>(Path);
        bool isNew = set == null;
        if (isNew) set = ScriptableObject.CreateInstance<ReliquarySet>();

        set.chestPrefab = One(Chest);
        set.banners = Many(Banners);
        set.runeStones = Many(RuneStones);
        set.remains = Many(Remains);
        set.archPrefab = One(Arch);
        set.lanternPrefab = One(Lantern);
        set.guardianPrefabs = Many(Guardians);

        if (isNew) AssetDatabase.CreateAsset(set, Path);
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        ReliquarySet.ClearCache();

        // The chest is the only thing that is fatal — everything else degrades
        // into a plainer shrine, which is worth saying rather than failing over.
        if (set.chestPrefab == null)
        {
            Debug.LogError($"[Reliquary] No chest prefab at {Chest}. Without it a reliquary is decoration with " +
                           "nothing to open, and the director will refuse to place any.");
        }
        else if (set.chestPrefab.GetComponent<LootChest>() == null)
        {
            Debug.LogError($"[Reliquary] {Chest} has no LootChest component. The site would build and never open.");
        }

        if (set.banners.Length == 0)
            Debug.LogWarning("[Reliquary] No banner prefabs resolved. Banners ARE the landmark — without them the " +
                             "site is invisible until the player is standing on it, which defeats the whole feature.");

        string report = $"[Reliquary] Set built -> {Path}\n" +
                        $"  chest {(set.chestPrefab != null ? set.chestPrefab.name : "MISSING")}, " +
                        $"banners {set.banners.Length}, stones {set.runeStones.Length}, remains {set.remains.Length}, " +
                        $"arch {(set.archPrefab != null ? "yes" : "no")}, lantern {(set.lanternPrefab != null ? "yes" : "no")}";
        if (missing.Count > 0) Debug.LogWarning(report + "\n  Not found:\n    " + string.Join("\n    ", missing));
        else Debug.Log(report);

        Selection.activeObject = set;
    }
}
