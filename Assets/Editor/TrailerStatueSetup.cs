using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Trailer shot 1 — the king's statue.
//
//   Tools ▸ Lore Trailer ▸ Setup Shot 1 (statue breaks open)
//
// Builds the whole shot from assets already in the project: the statue mesh, a
// dedicated camera, the crack/ray rig, rock debris and a night grade. Everything
// lands under one root so it can be parked like the other trailer rigs and the
// take recorded on its own.
//
// Re-running is safe: the rig is rebuilt from scratch rather than duplicated.
// That mattered enough to be worth stating — earlier trailer tools left six
// copies of their rig in the scene, and because GameObject.Find skips disabled
// objects, nobody noticed until the camera started flying to the wrong place.
public static class TrailerStatueSetup
{
    private const string RigName = "LoreTrailer_Statue_Rig";
    private const string StatuePath = "Assets/Locations/fbx/Statue.fbx";
    private const string RockDir = "Assets/Locations/fbx2";

    [MenuItem("Tools/Lore Trailer/Setup Shot 1 (statue breaks open)")]
    public static void Setup()
    {
        var statueAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StatuePath);
        if (statueAsset == null)
        {
            EditorUtility.DisplayDialog("Shot 1",
                $"Couldn't find the statue at:\n{StatuePath}\n\nPoint the tool at another mesh or restore that asset.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Setup Trailer Shot 1");

        ParkOtherTrailerRigs();
        RebuildRig(out GameObject rig);

        // --- Statue -----------------------------------------------------------
        GameObject statue = (GameObject)PrefabUtility.InstantiatePrefab(statueAsset, rig.transform);
        statue.name = "KingStatue";
        statue.transform.position = Vector3.zero;
        statue.transform.rotation = Quaternion.Euler(0f, 200f, 0f);   // three-quarter view reads better than face-on
        statue.transform.localScale = Vector3.one * 3.2f;

        // The crack placement raycasts against the statue, so it needs a collider.
        // The imported mesh has none, and without one every crack would fall back
        // to the bounding shell and the light would leak out of thin air.
        EnsureCollider(statue);

        // --- Camera -----------------------------------------------------------
        var camGO = new GameObject("Cam_Statue");
        camGO.transform.SetParent(rig.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.05f;
        cam.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();
        // Placed on the side the shot approaches from; TrailerStatueShot reads
        // this position to decide its approach azimuth, so composition set here
        // by hand survives.
        camGO.transform.position = new Vector3(-9.5f, 4.5f, -10.5f);
        camGO.transform.LookAt(new Vector3(0f, 4.5f, 0f));

        // --- Grade ------------------------------------------------------------
        var sunGO = new GameObject("Moonlight");
        sunGO.transform.SetParent(rig.transform, false);
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        // Cold and dim. The violet coming out of the statue has to be the only
        // warm-ish thing on screen, or it stops reading as unnatural.
        sun.color = new Color(0.42f, 0.52f, 0.72f);
        sun.intensity = 0.35f;
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(18f, 140f, 0f);

        // --- Shot director ----------------------------------------------------
        var shot = rig.AddComponent<TrailerStatueShot>();
        shot.statue = statue.transform;
        shot.shotCamera = cam;
        shot.debrisPrefabs = LoadRocks();

        Selection.activeGameObject = rig;
        SceneView.lastActiveSceneView?.FrameSelected();
        MarkSceneDirty();

        EditorUtility.DisplayDialog("Shot 1 ready",
            "Built LoreTrailer_Statue_Rig.\n\n" +
            "Press Play. Tune it on the TrailerStatueShot component:\n\n" +
            "PACING\n" +
            "  • establish — the dead-quiet hold before the first crack. This is what makes it land.\n" +
            "  • buildDuration — first fracture to full burst\n" +
            "  • stepIntervalStart / End — how fast the cracks crawl, and how much they accelerate\n\n" +
            "LOOK\n" +
            "  • motesPerRay — dust drifting through the shafts. Turn this to 0 and see how flat they go;\n" +
            "    it is the single biggest thing making them read as volumetric.\n" +
            "  • rayLensOffset — how wide the shafts sweep past the lens. Too low and they shrink to dots.\n" +
            "  • rayFlicker — a perfectly steady beam is the clearest CGI tell there is.\n" +
            "  • crackWidthScale — thickness of the fissures on the stone\n\n" +
            "CAMERA\n" +
            "  • framingBias — 0.5 is dead centre and looks like a product turntable\n" +
            "  • handheldBase / handheldAtPeak — the camera should get less steady as the stone fails\n\n" +
            "It wants a DARK scene: the light out of the statue should be the brightest thing in frame.",
            "OK");
    }

    private static void ParkOtherTrailerRigs()
    {
        foreach (var n in new[] { "LoreTrailer_Rig", "LoreTrailer_ActII_Rig", "LoreTrailer_Part2_Rig" })
        {
            foreach (var g in TrailerFind.AllByName(n))
            {
                if (g == null || !g.activeSelf) continue;
                Undo.RecordObject(g, "park rig");
                g.SetActive(false);
            }
        }
    }

    private static void RebuildRig(out GameObject rig)
    {
        foreach (var old in TrailerFind.AllByName(RigName))
            if (old != null) Undo.DestroyObjectImmediate(old);

        rig = new GameObject(RigName);
        Undo.RegisterCreatedObjectUndo(rig, "create statue rig");
    }

    private static void EnsureCollider(GameObject statue)
    {
        if (statue.GetComponentInChildren<Collider>() != null) return;

        foreach (var mf in statue.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    private static GameObject[] LoadRocks()
    {
        var rocks = new List<GameObject>();
        foreach (string guid in AssetDatabase.FindAssets("LProck t:GameObject", new[] { RockDir }))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null) rocks.Add(go);
        }
        if (rocks.Count == 0)
            Debug.LogWarning($"[Shot 1] No LProck* meshes found under {RockDir} — the statue will crack and flare but shed no debris.");
        return rocks.ToArray();
    }

    private static void MarkSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
