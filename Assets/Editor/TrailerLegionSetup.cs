using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Trailer shot 2 — builds the ground the legion marches over.
//
//   Tools ▸ Lore Trailer ▸ Setup Shot 2 (the legion marches)
//
// ==== THE ENVIRONMENT IS THE STORY ====
//
// The army is only half the shot. What sells it is ground that has already lost.
// Everything below is placed to say something, not to fill space:
//
//   THE ROAD IS DEAD AND THE FIELDS ARE NOT. A bare, trampled corridor runs the
//   length of the march, with living grass either side of it. The audience never
//   has to be told this army has come this way before — the ground says so, and
//   the contrast is what makes the dead part read as dead.
//
//   THE LAND GETS WORSE TOWARD THE HORIZON THEY CAME FROM. Grass thins and dirt
//   takes over with distance up the column. Where they have been is grey; where
//   they are going is still green. That gradient is the entire plot of the shot
//   rendered into a splatmap.
//
//   THE TREES ARE DEAD, AND DEADEST NEAREST THE ROAD. Trees are cleared from the
//   corridor entirely — nothing survives being walked over by an army — thin and
//   broken along its edges, and thicken further out. A uniform scatter would read
//   as decoration; a density that responds to the road reads as consequence.
//
//   THE VALLEY IS SHALLOW, NOT DEEP. The corridor sits slightly below the fields
//   so the camera can look down the line, but not so far that the column hides
//   in it. Terrain here exists to serve one camera move.
//
// Re-running rebuilds everything from scratch rather than stacking another copy.
public static class TrailerLegionSetup
{
    private const string RigName = "LoreTrailer_Legion_Rig";

    private const string GrassLayerPath = "Assets/Layers/GrassLayer.terrainlayer";
    private const string RockLayerPath  = "Assets/Layers/RockLayer.terrainlayer";
    private const string DirtLayerPath  = "Assets/Layers/SandLayer.terrainlayer";
    private const string DeadTreeDir    = "Assets/Prefabs/Trees/Dead_trees";
    private const string GrassDetailDir = "Assets/LowPoly Environment Pack/Prefabs";

    // Terrain metrics, in metres. Big enough that the column runs off both ends
    // of frame on the long lens, small enough to stay cheap.
    private const int   Size = 500;
    private const int   Height = 45;
    private const int   HeightRes = 513;
    private const int   AlphaRes = 512;
    private const int   DetailRes = 512;

    // The march runs up the middle along +Z.
    private const float RoadHalfWidth = 13f;
    private const float RoadFeather = 16f;

    [MenuItem("Tools/Lore Trailer/Setup Shot 2 (the legion marches)")]
    public static void Setup() { Build(Vector3.zero, parkOthers: true, showDialog: true); }

    // Built at an offset when it is part of the full trailer, so shot 1's statue
    // and this terrain are never occupying the same ground. They are never live
    // at the same time, but overlapping sets make the scene view unreadable and
    // invite exactly the kind of "why is the camera inside the terrain" hunt this
    // trailer has already had enough of.
    public static GameObject Build(Vector3 worldOrigin, bool parkOthers, bool showDialog)
    {
        var rankPrefabs = LoadRankPrefabs();
        if (rankPrefabs.Length == 0)
        {
            EditorUtility.DisplayDialog("Shot 2", "No skeleton prefabs found under Assets/Prefabs. Nothing to march.", "OK");
            return null;
        }

        Undo.SetCurrentGroupName("Setup Trailer Shot 2");
        if (parkOthers) ParkOtherTrailerRigs();
        SilenceGameplaySpawners();

        foreach (var old in TrailerFind.AllByName(RigName))
            if (old != null) Undo.DestroyObjectImmediate(old);

        var rig = new GameObject(RigName);
        Undo.RegisterCreatedObjectUndo(rig, "create legion rig");
        rig.transform.position = worldOrigin;

        Terrain terrain = BuildTerrain(rig.transform, worldOrigin, out Vector3 terrainOrigin);
        Vector3 columnStart = terrainOrigin + new Vector3(Size * 0.5f, 0f, Size * 0.30f);
        columnStart.y = terrain.SampleHeight(columnStart) + terrain.transform.position.y;

        // --- camera ---------------------------------------------------------
        var camGO = new GameObject("Cam_Legion");
        camGO.transform.SetParent(rig.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 900f;      // the column has to be visible to the horizon
        camGO.AddComponent<AudioListener>();

        // --- light + fog ----------------------------------------------------
        var sunGO = new GameObject("Overcast");
        sunGO.transform.SetParent(rig.transform, false);
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        // Flat, sunless, colourless. A sky with a sun in it has weather; this one
        // has no weather, which is worse.
        sun.color = new Color(0.62f, 0.64f, 0.70f);
        sun.intensity = 0.85f;
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(24f, 200f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        // Tuned so visibility dies at roughly 150 m. For exponential-squared fog
        // that is d ~ 1.73 / range. This does most of the work of "no visible
        // end": the column does not have to actually reach the horizon, it only
        // has to reach the point where the grey takes it.
        RenderSettings.fogDensity = 0.011f;
        RenderSettings.fogColor = new Color(0.58f, 0.58f, 0.60f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.50f);
        RenderSettings.ambientEquatorColor = new Color(0.32f, 0.32f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.15f);

        // --- the march ------------------------------------------------------
        var columnGO = new GameObject("Legion");
        columnGO.transform.SetParent(rig.transform, false);
        columnGO.transform.position = columnStart;

        DressRoadside(rig.transform, terrain, terrainOrigin);

        var march = columnGO.AddComponent<TrailerLegionMarch>();
        march.rankPrefabs = rankPrefabs;
        march.bossPrefabs = LoadBossPrefabs();
        march.shotCamera = cam;
        march.marchDirection = Vector3.forward;

        MarkDirty();
        if (!showDialog) return rig;

        Selection.activeGameObject = rig;
        SceneView.lastActiveSceneView?.FrameSelected();

        EditorUtility.DisplayDialog("Shot 2 ready",
            "Built LoreTrailer_Legion_Rig: terrain, painted ground, dead trees, fog and the marching column.\n\n" +
            "Press Play.\n\n" +
            "THE SHOT\n" +
            "  Beat A — camera down in the grass at boot height, wide lens. The army towers. Fear.\n" +
            "  Beat B — one unbroken crane up. No cut, so nobody gets to look away.\n" +
            "  Beat C — high, and the lens goes LONG. Telephoto compression stacks the ranks so\n" +
            "           the column reads far denser and longer than the unit count. Grandeur.\n\n" +
            "TUNING (on TrailerLegionMarch)\n" +
            "  • ranksAlive / unitsPerRank — the BUDGET, not the army. Ranks recycle behind the\n" +
            "    camera, so the column is endless whatever these say. Raise only if it looks thin.\n" +
            "  • highFov — the single most important value in the shot. Lower = denser, longer army.\n" +
            "  • animateWithinDistance — Animators are the whole cost of a crowd. Drop it if the\n" +
            "    frame rate suffers; at range nobody can tell a walk cycle from a pose.\n" +
            "  • bossEveryNRanks — ranks part around each boss, which is what says it outranks them.",
            "OK");
        return rig;
    }

    // Things that give the shot scale and history.
    //
    // A column marching across open ground has no size — it could be toy soldiers
    // on a lawn. Everything here exists to be MEASURED AGAINST:
    //
    //   A FENCE along the road. It is the one object in frame whose real height
    //   everybody already knows, which is what lets the ranks read as tall. It
    //   also says the land was farmed once, which makes what is walking over it
    //   worse.
    //
    //   BOULDERS near the verge, for a second scale reference at a different size
    //   and to break the road's edge from a drawn line into a place.
    //
    //   GROUND MIST lying in the shallow valley. It hides the point where the
    //   column meets the ground in the distance, and a column whose feet you
    //   cannot see is a column whose end you cannot find.
    private static void DressRoadside(Transform parent, Terrain terrain, Vector3 origin)
    {
        var dressing = new GameObject("Roadside");
        dressing.transform.SetParent(parent, false);

        var fence = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Locations/fbx2/CFence.fbx");
        var rocks = AssetDatabase.FindAssets("LProck t:GameObject", new[] { "Assets/Locations/fbx2" })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(g => g != null).ToArray();

        float cx = origin.x + Size * 0.5f;

        if (fence != null)
        {
            // Both verges, running the length of the march. Gapped and jittered:
            // an unbroken line reads as a wall, and a wall reads as level geometry
            // rather than as something a farmer put there.
            for (float z = origin.z + 40f; z < origin.z + Size - 40f; z += 6f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    if (Random.value < 0.22f) continue;          // gaps where it has fallen
                    float x = cx + side * (RoadHalfWidth + Random.Range(1.5f, 3.5f));
                    Vector3 p = new Vector3(x, 0f, z + Random.Range(-0.6f, 0.6f));
                    p.y = terrain.SampleHeight(p) + terrain.transform.position.y;

                    var f = (GameObject)PrefabUtility.InstantiatePrefab(fence, dressing.transform);
                    f.transform.position = p;
                    // Leaning, not upright. Nothing here has been maintained.
                    f.transform.rotation = Quaternion.Euler(Random.Range(-9f, 9f),
                                                            side > 0f ? 0f : 180f,
                                                            Random.Range(-7f, 7f));
                }
            }
        }
        else Debug.LogWarning("[Shot 2] No CFence at Assets/Locations/fbx2 — the road loses its scale reference.");

        if (rocks.Length > 0)
        {
            for (int i = 0; i < 60; i++)
            {
                float x = cx + (Random.value < 0.5f ? -1f : 1f) * Random.Range(RoadHalfWidth, RoadHalfWidth + 45f);
                float z = origin.z + Random.Range(30f, Size - 30f);
                Vector3 p = new Vector3(x, 0f, z);
                p.y = terrain.SampleHeight(p) + terrain.transform.position.y - Random.Range(0.05f, 0.35f);

                var r = (GameObject)PrefabUtility.InstantiatePrefab(rocks[Random.Range(0, rocks.Length)], dressing.transform);
                r.transform.position = p;
                r.transform.rotation = Random.rotation;
                r.transform.localScale = Vector3.one * Random.Range(0.35f, 1.5f);
            }
        }

        BuildGroundMist(dressing.transform, origin);
    }

    private static void BuildGroundMist(Transform parent, Vector3 origin)
    {
        var go = new GameObject("GroundMist");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(origin.x + Size * 0.5f, 0f, origin.z + Size * 0.5f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 14f;
        main.startSpeed = 0.25f;
        main.startSize = 34f;          // few, huge and faint beats many small and busy
        main.startColor = new Color(0.66f, 0.67f, 0.70f, 0.10f);
        main.gravityModifier = 0f;
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;           // already lying there when the shot opens

        var em = ps.emission;
        em.rateOverTime = 7f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        // A flat slab hugging the valley floor: mist that rises off the ground is
        // steam, and steam is the wrong story.
        sh.scale = new Vector3(Size * 0.75f, 1.2f, Size * 0.85f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var pr = ps.GetComponent<ParticleSystemRenderer>();
        Shader psh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psh == null) psh = Shader.Find("Sprites/Default");
        if (psh != null)
        {
            var mat = new Material(psh);
            var tex = TrailerSoftSprite.Get();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            // Transparent, or the soft sprite's alpha is ignored and every mote
            // draws as a flat card.
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            pr.material = mat;
        }
        else pr.enabled = false;
    }

    // ======================= terrain =======================

    private static Terrain BuildTerrain(Transform parent, Vector3 worldOrigin, out Vector3 origin)
    {
        var data = new TerrainData
        {
            heightmapResolution = HeightRes,
            alphamapResolution = AlphaRes,
            baseMapResolution = 1024,
            size = new Vector3(Size, Height, Size),
        };
        data.SetDetailResolution(DetailRes, 16);

        // Re-runnable: creating the folder or the asset a second time would fail
        // and leave a half-built rig behind, which is exactly when someone is most
        // likely to run it again.
        const string genDir = "Assets/TrailerGenerated";
        if (!AssetDatabase.IsValidFolder(genDir)) AssetDatabase.CreateFolder("Assets", "TrailerGenerated");
        const string dataPath = genDir + "/Legion_TerrainData.asset";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath) != null) AssetDatabase.DeleteAsset(dataPath);
        AssetDatabase.CreateAsset(data, dataPath);

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = "Legion_Terrain";
        Undo.RegisterCreatedObjectUndo(go, "create terrain");
        go.transform.SetParent(parent, true);
        go.transform.position = worldOrigin + new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
        origin = go.transform.position;

        var terrain = go.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.detailObjectDistance = 200f;
        terrain.detailObjectDensity = 1f;
        terrain.treeDistance = 700f;
        terrain.treeBillboardDistance = 160f;
        terrain.heightmapPixelError = 3f;

        ShapeLand(data);
        PaintLand(data);
        PlantTrees(terrain, data);
        PlantGrass(data);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return terrain;
    }

    // Rolling ground with a shallow trough down the middle for the march.
    private static void ShapeLand(TerrainData data)
    {
        int res = data.heightmapResolution;
        var h = new float[res, res];
        float seed = Random.Range(0f, 1000f);

        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / (res - 1);
            float nz = (float)z / (res - 1);

            // Two octaves: broad swells plus a little surface variation. More
            // than that starts to look like landscape-generator noise rather
            // than like fields.
            float e = Mathf.PerlinNoise(seed + nx * 2.4f, seed + nz * 2.4f) * 0.65f
                    + Mathf.PerlinNoise(seed + nx * 7f, seed + nz * 7f) * 0.12f;

            // Carve the road: flatten toward a common level, feathering out so
            // the edges are banks rather than a slot cut in the ground.
            float distFromAxis = Mathf.Abs(nx - 0.5f) * Size;
            float road = 1f - Mathf.SmoothStep(RoadHalfWidth, RoadHalfWidth + RoadFeather, distFromAxis);
            e = Mathf.Lerp(e, 0.30f, road * 0.92f);

            h[z, x] = e * 0.45f;
        }
        data.SetHeights(0, 0, h);
    }

    // The splatmap carries the story: a dead road, living fields, and land that
    // gets worse the further up the column you look.
    private static void PaintLand(TerrainData data)
    {
        var layers = new List<TerrainLayer>();
        foreach (string p in new[] { GrassLayerPath, DirtLayerPath, RockLayerPath })
        {
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
            if (l != null) layers.Add(l);
        }
        if (layers.Count == 0)
        {
            Debug.LogWarning("[Shot 2] No terrain layers found under Assets/Layers — the ground will be untextured.");
            return;
        }
        data.terrainLayers = layers.ToArray();

        int res = data.alphamapResolution;
        int n = layers.Count;
        var map = new float[res, res, n];

        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / (res - 1);
            float nz = (float)z / (res - 1);

            float distFromAxis = Mathf.Abs(nx - 0.5f) * Size;
            float road = 1f - Mathf.SmoothStep(RoadHalfWidth * 0.8f, RoadHalfWidth + RoadFeather, distFromAxis);

            // Blight rising toward +Z — the direction the column is coming from.
            // Where they have been is grey; where they are going is still green.
            float blight = Mathf.SmoothStep(0.35f, 1f, nz);
            // Break the boundary up so it is a front, not a painted line.
            blight += (Mathf.PerlinNoise(nx * 9f, nz * 9f) - 0.5f) * 0.35f;
            blight = Mathf.Clamp01(blight);

            float steep = Mathf.Clamp01(data.GetSteepness(nx, nz) / 42f);

            float dirt = Mathf.Clamp01(Mathf.Max(road, blight * 0.85f));
            float rock = steep * 0.8f;
            float grass = Mathf.Clamp01(1f - dirt - rock * 0.6f);

            float sum = grass + dirt + rock;
            if (sum < 0.0001f) { grass = 1f; sum = 1f; }

            map[z, x, 0] = grass / sum;
            if (n > 1) map[z, x, 1] = dirt / sum;
            if (n > 2) map[z, x, 2] = rock / sum;
        }
        data.SetAlphamaps(0, 0, map);
    }

    // Dead trees only, cleared from the road, thin along its banks, thick beyond.
    private static void PlantTrees(Terrain terrain, TerrainData data)
    {
        var prefabs = AssetDatabase.FindAssets("t:GameObject", new[] { DeadTreeDir })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(g => g != null).ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogWarning($"[Shot 2] No dead-tree prefabs under {DeadTreeDir}.");
            return;
        }

        data.treePrototypes = prefabs.Select(p => new TreePrototype { prefab = p }).ToArray();

        var instances = new List<TreeInstance>(2000);
        for (int i = 0; i < 4500; i++)
        {
            float nx = Random.value, nz = Random.value;

            float distFromAxis = Mathf.Abs(nx - 0.5f) * Size;
            // Nothing survives being marched over. Hard clear, then a thin belt of
            // survivors, then real woodland.
            if (distFromAxis < RoadHalfWidth + 3f) continue;
            float openness = Mathf.SmoothStep(RoadHalfWidth + 3f, RoadHalfWidth + 60f, distFromAxis);
            if (Random.value > openness * 0.75f) continue;

            // Clumps, not an even sprinkle: real woodland has gaps.
            if (Mathf.PerlinNoise(nx * 6f, nz * 6f) < 0.42f) continue;
            if (Mathf.Clamp01(data.GetSteepness(nx, nz) / 45f) > 0.6f) continue;

            instances.Add(new TreeInstance
            {
                position = new Vector3(nx, 0f, nz),
                prototypeIndex = Random.Range(0, prefabs.Length),
                // Near the road they are stunted and broken; further out, whole.
                // Kept near 1. Stunting them too far reads as a scaling bug rather
                // than as blighted woodland.
                heightScale = Mathf.Lerp(0.8f, 1.15f, openness) * Random.Range(0.92f, 1.08f),
                widthScale = Random.Range(0.92f, 1.08f),
                rotation = Random.Range(0f, Mathf.PI * 2f),
                color = Color.white,
                lightmapColor = Color.white,
            });
        }
        // SetTreeInstances with snapToHeightmap, NOT the treeInstances setter.
        // TreeInstance.position.y is a NORMALISED height, so the 0 written above
        // means the bottom of the terrain's height range — which is why trees came
        // out buried to the waist. Snapping puts every one on the surface.
        data.RefreshPrototypes();
        terrain.terrainData.SetTreeInstances(instances.ToArray(), true);
        terrain.Flush();
    }

    // Take the grass the GAME paints with, not a lookalike.
    //
    // Picking two meshes out of an asset pack by name gave the fields a different
    // plant from every other scene, and a worse one. The project's own terrains
    // already have detail prototypes authored on them — tuned width, height,
    // colours and all — so the honest thing is to copy those. Only if the project
    // genuinely has none do we fall back to guessing.
    private static DetailPrototype[] BorrowGameGrass()
    {
        DetailPrototype[] best = null;
        foreach (string guid in AssetDatabase.FindAssets("t:TerrainData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("TrailerGenerated")) continue;   // don't copy from ourselves
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (td == null || td.detailPrototypes == null || td.detailPrototypes.Length == 0) continue;
            if (best == null || td.detailPrototypes.Length > best.Length) best = td.detailPrototypes;
        }
        return best;
    }

    private static void PlantGrass(TerrainData data)
    {
        DetailPrototype[] borrowed = BorrowGameGrass();
        if (borrowed != null && borrowed.Length > 0)
        {
            data.detailPrototypes = borrowed;
            Debug.Log($"[Shot 2] Using the game's own {borrowed.Length} grass prototype(s).");
        }
        else
        {
            var meshes = AssetDatabase.FindAssets("Grass t:GameObject", new[] { GrassDetailDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(g => g != null).Take(2).ToArray();

            if (meshes.Length == 0)
            {
                Debug.LogWarning("[Shot 2] No detail prototypes on any terrain in the project and no fallback grass meshes — the fields will be bare.");
                return;
            }

            Debug.LogWarning("[Shot 2] No terrain in the project has detail prototypes to copy; falling back to pack meshes, which will not match the game's grass.");
            data.detailPrototypes = meshes.Select(m => new DetailPrototype
            {
                prototype = m,
                usePrototypeMesh = true,
                useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                minWidth = 1.1f, maxWidth = 2.0f,
                minHeight = 1.0f, maxHeight = 2.0f,
                noiseSpread = 0.35f,
                healthyColor = new Color(0.55f, 0.58f, 0.42f),
                dryColor = new Color(0.52f, 0.46f, 0.30f),
            }).ToArray();
        }

        int res = data.detailResolution;
        int layerCount = data.detailPrototypes.Length;
        for (int layer = 0; layer < layerCount; layer++)
        {
            var d = new int[res, res];
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float nz = (float)z / (res - 1);

                float distFromAxis = Mathf.Abs(nx - 0.5f) * Size;
                // The road is bare. That bareness is only legible because there is
                // grass right up to its edge.
                if (distFromAxis < RoadHalfWidth) continue;
                float openness = Mathf.SmoothStep(RoadHalfWidth, RoadHalfWidth + 25f, distFromAxis);

                // Same blight gradient as the splatmap, so ground cover and ground
                // colour tell the same story instead of contradicting each other.
                float blight = Mathf.Clamp01(Mathf.SmoothStep(0.35f, 1f, nz)
                                             + (Mathf.PerlinNoise(nx * 9f, nz * 9f) - 0.5f) * 0.35f);
                float life = openness * (1f - blight * 0.85f);

                // Density, not presence. The old gate only let grass through where
                // a noise sample cleared a threshold AND then placed 1-5 blades, so
                // the fields came out as a few lonely sprigs on bare dirt. Grass is
                // the ground cover here: it should be continuous, and thin out
                // because the land is dying, not because a noise test failed.
                float clump = 0.55f + 0.45f * Mathf.PerlinNoise(nx * 14f + layer * 30f, nz * 14f);
                float density = life * clump;
                if (density > 0.08f) d[z, x] = Mathf.RoundToInt(Mathf.Lerp(2f, 14f, density));
            }
            data.SetDetailLayer(0, 0, layer, d);
        }
    }

    // ======================= assets =======================

    private static GameObject[] LoadRankPrefabs()
    {
        string[] names = { "Skeleton_Minion", "Skeleton_Warrior", "Skeleton_Rogue", "Skeleton_Mage" };
        return names
            .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{n}.prefab"))
            .Where(g => g != null).ToArray();
    }

    private static GameObject[] LoadBossPrefabs()
    {
        string[] names = { "Boss_Skeleton_1", "Boss_Skeleton_2", "Boss_Skeleton_3" };
        var found = names
            .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{n}.prefab"))
            .Where(g => g != null).ToArray();
        if (found.Length == 0)
            Debug.LogWarning("[Shot 2] No Boss_Skeleton_* prefabs found — the ranks will have nothing to part around.");
        return found;
    }

    // Shut down anything in the scene that spawns or drives enemies.
    //
    // Skeletons appearing in a heap and running off in all directions, with health
    // bars over their heads, are not the column — they are the game's own spawners
    // doing their job in a scene that also happens to contain a trailer rig. The
    // marching units are stripped of their AI individually, but that does nothing
    // about a spawner making NEW ones behind the camera.
    private static void SilenceGameplaySpawners()
    {
        int off = 0;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string n = mb.GetType().Name;
            if (n != "EnemySpawner" && n != "WorldEncounterDirector" && n != "RegionAlertDirector"
                && n != "EnemyEncounterGroup" && n != "RegionTotem" && n != "WorldGenerator") continue;
            if (!mb.enabled) continue;
            Undo.RecordObject(mb, "silence spawner");
            mb.enabled = false;
            off++;
        }

        // And clear out anything they already put in the scene.
        int killed = 0;
        foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ai == null) continue;
            Undo.DestroyObjectImmediate(ai.gameObject);
            killed++;
        }

        if (off > 0 || killed > 0)
            Debug.Log($"[Shot 2] Disabled {off} gameplay spawner(s) and removed {killed} existing enemy/enemies so the shot only contains the column.");
    }

    private static void ParkOtherTrailerRigs()
    {
        foreach (var n in new[] { "LoreTrailer_Rig", "LoreTrailer_ActII_Rig", "LoreTrailer_Part2_Rig", "LoreTrailer_Statue_Rig" })
            foreach (var g in TrailerFind.AllByName(n))
                if (g != null && g.activeSelf) { Undo.RecordObject(g, "park rig"); g.SetActive(false); }
    }

    private static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
