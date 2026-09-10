using UnityEditor;
using UnityEngine;

// Builds the whole trailer so far and chains the shots together.
//
//   Tools ▸ Lore Trailer ▸ Setup FULL TRAILER (shot 1 → shot 2)
//
// Each shot stays a self-contained rig with its own camera, lighting, fog and
// set. The sequencer's only job is to keep exactly one of them live and to hand
// over when a shot says it is done.
//
// Shot 2 is built a long way from the origin. The two rigs are never on at the
// same time, so they could overlap harmlessly — but a statue standing inside a
// terrain makes the scene view unreadable, and this trailer has already spent
// enough time on "why is the camera inside something".
public static class TrailerFullSetup
{
    private const string ChainName = "LoreTrailer_Sequence";

    // Far enough apart that neither set can be mistaken for part of the other in
    // the scene view, and well inside float precision.
    private static readonly Vector3 Shot2Origin = new Vector3(2000f, 0f, 0f);

    [MenuItem("Tools/Lore Trailer/Setup FULL TRAILER (shot 1 → shot 2)")]
    public static void Setup()
    {
        Undo.SetCurrentGroupName("Setup Full Trailer");

        foreach (var old in TrailerFind.AllByName(ChainName))
            if (old != null) Undo.DestroyObjectImmediate(old);

        // parkOthers: false — each builder would otherwise switch the other one
        // off as it goes, and the last one built would be the only one left on.
        GameObject shot1 = TrailerStatueSetup.Build(parkOthers: false, showDialog: false);
        GameObject shot2 = TrailerLegionSetup.Build(Shot2Origin, parkOthers: false, showDialog: false);

        if (shot1 == null || shot2 == null)
        {
            EditorUtility.DisplayDialog("Full trailer",
                "One of the shots could not be built — see the console. Nothing was chained.", "OK");
            return;
        }

        // The shots must not fire the moment their rig switches on; the sequencer
        // starts them on cue.
        var s1 = shot1.GetComponentInChildren<TrailerStatueShot>(true);
        if (s1 != null) s1.autoPlay = false;
        var s2 = shot2.GetComponentInChildren<TrailerLegionMarch>(true);
        if (s2 != null) s2.autoPlay = false;

        var chainGO = new GameObject(ChainName);
        Undo.RegisterCreatedObjectUndo(chainGO, "create trailer sequence");
        var chain = chainGO.AddComponent<TrailerShotChain>();
        chain.shotRigs = new[] { shot1, shot2 };

        // Off until the sequencer turns them on, so pressing Play does not start
        // both shots at once with two cameras and two audio listeners fighting.
        shot1.SetActive(false);
        shot2.SetActive(false);

        Selection.activeGameObject = chainGO;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Full trailer ready",
            "Built both shots and chained them under 'LoreTrailer_Sequence'.\n\n" +
            "Press Play — shot 1 runs, ends on the shaft taking the lens, and shot 2 starts from black.\n\n" +
            "The sequencer waits for each shot to REPORT that it is done rather than counting seconds, " +
            "so retuning any beat inside a shot cannot drift the hand-over.\n\n" +
            "To work on one shot alone, use the individual Setup Shot 1 / Shot 2 items — those park " +
            "everything else and leave the shot playing on its own.",
            "OK");
    }
}
