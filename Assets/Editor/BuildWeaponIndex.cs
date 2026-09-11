using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Collects every WeaponData in the project into a Resources asset so code
// outside the shop scene can reach the weapon list. See WeaponIndex.
public static class BuildWeaponIndexTool
{
    private const string Path = "Assets/Resources/" + WeaponIndex.ResourceName + ".asset";

    [MenuItem("Tools/Shop/Build Weapon Index")]
    public static void Build()
    {
        var weapons = AssetDatabase.FindAssets("t:WeaponData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<WeaponData>)
            .Where(w => w != null)
            .OrderBy(w => w.weaponID)
            .ToArray();

        if (weapons.Length == 0)
        {
            EditorUtility.DisplayDialog("Weapon index", "No WeaponData assets found in the project.", "OK");
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        var index = AssetDatabase.LoadAssetAtPath<WeaponIndex>(Path);
        bool isNew = index == null;
        if (isNew) index = ScriptableObject.CreateInstance<WeaponIndex>();

        var armour = AssetDatabase.FindAssets("t:ArmorData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ArmorData>)
            .Where(a => a != null)
            .OrderBy(a => a.armorID)
            .ToArray();

        index.weapons = weapons;
        index.armour = armour;
        if (isNew) AssetDatabase.CreateAsset(index, Path);
        EditorUtility.SetDirty(index);
        AssetDatabase.SaveAssets();
        WeaponIndex.ClearCache();

        // Duplicate IDs would make "does the player own this" ambiguous, and the
        // symptom would be a cache handing out a weapon that never appears.
        var dupes = weapons.GroupBy(w => w.weaponID).Where(g => g.Count() > 1).ToArray();
        var armourDupes = armour.GroupBy(a => a.armorID).Where(g => g.Count() > 1).ToArray();
        int noIcon = armour.Count(a => a.icon == null) + weapons.Count(w => w.icon == null);

        string body = string.Join("\n  ", weapons.Select(w => $"W{w.weaponID,3}  {w.weaponName}  ({w.price})"));
        string summary = $"[WeaponIndex] {weapons.Length} weapon(s) and {armour.Length} armour piece(s) indexed -> {Path}";

        if (dupes.Length > 0 || armourDupes.Length > 0)
            Debug.LogWarning($"{summary}\n  DUPLICATE IDs — ownership is keyed on the ID, so unlocking one unlocks " +
                             $"the other. Weapons: {string.Join(", ", dupes.Select(d => d.Key))}. " +
                             $"Armour: {string.Join(", ", armourDupes.Select(d => d.Key))}\n  {body}");
        else
            Debug.Log($"{summary}\n  {body}");

        // The reveal is built around the icon. A drop with none still shows its
        // name, but it is the one thing worth knowing about before shipping.
        if (noIcon > 0)
            Debug.LogWarning($"[WeaponIndex] {noIcon} indexed item(s) have no icon sprite — their reward reveal will " +
                             "show the name over an empty frame.");

        Selection.activeObject = index;
    }
}
