using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Fixes camp NPCs that slide around holding a single pose.
//
// ==== WHAT IS ACTUALLY WRONG ====
//
// Elias walks his patrol route in one frozen pose. Everything about his setup
// looks correct and that is why it is confusing: he has an Animator, a valid
// avatar, a controller with Idle/Walk/sitting states and IsWalking/IsSitting
// parameters, an NPCPatroller with all of it wired, and a NavMeshAgent. The
// patroller sets the parameter. The state machine transitions. Nothing errors.
//
// The mismatch is one line in an import setting. His model, Cowboy_Male.fbx, is
// imported as GENERIC, while the clips his controller plays — from Peasant Nolant
// and Sitting Idle — are HUMANOID.
//
// A humanoid clip is stored in muscle space, not as curves on named transforms.
// It can only be applied through a humanoid avatar, which retargets it onto
// whatever proportions the character happens to have. Hand one to a generic
// avatar and there is nothing to bind to: the animator plays it, reports no
// error, and writes nothing. The character holds its bind pose while the
// NavMeshAgent carries it around — exactly what you see.
//
// It is the same fault as the Skeleton_Mage bug earlier, which is worth saying
// out loud: this failure mode is silent, it looks like a missing animation rather
// than a wrong checkbox, and it will keep happening whenever a model and its
// clips come from different packs.
//
// The fix is to import the model as Humanoid. Done through the importer rather
// than by editing the .meta by hand, so it can be re-run and so it reports what
// it changed.
public static class CampNpcAnimationFix
{
    // The shared body used by the Lvl1/camp NPC characters.
    private const string CharacterDir = "Assets/Lvl/Models1/FBX";

    [MenuItem("Tools/NPCs/Fix Camp NPC Animation (Generic -> Humanoid)")]
    public static void Fix()
    {
        var offenders = FindGenericCharacters();
        if (offenders.Count == 0)
        {
            EditorUtility.DisplayDialog("Camp NPC animation",
                "Every character model under\n" + CharacterDir + "\nis already imported as Humanoid.", "OK");
            return;
        }

        string list = string.Join("\n  ", offenders.Select(Path.GetFileName).Take(25));
        if (offenders.Count > 25) list += $"\n  ...and {offenders.Count - 25} more";

        bool go = EditorUtility.DisplayDialog("Camp NPC animation",
            $"{offenders.Count} character model(s) are imported as GENERIC and so cannot play the " +
            $"HUMANOID clips their controllers reference — which is why they move without animating:\n\n  {list}\n\n" +
            "Re-import them as Humanoid?\n\nUnity will rebuild an avatar for each. Check them in the scene " +
            "afterwards: a model whose bones cannot be mapped to a humanoid skeleton will report it in the console.",
            "Re-import as Humanoid", "Cancel");
        if (!go) return;

        int done = 0, failed = 0;
        try
        {
            for (int i = 0; i < offenders.Count; i++)
            {
                string path = offenders[i];
                EditorUtility.DisplayProgressBar("Re-importing as Humanoid", Path.GetFileName(path), i / (float)offenders.Count);

                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;

                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.SaveAndReimport();

                // A model whose bones Unity cannot map to the humanoid rig comes
                // back with no avatar, and silently leaves the NPC exactly as
                // broken as before. Say so rather than reporting success.
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (avatar != null && avatar.isValid) done++;
                else
                {
                    failed++;
                    Debug.LogWarning($"[CampNPC] '{Path.GetFileName(path)}' could not be mapped to a humanoid rig — " +
                                     "it is still Generic in effect. Configure its avatar by hand, or give its " +
                                     "controller clips from its own rig instead.");
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.Refresh();
        int repaired = RepairControllers();
        Debug.Log($"[CampNPC] {done} model(s) now Humanoid" + (failed > 0 ? $", {failed} could not be mapped" : "") +
                  $", {repaired} controller state(s) re-pointed at humanoid clips. " +
                  "Enter play mode in the camp and check that Elias walks rather than slides.");
    }

    // The other half of the same mismatch, in the opposite direction.
    //
    // Flipping the model to Humanoid fixes the states whose clips are humanoid —
    // which for Elias is Walk and sitting. But his Idle points at
    // CharacterArmature_Idle.anim, a GENERIC clip: 146 curves bound to transform
    // paths and not one muscle curve. On a humanoid avatar that one now stops
    // applying, so fixing the walk on its own would trade a frozen walk for a
    // frozen idle and look like no progress at all.
    //
    // So every state whose clip cannot play on a humanoid rig gets re-pointed at
    // one that can, matched by what the state is called. Clips are preferred from
    // the same source the controller already uses elsewhere, so the idle and the
    // walk still look like the same character moving.
    private static int RepairControllers()
    {
        var humanoid = CollectHumanoidClips();
        if (humanoid.Count == 0) return 0;

        int fixedStates = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/Animators" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
            if (ac == null) continue;

            bool dirty = false;
            foreach (var layer in ac.layers)
            {
                if (layer.stateMachine == null) continue;
                foreach (var child in layer.stateMachine.states)
                {
                    var state = child.state;
                    if (state == null || !(state.motion is AnimationClip clip)) continue;
                    if (clip.humanMotion) continue;   // already fine

                    AnimationClip replacement = MatchByRole(state.name, humanoid);
                    if (replacement == null)
                    {
                        Debug.LogWarning($"[CampNPC] '{Path.GetFileNameWithoutExtension(path)}' state '{state.name}' " +
                                         $"plays generic clip '{clip.name}', which a humanoid rig cannot use, and no " +
                                         "humanoid clip matched that state name. Assign one by hand.");
                        continue;
                    }

                    Debug.Log($"[CampNPC] {Path.GetFileNameWithoutExtension(path)}: state '{state.name}' " +
                              $"'{clip.name}' (generic) -> '{replacement.name}' (humanoid)");
                    state.motion = replacement;
                    fixedStates++;
                    dirty = true;
                }
            }
            if (dirty) EditorUtility.SetDirty(ac);
        }

        if (fixedStates > 0) AssetDatabase.SaveAssets();
        return fixedStates;
    }

    private static AnimationClip MatchByRole(string stateName, List<AnimationClip> pool)
    {
        string s = stateName.ToLowerInvariant();
        string[] wanted =
            s.Contains("sit")  ? new[] { "sit_chair_idle", "sitting idle", "sit" } :
            s.Contains("walk") ? new[] { "metarig|walk", "walking_a", "walk" } :
            s.Contains("run")  ? new[] { "running_a", "run" } :
                                 new[] { "metarig|idle", "idle_a", "idle" };

        foreach (var want in wanted)
            foreach (var c in pool)
                if (c.name.ToLowerInvariant() == want) return c;
        foreach (var want in wanted)
            foreach (var c in pool)
                if (c.name.ToLowerInvariant().Contains(want)) return c;
        return null;
    }

    // Humanoid clips from the packs the camp NPCs already draw on, plus the
    // KayKit libraries as a fallback.
    private static List<AnimationClip> CollectHumanoidClips()
    {
        var found = new List<AnimationClip>();
        string[] sources =
        {
            "Assets/Stylized NPC - Peasant Nolant/Models",
            "Assets/Animators",
            "Assets/HeroAnimations/Animations/fbx/Rig_Medium",
        };
        foreach (var dir in sources)
        {
            if (!AssetDatabase.IsValidFolder(dir)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(p))
                    if (o is AnimationClip c && c.humanMotion && !c.name.StartsWith("__preview__")) found.Add(c);
                var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (direct != null && direct.humanMotion) found.Add(direct);
            }
        }
        return found;
    }

    [MenuItem("Tools/NPCs/Report Rig Types")]
    public static void Report()
    {
        var lines = new List<string>();
        foreach (var path in CharacterFiles())
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) continue;
            lines.Add($"{Path.GetFileNameWithoutExtension(path),-28} {mi.animationType}");
        }
        lines.Sort();
        Debug.Log($"[CampNPC] Rig types under {CharacterDir}:\n  " + string.Join("\n  ", lines));
    }

    private static IEnumerable<string> CharacterFiles()
    {
        if (!Directory.Exists(CharacterDir)) yield break;
        foreach (var p in Directory.GetFiles(CharacterDir, "*.fbx", SearchOption.TopDirectoryOnly))
            yield return p.Replace('\\', '/');
    }

    private static List<string> FindGenericCharacters()
    {
        var list = new List<string>();
        foreach (var path in CharacterFiles())
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi != null && mi.animationType != ModelImporterAnimationType.Human) list.Add(path);
        }
        return list;
    }
}
