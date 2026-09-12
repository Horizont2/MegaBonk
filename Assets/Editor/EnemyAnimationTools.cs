using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Turns the animation libraries already sitting in this project into clips the
// game can actually use, and collects them into one loadable asset.
//
// ==== WHY THIS EXISTS ====
//
// Assets/HeroAnimations/Animations/fbx/ holds the COMPLETE KayKit character
// animation pack — eight Rig_Medium libraries and six Rig_Large — 159 clips
// covering melee, ranged, magic, blocking, dodging, taunts, resurrection and a
// set of skeleton-specific animations that could not be more on-brief for this
// game. Unity has never split a single one of them: every .meta reads
// `clipAnimations: []`, which means the takes inside those FBXs have never been
// turned into clips and nothing in the project can reference them.
//
// So the enemies have been sharing one controller with nine states while a
// purpose-built library sat unopened in the repo. This does the opening.
//
// Two steps, both idempotent, both safe to re-run:
//
//   IMPORT  reads each FBX's take list and writes a clip definition per take.
//           Nothing is invented — the names come from the file itself.
//
//   BUILD   collects the resulting clips by role into a Resources asset, which
//           is how the runtime reaches them (AssetDatabase does not exist in a
//           build, and wiring nine prefabs by hand is nine chances to miss one).
public static class EnemyAnimationTools
{
    private const string MediumDir = "Assets/HeroAnimations/Animations/fbx/Rig_Medium";
    private const string LargeDir = "Assets/HeroAnimations/Animations/fbx/Rig_Large";

    // Assets/Skeletons/Animations/fbx/Rig_Medium is DELIBERATELY NOT IMPORTED.
    //
    // EnemyAnimator.controller — the one every enemy in the game runs on — has
    // its Idle, Hit and Death states pointing at clips inside those FBXs, and it
    // stores those references as fileIDs. Re-importing the file regenerates the
    // clip definitions and therefore the internalIDs behind those fileIDs, so
    // the controller's states silently end up on whichever clip now sits at that
    // id. That is a scrambled animator: attacks playing hit reactions, idles
    // playing deaths, and nothing in the console to say why.
    //
    // Those libraries are byte-identical copies of two of the eight in
    // HeroAnimations, so nothing is lost by leaving them alone.
    private const string SetPath = "Assets/Resources/" + EnemyAnimationSet.ResourceName + ".asset";

    [MenuItem("Tools/Enemies/1 - Import Animation Libraries", priority = 1)]
    public static void ImportLibraries()
    {
        var files = LibraryFiles();
        if (files.Count == 0)
        {
            EditorUtility.DisplayDialog("Import animation libraries",
                $"No FBX libraries found under\n{MediumDir}\nor\n{LargeDir}", "OK");
            return;
        }

        int split = 0, clips = 0;
        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                EditorUtility.DisplayProgressBar("Importing animation libraries",
                    Path.GetFileName(path), i / (float)files.Count);

                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;

                bool changed = false;

                // Humanoid on BOTH rigs. Rig_Large ships as Generic, and a generic
                // clip only binds by transform path — it will not play on the
                // skeleton avatars at all, which is precisely the failure mode that
                // leaves a character sliding around in its bind pose. As Humanoid
                // the boss clips retarget onto the same skeletons as everything else.
                if (mi.animationType != ModelImporterAnimationType.Human)
                {
                    mi.animationType = ModelImporterAnimationType.Human;
                    mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }
                if (!mi.importAnimation) { mi.importAnimation = true; changed = true; }

                var takes = mi.importedTakeInfos;
                if (takes != null && takes.Length > 0 && mi.clipAnimations.Length != takes.Length)
                {
                    var defs = new List<ModelImporterClipAnimation>(takes.Length);
                    foreach (var take in takes)
                    {
                        string name = string.IsNullOrEmpty(take.defaultClipName) ? take.name : take.defaultClipName;
                        if (name == "T-Pose" || name == "T") continue;   // a rest pose, not an animation

                        defs.Add(new ModelImporterClipAnimation
                        {
                            name = name,
                            takeName = take.name,
                            firstFrame = take.bakeStartTime * take.sampleRate,
                            lastFrame = take.bakeStopTime * take.sampleRate,
                            // Cycles loop, one-shots do not. Getting this wrong is
                            // the difference between a patrol that walks and a
                            // patrol that takes one step and freezes.
                            loopTime = IsCycle(name),
                            keepOriginalOrientation = true,
                            keepOriginalPositionY = true,
                            keepOriginalPositionXZ = true,
                            lockRootRotation = false,
                        });
                    }
                    mi.clipAnimations = defs.ToArray();
                    clips += defs.Count;
                    changed = true;
                }

                if (changed) { mi.SaveAndReimport(); split++; }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.Refresh();
        Debug.Log($"[EnemyAnim] Imported {split} librar{(split == 1 ? "y" : "ies")}, {clips} clips defined. " +
                  "Now run Tools > Enemies > 2 - Build Animation Set.");
    }

    // Anything that has to tile seamlessly while a state is held. Everything else
    // plays once and hands back to the state machine.
    private static bool IsCycle(string n)
    {
        string s = n.ToLowerInvariant();
        if (s.Contains("_pose")) return false;
        return s.Contains("idle") || s.Contains("walking") || s.Contains("running")
            || s.Contains("blocking") || s.Contains("aiming") || s.Contains("shooting")
            || s.Contains("sneaking") || s.Contains("crawling") || s.Contains("crouching")
            || s.Contains("holding") || s.Contains("spellcasting") || s.Contains("cheering");
    }

    private static List<string> LibraryFiles()
    {
        var files = new List<string>();
        foreach (var dir in new[] { MediumDir, LargeDir })
        {
            if (!Directory.Exists(dir)) continue;
            files.AddRange(Directory.GetFiles(dir, "*.fbx", SearchOption.TopDirectoryOnly)
                                    .Select(p => p.Replace('\\', '/')));
        }
        return files;
    }

    // ---------------------------------------------------------------------

    [MenuItem("Tools/Enemies/2 - Build Animation Set", priority = 2)]
    public static void BuildSet()
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var path in LibraryFiles())
        {
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                    clips[c.name] = c;   // later libraries win; the skeleton pack is loaded last
            }
        }

        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("Build animation set",
                "No clips found. Run 'Tools > Enemies > 1 - Import Animation Libraries' first — " +
                "the FBX libraries in this project have never been split into clips.", "OK");
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>(SetPath);
        bool isNew = set == null;
        if (isNew) set = ScriptableObject.CreateInstance<EnemyAnimationSet>();

        AnimationClip C(string n) => clips.TryGetValue(n, out var c) ? c : null;
        AnimationClip[] A(params string[] ns) => ns.Select(C).Where(c => c != null).ToArray();

        set.idles = A("Idle_A", "Idle_B", "Skeletons_Idle");
        set.idleBusiness = A("Interact", "PickUp", "Use_Item");
        set.walks = A("Walking_A", "Walking_B", "Walking_C", "Skeletons_Walking");
        set.runs = A("Running_A", "Running_B");
        set.runHoldingBow = C("Running_HoldingBow");

        // NO KICKS. Melee_Unarmed_Attack_Kick reads as a brawl move on a
        // skeleton carrying a sword, and it was showing up on rank-and-file
        // enemies mid-fight. Armed swings only.
        set.attacksUnarmed = A("Melee_Unarmed_Attack_Punch_A");
        set.attacks1H = A("Melee_1H_Attack_Chop", "Melee_1H_Attack_Slice_Diagonal",
                          "Melee_1H_Attack_Slice_Horizontal", "Melee_1H_Attack_Stab");
        set.attacks2H = A("Melee_2H_Attack_Chop", "Melee_2H_Attack_Slice",
                          "Melee_2H_Attack_Spin", "Melee_2H_Attack_Stab");
        set.attacksDualWield = A("Melee_Dualwield_Attack_Chop", "Melee_Dualwield_Attack_Slice",
                                 "Melee_Dualwield_Attack_Stab");
        set.spellcasts = A("Ranged_Magic_Shoot", "Ranged_Magic_Spellcasting", "Ranged_Magic_Raise");
        set.summon = C("Ranged_Magic_Summon");
        set.bow = A("Ranged_Bow_Release", "Ranged_Bow_Draw", "Ranged_Bow_Aiming_Idle");

        set.hits = A("Hit_A", "Hit_B");
        set.deaths = A("Death_A", "Death_B", "Skeletons_Death");
        set.blockHit = C("Melee_Block_Hit");

        set.spawnGround = C("Skeletons_Spawn_Ground") ?? C("Spawn_Ground");
        set.awakenStanding = C("Skeletons_Awaken_Standing");
        set.awakenFloor = C("Skeletons_Awaken_Floor");
        set.taunts = A("Skeletons_Taunt", "Skeletons_Taunt_Longer");
        set.resurrect = C("Skeletons_Death_Resurrect");

        // Smash stays — it is a two-fisted overhead, not a kick — but nothing
        // else unarmed.
        set.bossAttacks = A("Melee_2H_Attack", "Melee_1H_Slash", "Melee_Unarmed_Smash");
        set.bossSlam = C("Melee_2H_Slam") ?? C("Melee_Unarmed_Smash");

        if (isNew) AssetDatabase.CreateAsset(set, SetPath);
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        EnemyAnimationSet.ClearCache();

        // Report what did NOT resolve. A silently-empty role is the failure this
        // whole pipeline is most likely to produce and the hardest to notice: the
        // game just goes on looking exactly as repetitive as before.
        var missing = new List<string>();
        void Need(string role, Object v) { if (v == null) missing.Add(role); }
        void NeedAny(string role, AnimationClip[] v) { if (v == null || v.Length == 0) missing.Add(role); }
        NeedAny("idles", set.idles); NeedAny("walks", set.walks); NeedAny("runs", set.runs);
        NeedAny("hits", set.hits); NeedAny("deaths", set.deaths);
        NeedAny("attacks1H", set.attacks1H); NeedAny("attacks2H", set.attacks2H);
        NeedAny("spellcasts", set.spellcasts); NeedAny("taunts", set.taunts);
        Need("spawnGround", set.spawnGround); Need("summon", set.summon); Need("bossSlam", set.bossSlam);

        string summary = $"[EnemyAnim] Set built from {clips.Count} clips -> {SetPath}\n" +
                         $"  idles {set.idles.Length}, walks {set.walks.Length}, runs {set.runs.Length}, " +
                         $"hits {set.hits.Length}, deaths {set.deaths.Length}, " +
                         $"1H {set.attacks1H.Length}, 2H {set.attacks2H.Length}, dual {set.attacksDualWield.Length}, " +
                         $"spells {set.spellcasts.Length}, bow {set.bow.Length}, taunts {set.taunts.Length}, " +
                         $"boss {set.bossAttacks.Length}";
        if (missing.Count > 0) Debug.LogWarning(summary + "\n  EMPTY ROLES: " + string.Join(", ", missing));
        else Debug.Log(summary);

        Selection.activeObject = set;
    }

    // Says what each state of the shared enemy controller is ACTUALLY playing.
    //
    // The states are named after the clips they were built with — Idle_A plays
    // Idle_A — so a state whose clip no longer matches its own name is the
    // signature of a scrambled controller: clip references are stored as fileIDs
    // into an FBX, and re-importing that FBX can move which clip sits at each id.
    // That failure is completely silent in play mode; here it is one line.
    [MenuItem("Tools/Enemies/Report Enemy Animator States", priority = 19)]
    public static void ReportStates()
    {
        const string path = "Assets/Skeletons/characters/fbx/EnemyAnimator.controller";
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (ac == null)
        {
            Debug.LogWarning($"[EnemyAnim] No controller at {path}.");
            return;
        }

        var sb = new System.Text.StringBuilder($"[EnemyAnim] {System.IO.Path.GetFileName(path)} states:\n");
        int suspicious = 0, empty = 0;

        foreach (var layer in ac.layers)
        {
            if (layer.stateMachine == null) continue;
            foreach (var child in layer.stateMachine.states)
            {
                var st = child.state;
                if (st == null) continue;
                string clipName = st.motion != null ? st.motion.name : "(none)";
                if (st.motion == null) empty++;

                // Compare only the leading token, so "Idle_A" matching "Idle_A"
                // passes while "Idle_A" playing "Death_B" does not.
                string a = Head(st.name), b = Head(clipName);
                bool odd = st.motion != null && !string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
                if (odd) suspicious++;

                sb.AppendLine($"  {st.name,-14} -> {clipName}{(odd ? "   <-- does not match its state name" : "")}");
            }
        }

        if (suspicious > 0 || empty > 0)
            Debug.LogWarning(sb + $"\n  {suspicious} state(s) play a clip that does not match their name and {empty} play nothing.\n" +
                             "  If several look shuffled, the FBX their clips live in was re-imported and the fileID\n" +
                             "  references moved. Restore it with:\n" +
                             "    git checkout -- \"Assets/Skeletons/Animations/fbx/Rig_Medium\"\n" +
                             "  then let Unity reimport.");
        else
            Debug.Log(sb + "  All states play a clip matching their name.");
    }

    private static string Head(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        int i = s.IndexOfAny(new[] { '_', ' ', '|' });
        return i > 0 ? s.Substring(0, i) : s;
    }

    [MenuItem("Tools/Enemies/List Every Clip Found", priority = 20)]
    public static void ListClips()
    {
        var names = new List<string>();
        foreach (var path in LibraryFiles())
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                    names.Add($"{c.name}  ({Path.GetFileNameWithoutExtension(path)}, {c.length:F2}s{(c.isLooping ? ", loop" : "")})");
        names.Sort();
        Debug.Log($"[EnemyAnim] {names.Count} clips:\n  " + string.Join("\n  ", names));
    }
}
