using UnityEditor;
using UnityEngine;

// Start the region flythrough from the menu as well as from the key.
//
//   Tools ▸ Lore Trailer ▸ Play Region Flythrough
//
// A hotkey is convenient right up until it collides with something — F9 was
// already QuickLoad, so pressing it restored a save and played the resource
// sound instead of starting anything, which looked like the flythrough being
// broken rather than the key being taken. A menu item cannot collide, and it
// says plainly when it will not work and why.
public static class RegionFlythroughMenu
{
    [MenuItem("Tools/Lore Trailer/Play Region Flythrough")]
    public static void Play()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Region flythrough",
                "Enter Play mode first and start a region assault.\n\n" +
                "The shot is built from what is actually in the scene — the live totem, the enemies " +
                "near the player, the player themselves — so there is nothing to film until the region " +
                "is running.", "OK");
            return;
        }

        var fly = Object.FindFirstObjectByType<RegionFlythrough>();
        if (fly == null)
        {
            // The component installs itself at scene load, so its absence means
            // play mode has not actually started or the script failed to compile.
            EditorUtility.DisplayDialog("Region flythrough",
                "No RegionFlythrough in the scene. It installs itself when play mode starts — " +
                "if it is missing, check the console for a compile error.", "OK");
            return;
        }

        fly.Play();
    }

    [MenuItem("Tools/Lore Trailer/Play Region Flythrough", true)]
    private static bool Validate() => Application.isPlaying;
}
