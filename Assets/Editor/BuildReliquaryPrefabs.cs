using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Authors the three exploration chests as REAL PREFABS.
//
// ==== WHY THIS EXISTS ====
//
// The sites used to be assembled at runtime by a director: find clear ground,
// instantiate a chest model, measure it, scale it, bolt on an Animator, wrap it
// in a LootChest. Every one of those steps was invisible until the game was
// running, and two of them kept going wrong in ways nobody could see from the
// editor — the chest came out the size of a house because the model was measured
// at the wrong moment, and the lid never moved because the controller reference
// was empty in the Resources index.
//
// Baking it into a prefab moves both problems into the editor, where they are
// visible, inspectable and fixable by hand. The prefab is measured ONCE, here,
// with the mesh bounds read straight off the asset; the Animator is a real
// component with a real controller you can see in the inspector; and the size
// that ships is the size you saw in the scene view.
//
// It also unlocks the workflow that matters: a designer builds a location by
// hand, drops Reliquary_Shrine into it, and adds that location to the
// generator's POI list. The chest brings its own guardians, seal and channel —
// nothing has to search the terrain for anywhere to put it.
public static class BuildReliquaryPrefabsTool
{
    private const string Folder = "Assets/Prefabs/Exploration";

    // Used only if the model somehow has no controller AND the set has none.
    private const string FallbackController =
        "Assets/Animated Fantasy Polygon Chest/Animation/Fantasy_Polygon_Chest_Animation_Controller.controller";

    // Grade, chest model, finished height in metres, guardian ring.
    //
    // The heights are the whole point of this file. The pack's meshes are
    // authored several metres tall in their own units, so a chest placed at its
    // authored scale towers over the player — which is exactly what the
    // screenshots showed. A chest is knee-to-hip height on a person; a barrow's
    // is a little grander because it has to look worth a nine-second hold.
    private static readonly (Reliquary.Grade grade, int model, float height, float ring)[] Grades =
    {
        (Reliquary.Grade.Wayside, 0, 0.80f, 3.0f),
        (Reliquary.Grade.Shrine,  1, 0.95f, 3.8f),
        (Reliquary.Grade.Barrow,  2, 1.20f, 5.0f),
    };

    [MenuItem("Tools/Exploration/Build Reliquary Prefabs")]
    public static void Build()
    {
        var set = ReliquarySet.Load();
        if (set == null)
        {
            // Load() caches a miss, so a set built moments ago in the same session
            // would otherwise still read as absent.
            ReliquarySet.ClearCache();
            set = ReliquarySet.Load();
        }
        if (set == null || !set.IsUsable)
        {
            EditorUtility.DisplayDialog("Reliquary",
                "No usable ReliquarySet found.\n\nRun Tools > Exploration > Build Reliquary Set first — the chest " +
                "models, loot and animator controller all come from it.", "OK");
            return;
        }

        Directory.CreateDirectory(Folder);
        var report = new List<string>();
        var built = new GameObject[3];

        foreach (var (grade, modelIndex, height, ring) in Grades)
        {
            var model = set.ChestFor(modelIndex);
            if (model == null) { report.Add($"{grade}: no chest model"); continue; }

            string path = $"{Folder}/Reliquary_{grade}.prefab";
            var root = new GameObject($"Reliquary_{grade}");

            try
            {
                var art = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform.root);
                if (art == null) art = Object.Instantiate(model);
                art.name = "Chest_Model";
                art.transform.SetParent(root.transform, false);
                art.transform.localPosition = Vector3.zero;
                art.transform.localRotation = Quaternion.identity;

                // Measure off the SHARED MESHES rather than Renderer.bounds.
                //
                // Renderer.bounds needs the object live in a scene and, for a
                // skinned mesh, is not reliable on the frame it was created —
                // which is precisely the trap the runtime version fell into. Mesh
                // bounds are baked into the asset and give the same answer every
                // time, in the editor, with nothing rendered.
                Bounds local = MeasureLocal(art);
                if (local.size.y > 0.0001f)
                {
                    float k = height / local.size.y;
                    art.transform.localScale = Vector3.one * k;
                    local = MeasureLocal(art);   // re-read: scale moved it
                }

                // Sit the BOTTOM of the mesh on the prefab's origin, so dropping
                // the prefab on the ground puts the chest on the ground whatever
                // the model's pivot happens to be — and the pivots in this pack
                // are not all in the same place.
                art.transform.localPosition -= new Vector3(0f, local.min.y, 0f);

                // The rig is in the prefab; the Animator component is not. Without
                // this the lid never moves and LootChest quietly falls back to a
                // timer, which is what "the opening animation doesn't work" was.
                var anim = art.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = art.AddComponent<Animator>();

                // NEVER write a null controller over a good one.
                //
                // This is the bug that made "the opening animation doesn't work"
                // survive three rounds of fixes. The chest models ALREADY carry a
                // working Animator with the right controller — but this line used
                // to assign set.chestAnimatorController unconditionally, and when
                // the set had not been rebuilt yet that value was null. Unity then
                // recorded "m_Controller = none" as a PREFAB OVERRIDE, which beats
                // the source prefab's correct one forever after. The result was a
                // chest that looked perfectly wired in the inspector, silently
                // swallowed SetTrigger("Open"), and left LootChest falling back to
                // a plain timer with the lid shut.
                //
                // So: only ever assign something real, and only when there is
                // nothing there already.
                if (anim.runtimeAnimatorController == null)
                {
                    var controller = set.chestAnimatorController;
                    if (controller == null)
                        controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FallbackController);
                    if (controller != null) anim.runtimeAnimatorController = controller;
                    else Debug.LogError($"[Reliquary] {grade}: the chest model has no animator controller and none " +
                                        "could be found. The lid will not move. Run Tools > Exploration > " +
                                        "Build Reliquary Set first.");
                }
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // Something solid to bump into, if the model brought nothing.
                if (root.GetComponentsInChildren<Collider>(true).Length == 0)
                {
                    local = MeasureLocal(art);
                    var box = root.AddComponent<BoxCollider>();
                    box.center = art.transform.localPosition + local.center;
                    box.size = local.size;
                }

                var chest = root.AddComponent<LootChest>();
                chest.chestAnimator = anim;
                chest.possibleLoot = set.chestLoot;
                chest.interactRange = 3.2f;
                // An opened reliquary stays standing. These sit inside locations
                // somebody composed by hand, and a chest that deletes itself ten
                // seconds after being looted punches a hole in the composition.
                chest.destroyDelay = 0f;
                switch (grade)
                {
                    case Reliquary.Grade.Barrow: chest.minLootItems = 6; chest.maxLootItems = 12; break;
                    case Reliquary.Grade.Shrine: chest.minLootItems = 4; chest.maxLootItems = 8; break;
                    default: chest.minLootItems = 2; chest.maxLootItems = 5; break;
                }

                var rel = root.AddComponent<Reliquary>();
                rel.grade = grade;
                rel.spawnGuardians = true;
                rel.guardRadius = ring;
                // OFF by design. A prefab dropped into a location somebody built
                // by hand must not scatter its own banners and bones through their
                // composition. The director turns it on for the sites it places on
                // bare ground, where there is no dressing to clash with.
                rel.buildDecor = false;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                built[(int)grade] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                report.Add($"{grade}: {model.name} scaled to {height:0.00} m -> {path}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Hand the finished prefabs back to the index so the director can place
        // them without knowing any paths.
        set.sitePrefabByGrade = built;
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ReliquarySet.ClearCache();

        Debug.Log("[Reliquary] Prefabs built:\n  " + string.Join("\n  ", report) +
                  "\n\nDrop one into a hand-built location and add that location to the generator's POI list. " +
                  "The chest posts its own guardians, raises its own seal and runs its own channel.");
        if (built[0] != null) Selection.activeObject = built[0];
    }

    // Combined bounds of every mesh under `go`, expressed in go's local space.
    private static Bounds MeasureLocal(GameObject go)
    {
        var toLocal = go.transform.worldToLocalMatrix;
        bool any = false;
        Bounds acc = new Bounds();

        void Add(Transform t, Mesh mesh)
        {
            if (mesh == null) return;
            Bounds mb = mesh.bounds;
            Matrix4x4 m = toLocal * t.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? mb.min.x : mb.max.x,
                    (i & 2) == 0 ? mb.min.y : mb.max.y,
                    (i & 4) == 0 ? mb.min.z : mb.max.z);
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any) { acc = new Bounds(p, Vector3.zero); any = true; }
                else acc.Encapsulate(p);
            }
        }

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) Add(mf.transform, mf.sharedMesh);
        foreach (var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) Add(sk.transform, sk.sharedMesh);
        return any ? acc : new Bounds(Vector3.zero, Vector3.one);
    }
}
