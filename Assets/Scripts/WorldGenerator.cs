using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Terrain))]
public class WorldGenerator : MonoBehaviour
{
    [Header("Mountain & Arena Settings")]
    public float depth = 120f;
    public float scale = 2.5f;
    [Range(1, 6)] public int octaves = 4;
    public float persistence = 0.45f;
    public float lacunarity = 2.5f;
    [Range(1f, 5f)] public float peakSharpness = 2.5f;
    public int terraceCount = 12;
    public float edgeMountainMultiplier = 2.5f;

    private float offsetX;
    private float offsetZ;

    [Header("Biome Textures (Terrain Layers)")]
    public TerrainLayer grassLayer;
    public TerrainLayer sandLayer;
    public TerrainLayer snowLayer;
    public TerrainLayer rockLayer;

    [Header("Biome & Cluster Settings")]
    public int spawnAttempts = 35000;
    public float clusterScale = 12f;
    [Range(0f, 1f)] public float forestThreshold = 0.50f;
    public float globalBiomeScale = 2.5f;

    [Header("Breakable Trees")]
    public GameObject[] forestTrees;
    public GameObject[] desertTrees;
    public GameObject[] snowTrees;

    [Header("Grass & Bushes (Food)")]
    public GameObject[] forestGrass;
    public GameObject[] desertGrass;
    public GameObject[] snowGrass;

    [Header("Breakable Rocks & Logs")]
    public GameObject[] forestRocks;
    public GameObject[] desertRocks;
    public GameObject[] snowRocks;
    public GameObject[] logPrefabs;

    [Header("Points of Interest")]
    public GameObject[] poiPrefabs;
    public int maxPOIs = 15;
    public float maxPOISteepness = 12f;
    public float poiClearanceRadius = 4f;

    [Header("Extraction Settings")]
    public GameObject extractionCartPrefab;
    public int extractionCartsAmount = 3;
    public float cartClearanceRadius = 6f;

    private Terrain terrain;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();

        if (PlayerPrefs.GetInt("IsContinuing", 0) == 1)
        {
            offsetX = PlayerPrefs.GetFloat("MapSeedX", 0f);
            offsetZ = PlayerPrefs.GetFloat("MapSeedZ", 0f);
        }
        else
        {
            offsetX = Random.Range(0f, 9999f);
            offsetZ = Random.Range(0f, 9999f);
            PlayerPrefs.SetFloat("MapSeedX", offsetX);
            PlayerPrefs.SetFloat("MapSeedZ", offsetZ);
            PlayerPrefs.Save();
        }

        AdjustSettingsForBiome();

        terrain.terrainData = GenerateHeights(terrain.terrainData);
        PaintTerrain(terrain.terrainData);

        PopulateBiomes();
        Physics.SyncTransforms();

        SpawnPOIs();
        SpawnExtractionCarts();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float groundY = terrain.SampleHeight(player.transform.position) + terrain.transform.position.y;
            player.transform.position = new Vector3(player.transform.position.x, groundY + 1.5f, player.transform.position.z);
        }
    }

    private void AdjustSettingsForBiome()
    {
        bool isRegionMission = PlayerPrefs.GetInt("IsRegionMission", 0) == 1;
        if (isRegionMission)
        {
            int biomeType = PlayerPrefs.GetInt("RegionBiomeType", 0);

            if (biomeType == 1)
            {
                peakSharpness = 1.8f;
                edgeMountainMultiplier = 1.5f;
                terraceCount = 0;
            }
            else if (biomeType == 2)
            {
                peakSharpness = 3.5f;
                edgeMountainMultiplier = 3.5f;
                terraceCount = 15;
            }
            else
            {
                peakSharpness = 2.5f;
                edgeMountainMultiplier = 2.5f;
                terraceCount = 12;
            }
        }
    }

    private float GetTemperature(float normX, float normZ)
    {
        bool isRegionMission = PlayerPrefs.GetInt("IsRegionMission", 0) == 1;
        if (isRegionMission)
        {
            int biomeType = PlayerPrefs.GetInt("RegionBiomeType", 0);
            if (biomeType == 1) return 0.8f;
            if (biomeType == 2) return 0.2f;
            return 0.5f;
        }

        return Mathf.PerlinNoise(normX * globalBiomeScale + offsetX + 500f, normZ * globalBiomeScale + offsetZ + 500f);
    }

    private TerrainData GenerateHeights(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];
        float centerX = width / 2f;
        float centerY = height / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amplitude = 1f; float frequency = 1f; float noiseHeight = 0f; float maxAmplitude = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (float)x / width * scale * frequency + offsetX;
                    float yCoord = (float)y / height * scale * frequency + offsetZ;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    perlinValue = 1f - Mathf.Abs(perlinValue * 2f - 1f);
                    perlinValue *= perlinValue;
                    noiseHeight += perlinValue * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= persistence; frequency *= lacunarity;
                }

                float normalizedHeight = noiseHeight / maxAmplitude;
                if (terraceCount > 0) normalizedHeight = Mathf.Round(normalizedHeight * terraceCount) / terraceCount;

                float sharpenedNoise = Mathf.Pow(normalizedHeight, peakSharpness);
                float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float edgeWall = Mathf.Pow(distFromCenter / centerX, 4f) * edgeMountainMultiplier;

                heights[x, y] = Mathf.Clamp01(sharpenedNoise + edgeWall);
            }
        }

        terrainData.SetHeights(0, 0, heights);
        terrainData.size = new Vector3(terrainData.size.x, depth, terrainData.size.z);
        return terrainData;
    }

    private void PaintTerrain(TerrainData terrainData)
    {
        if (grassLayer == null || sandLayer == null || snowLayer == null || rockLayer == null) return;

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, sandLayer, snowLayer, rockLayer };
        int aWidth = terrainData.alphamapWidth;
        int aHeight = terrainData.alphamapHeight;
        float[,,] splatmapData = new float[aWidth, aHeight, 4];

        for (int y = 0; y < aHeight; y++)
        {
            for (int x = 0; x < aWidth; x++)
            {
                float temp = GetTemperature((float)x / aWidth, (float)y / aHeight);
                float steepness = terrainData.GetSteepness((float)x / aWidth, (float)y / aHeight);
                float[] weights = new float[4];

                if (temp >= 0.65f) weights[1] = 1f;
                else if (temp <= 0.35f) weights[2] = 1f;
                else weights[0] = 1f;

                weights[3] = Mathf.Clamp01(Mathf.InverseLerp(35f, 45f, steepness));
                float remain = 1f - weights[3];
                weights[0] *= remain; weights[1] *= remain; weights[2] *= remain;

                splatmapData[y, x, 0] = weights[0];
                splatmapData[y, x, 1] = weights[1];
                splatmapData[y, x, 2] = weights[2];
                splatmapData[y, x, 3] = weights[3];
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private void PopulateBiomes()
    {
        float w = terrain.terrainData.size.x;
        float l = terrain.terrainData.size.z;

        Transform treeContainer = new GameObject("TreesContainer").transform;
        Transform rockContainer = new GameObject("RocksContainer").transform;
        Transform grassContainer = new GameObject("GrassContainer").transform;
        Transform logContainer = new GameObject("LogsContainer").transform;

        treeContainer.SetParent(this.transform); rockContainer.SetParent(this.transform);
        grassContainer.SetParent(this.transform); logContainer.SetParent(this.transform);

        for (int i = 0; i < spawnAttempts; i++)
        {
            float px = Random.Range(10f, w - 10f);
            float pz = Random.Range(10f, l - 10f);

            float worldX = transform.position.x + px;
            float worldZ = transform.position.z + pz;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + transform.position.y;

            float normalizedX = px / w;
            float normalizedZ = pz / l;
            float steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
            float localTemp = GetTemperature(normalizedX, normalizedZ);

            Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
            Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal);

            // --- Ô²ÊÑ ÏÎÐÎÃ²Â: Òåïåð 0.65 äëÿ ïóñòåë³ ³ 0.35 äëÿ ñí³ãó ñêð³çü! ---
            if (steepness > 45f)
            {
                if (Random.value > 0.98f)
                {
                    GameObject rockPrefab = localTemp >= 0.65f ? GetRandomPrefab(desertRocks) : (localTemp <= 0.35f ? GetRandomPrefab(snowRocks) : GetRandomPrefab(forestRocks));
                    if (rockPrefab != null)
                    {
                        Quaternion randomY = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                        Quaternion rockRot = slopeRotation * randomY * rockPrefab.transform.rotation;
                        GameObject obj = Instantiate(rockPrefab, new Vector3(worldX, worldY, worldZ), rockRot, rockContainer);
                        obj.transform.localScale *= Random.Range(1.5f, 3f);
                    }
                }
                continue;
            }

            float density = Mathf.PerlinNoise(normalizedX * clusterScale + offsetX, normalizedZ * clusterScale + offsetZ);
            GameObject prefabToSpawn = null; Transform targetContainer = null; bool alignToSlope = true;

            if (density > forestThreshold && steepness <= 25f)
            {
                float randomSpawn = Random.value;
                if (randomSpawn > 0.65f)
                {
                    targetContainer = treeContainer; alignToSlope = false;
                    if (localTemp >= 0.65f) prefabToSpawn = GetRandomPrefab(desertTrees);
                    else if (localTemp <= 0.35f) prefabToSpawn = GetRandomPrefab(snowTrees);
                    else prefabToSpawn = GetRandomPrefab(forestTrees);
                }
                else if (randomSpawn > 0.40f)
                {
                    targetContainer = grassContainer;
                    if (localTemp >= 0.65f) prefabToSpawn = GetRandomPrefab(desertGrass);
                    else if (localTemp <= 0.35f) prefabToSpawn = GetRandomPrefab(snowGrass);
                    else prefabToSpawn = GetRandomPrefab(forestGrass);
                }
            }
            else if (density < 0.3f)
            {
                if (Random.value > 0.93f)
                {
                    targetContainer = rockContainer;
                    GameObject rockBase = localTemp >= 0.65f ? GetRandomPrefab(desertRocks) : (localTemp <= 0.35f ? GetRandomPrefab(snowRocks) : GetRandomPrefab(forestRocks));
                    if (rockBase != null)
                    {
                        int clusterSize = Random.Range(1, 5);
                        for (int c = 0; c < clusterSize; c++)
                        {
                            float ox = Random.Range(-3f, 3f); float oz = Random.Range(-3f, 3f);
                            float cy = terrain.SampleHeight(new Vector3(worldX + ox, 0, worldZ + oz)) + transform.position.y;
                            Quaternion randomY = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                            Quaternion clusterRot = slopeRotation * randomY * rockBase.transform.rotation;
                            GameObject obj = Instantiate(rockBase, new Vector3(worldX + ox, cy, worldZ + oz), clusterRot, rockContainer);
                            obj.transform.localScale *= Random.Range(0.5f, 1.5f);
                        }
                    }
                    continue;
                }
            }
            else
            {
                float rand = Random.value;
                if (rand > 0.92f) { prefabToSpawn = GetRandomPrefab(logPrefabs); targetContainer = logContainer; }
                else if (rand > 0.75f)
                {
                    targetContainer = grassContainer;
                    if (localTemp >= 0.65f) prefabToSpawn = GetRandomPrefab(desertGrass);
                    else if (localTemp <= 0.35f) prefabToSpawn = GetRandomPrefab(snowGrass);
                    else prefabToSpawn = GetRandomPrefab(forestGrass);
                }
            }

            if (prefabToSpawn != null)
            {
                Quaternion randomYRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                Quaternion baseRot = prefabToSpawn.transform.rotation;
                Quaternion finalRot = alignToSlope ? (slopeRotation * randomYRot * baseRot) : (randomYRot * baseRot);
                GameObject obj = Instantiate(prefabToSpawn, new Vector3(worldX, worldY, worldZ), finalRot, targetContainer);
                obj.transform.localScale *= Random.Range(0.8f, 1.2f);
            }
        }
    }

    private void SpawnPOIs()
    {
        if (poiPrefabs == null || poiPrefabs.Length == 0) return;
        Transform poiContainer = new GameObject("POIContainer").transform;
        poiContainer.SetParent(this.transform);
        float w = terrain.terrainData.size.x; float l = terrain.terrainData.size.z; int spawnedCount = 0;
        for (int i = 0; i < 3000; i++)
        {
            if (spawnedCount >= maxPOIs) break;
            float px = Random.Range(20f, w - 20f); float pz = Random.Range(20f, l - 20f);
            float worldX = transform.position.x + px; float worldZ = transform.position.z + pz;
            float normalizedX = px / w; float normalizedZ = pz / l;
            float steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
            if (steepness > maxPOISteepness) continue;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + transform.position.y;
            Vector3 spawnPos = new Vector3(worldX, worldY, worldZ);
            if (IsPositionClear(spawnPos, poiClearanceRadius))
            {
                GameObject prefabToSpawn = GetRandomPrefab(poiPrefabs);
                Instantiate(prefabToSpawn, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), poiContainer);
                spawnedCount++;
            }
        }
    }

    private void SpawnExtractionCarts()
    {
        if (extractionCartPrefab == null) return;
        float w = terrain.terrainData.size.x; float l = terrain.terrainData.size.z; int spawnedCarts = 0;
        for (int i = 0; i < 5000; i++)
        {
            if (spawnedCarts >= extractionCartsAmount) break;
            float px = Random.Range(30f, w - 30f); float pz = Random.Range(30f, l - 30f);
            float worldX = transform.position.x + px; float worldZ = transform.position.z + pz;
            float normalizedX = px / w; float normalizedZ = pz / l;
            float steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
            if (steepness < 8f)
            {
                float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + transform.position.y;
                Vector3 spawnPos = new Vector3(worldX, worldY, worldZ);
                if (IsPositionClear(spawnPos, cartClearanceRadius))
                {
                    Instantiate(extractionCartPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                    spawnedCarts++;
                }
            }
        }
    }

    private bool IsPositionClear(Vector3 position, float radius)
    {
        Vector3 checkPos = position + Vector3.up * 1.5f;
        Collider[] colliders = Physics.OverlapSphere(checkPos, radius);
        foreach (Collider col in colliders)
        {
            if (col.GetComponent<TerrainCollider>() != null || col.GetComponent<Terrain>() != null) continue;
            if (col.isTrigger) continue;
            return false;
        }
        return true;
    }

    private GameObject GetRandomPrefab(GameObject[] array)
    {
        if (array == null || array.Length == 0) return null;
        return array[Random.Range(0, array.Length)];
    }
}