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

        index.weapons = weapons;
        if (isNew) AssetDatabase.CreateAsset(index, Path);
        EditorUtility.SetDirty(index);
        AssetDatabase.SaveAssets();
        WeaponIndex.ClearCache();

        // Duplicate IDs would make "does the player own this" ambiguous, and the
        // symptom would be a cache handing out a weapon that never appears.
        var dupes = weapons.GroupBy(w => w.weaponID).Where(g => g.Count() > 1).ToArray();
        string body = string.Join("\n  ", weapons.Select(w => $"{w.weaponID,3}  {w.weaponName}  ({w.price})"));
        if (dupes.Length > 0)
            Debug.LogWarning($"[WeaponIndex] {weapons.Length} weapon(s) indexed, but these IDs are used more than " +
                             $"once: {string.Join(", ", dupes.Select(d => d.Key))}. Ownership is keyed on the ID, so " +
                             $"unlocking one unlocks the other.\n  {body}");
        else
            Debug.Log($"[WeaponIndex] {weapons.Length} weapon(s) indexed -> {Path}\n  {body}");

        Selection.activeObject = index;
    }
}
