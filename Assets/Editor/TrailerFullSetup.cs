using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the whole trailer, in a scene of its own.
//
//   Tools ▸ Lore Trailer ▸ Setup FULL TRAILER (own scene)
//
// ==== WHY ITS OWN SCENE ====
//
// The new shots were being built into Trailer_Lvl_1, which already held an ENTIRE
// earlier trailer: a master sequence director, nine undead-pursuit skeletons, a
// horse and rider, a foliage recolouring pass over 1406 renderers, rain,
// lightning, terrain seasons and a second audio listener — all live, all running
// alongside the new work.
//
// That one fact accounts for nearly every symptom chased for days: a horse
// galloping under every shot; thousands of console warnings a second from an old
// component writing to an animator parameter the skeletons do not have, which was
// enough to lock a laptop hard; shot 1 never reporting that it had finished,
// because another director was contending for the camera and the timescale; and
// the lag that looked like the new scene being too heavy and was not.
//
// A cinematic needs a controlled stage. Building one inside another one is the
// mistake, and no amount of defensive code inside the shots fixes it properly —
// so the trailer now gets an empty scene, and everything in it is there because
// this tool put it there.
public static class TrailerFullSetup
{
    private const string ChainName = "LoreTrailer_Sequence";
    private const string ScenePath = "Assets/Scenes/Trailer_Shots.unity";

    // Far enough apart that neither set can be mistaken for part of the other in
    // the scene view, and well inside float precision.
    private static readonly Vector3 Shot2Origin = new Vector3(2000f, 0f, 0f);

    [MenuItem("Tools/Lore Trailer/Setup FULL TRAILER (own scene)")]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Build the trailer scene",
                "This creates a NEW, EMPTY scene for the trailer and builds both shots into it.\n\n" +
                $"It will be saved as {ScenePath}.\n\n" +
                "Your current scene will be closed — you will be asked to save it first if it has changes.\n\n" +
                "The trailer needs a stage of its own: built into a scene that already contains another " +
                "cinematic, the two fight over the camera, the audio listener and the timescale.",
                "Build it", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // An empty scene has no lighting settings worth speaking of, and each shot
        // sets its own fog and ambient anyway — but skybox-less flat black is a
        // better neutral than whatever the last scene left behind.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.skybox = null;

        GameObject shot1 = TrailerStatueSetup.Build(parkOthers: false, showDialog: false);
        GameObject shot2 = TrailerLegionSetup.Build(Shot2Origin, parkOthers: false, showDialog: false);

        if (shot1 == null || shot2 == null)
        {
            EditorUtility.DisplayDialog("Full trailer",
                "One of the shots could not be built — see the console. The scene has been created but is incomplete.", "OK");
            return;
        }

        // The shots must not fire the moment their rig switches on; the sequencer
        // starts them on cue.
        var s1 = shot1.GetComponentInChildren<TrailerStatueShot>(true);
        if (s1 != null) s1.autoPlay = false;
        var s2 = shot2.GetComponentInChildren<TrailerLegionMarch>(true);
        if (s2 != null) s2.autoPlay = false;

        var chainGO = new GameObject(ChainName);
        var chain = chainGO.AddComponent<TrailerShotChain>();
        chain.shotRigs = new[] { shot1, shot2 };

        // Off until the sequencer turns them on, so pressing Play does not start
        // both shots at once with two cameras and two audio listeners fighting.
        shot1.SetActive(false);
        shot2.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = chainGO;

        EditorUtility.DisplayDialog("Trailer scene ready",
            $"Built {ScenePath} with both shots chained under '{ChainName}'.\n\n" +
            "Press Play — shot 1 runs, ends on the shaft taking the lens, and shot 2 starts from black.\n\n" +
            "THIS SCENE IS THE TRAILER. Keep it clean: anything else added here will compete with the " +
            "shots for the camera, the audio listener and the timescale, which is exactly what made the " +
            "previous attempts stall and spam the console.\n\n" +
            "To work on one shot alone, open this scene and use Setup Shot 1 / Setup Shot 2 — those park " +
            "everything else and leave the one shot playing.",
            "OK");
    }
}
