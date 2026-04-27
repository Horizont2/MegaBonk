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

    [Header("Points of Interest (Багаття, Намети, Бочки)")]
    public GameObject[] poiPrefabs;
    public int maxPOIs = 15;
    public float maxPOISteepness = 4f;

    [Header("Extraction Settings (Коні з возом)")]
    public GameObject extractionCartPrefab;
    public int extractionCartsAmount = 3; // Скільки точок евакуації спавнити

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

        terrain.terrainData = GenerateHeights(terrain.terrainData);
        PaintTerrain(terrain.terrainData);
        PopulateBiomes();
        SpawnPOIs();

        // --- НОВЕ: Спавнимо коней з возами ---
        SpawnExtractionCarts();

        // Садимо гравця на землю
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float groundY = terrain.SampleHeight(player.transform.position) + terrain.transform.position.y;
            player.transform.position = new Vector3(player.transform.position.x, groundY + 1.5f, player.transform.position.z);
        }
    }

    private float GetTemperature(float normX, float normZ)
    {
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
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;
                float maxAmplitude = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (float)x / width * scale * frequency + offsetX;
                    float yCoord = (float)y / height * scale * frequency + offsetZ;

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
                    perlinValue = 1f - Mathf.Abs(perlinValue * 2f - 1f);
                    perlinValue *= perlinValue;

                    noiseHeight += perlinValue * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                float normalizedHeight = noiseHeight / maxAmplitude;

                if (terraceCount > 0)
                {
                    normalizedHeight = Mathf.Round(normalizedHeight * terraceCount) / terraceCount;
                }

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
                float normX = (float)x / aWidth;
                float normY = (float)y / aHeight;

                float temp = GetTemperature(normX, normY);
                float steepness = terrainData.GetSteepness(normX, normY);
                float[] weights = new float[4];

                weights[2] = Mathf.Clamp01(Mathf.InverseLerp(0.45f, 0.35f, temp));
                weights[1] = Mathf.Clamp01(Mathf.InverseLerp(0.55f, 0.65f, temp));
                weights[0] = 1f - weights[2] - weights[1];
                weights[3] = Mathf.Clamp01(Mathf.InverseLerp(35f, 45f, steepness));

                float remain = 1f - weights[3];
                weights[0] *= remain;
                weights[1] *= remain;
                weights[2] *= remain;

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

        treeContainer.SetParent(this.transform);
        rockContainer.SetParent(this.transform);
        grassContainer.SetParent(this.transform);
        logContainer.SetParent(this.transform);

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

            if (steepness > 45f)
            {
                if (Random.value > 0.98f)
                {
                    GameObject rockPrefab = localTemp > 0.6f ? GetRandomPrefab(desertRocks) : (localTemp < 0.4f ? GetRandomPrefab(snowRocks) : GetRandomPrefab(forestRocks));
                    if (rockPrefab != null)
                    {
                        Vector3 cliffNormal = terrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
                        Quaternion cliffRot = Quaternion.LookRotation(cliffNormal) * Quaternion.Euler(0, 0, Random.Range(0, 360f));

                        GameObject obj = Instantiate(rockPrefab, new Vector3(worldX, worldY, worldZ), cliffRot, rockContainer);
                        obj.transform.localScale *= Random.Range(1.5f, 3f);
                    }
                }
                continue;
            }

            float density = Mathf.PerlinNoise(normalizedX * clusterScale + offsetX, normalizedZ * clusterScale + offsetZ);
            GameObject prefabToSpawn = null;
            Transform targetContainer = null;

            if (density > forestThreshold && steepness <= 25f)
            {
                targetContainer = treeContainer;
                if (localTemp > 0.6f) prefabToSpawn = GetRandomPrefab(desertTrees);
                else if (localTemp < 0.4f) prefabToSpawn = GetRandomPrefab(snowTrees);
                else prefabToSpawn = GetRandomPrefab(forestTrees);

                if (prefabToSpawn != null)
                {
                    GameObject obj = Instantiate(prefabToSpawn, new Vector3(worldX, worldY, worldZ), prefabToSpawn.transform.rotation, targetContainer);
                    obj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.World);
                    obj.transform.localScale *= Random.Range(0.8f, 1.2f);
                }
            }
            else if (density < 0.3f)
            {
                if (Random.value > 0.93f)
                {
                    targetContainer = rockContainer;
                    GameObject rockBase = localTemp > 0.6f ? GetRandomPrefab(desertRocks) : (localTemp < 0.4f ? GetRandomPrefab(snowRocks) : GetRandomPrefab(forestRocks));

                    if (rockBase != null)
                    {
                        int clusterSize = Random.Range(1, 5);
                        for (int c = 0; c < clusterSize; c++)
                        {
                            float ox = Random.Range(-3f, 3f);
                            float oz = Random.Range(-3f, 3f);
                            float cy = terrain.SampleHeight(new Vector3(worldX + ox, 0, worldZ + oz)) + transform.position.y;

                            GameObject obj = Instantiate(rockBase, new Vector3(worldX + ox, cy, worldZ + oz), rockBase.transform.rotation, targetContainer);
                            obj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.World);
                            obj.transform.localScale *= Random.Range(0.5f, 1.5f);
                        }
                    }
                }
            }
            else
            {
                float rand = Random.value;
                if (rand > 0.92f)
                {
                    prefabToSpawn = GetRandomPrefab(logPrefabs);
                    targetContainer = logContainer;
                }
                else if (rand > 0.75f)
                {
                    targetContainer = grassContainer;
                    if (localTemp > 0.6f) prefabToSpawn = GetRandomPrefab(desertGrass);
                    else if (localTemp < 0.4f) prefabToSpawn = GetRandomPrefab(snowGrass);
                    else prefabToSpawn = GetRandomPrefab(forestGrass);
                }

                if (prefabToSpawn != null)
                {
                    GameObject obj = Instantiate(prefabToSpawn, new Vector3(worldX, worldY, worldZ), prefabToSpawn.transform.rotation, targetContainer);
                    obj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.World);
                    obj.transform.localScale *= Random.Range(0.8f, 1.2f);
                }
            }
        }
    }

    private void SpawnPOIs()
    {
        if (poiPrefabs == null || poiPrefabs.Length == 0) return;

        Transform poiContainer = new GameObject("POIContainer").transform;
        poiContainer.SetParent(this.transform);

        float w = terrain.terrainData.size.x;
        float l = terrain.terrainData.size.z;
        int spawnedCount = 0;

        for (int i = 0; i < 2000; i++)
        {
            if (spawnedCount >= maxPOIs) break;

            float px = Random.Range(20f, w - 20f);
            float pz = Random.Range(20f, l - 20f);

            float worldX = transform.position.x + px;
            float worldZ = transform.position.z + pz;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + transform.position.y;

            float normalizedX = px / w;
            float normalizedZ = pz / l;
            float steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);

            if (steepness > maxPOISteepness) continue;

            GameObject prefabToSpawn = GetRandomPrefab(poiPrefabs);
            Vector3 spawnPos = new Vector3(worldX, worldY, worldZ);

            GameObject obj = Instantiate(prefabToSpawn, spawnPos, prefabToSpawn.transform.rotation, poiContainer);
            obj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.World);
            spawnedCount++;
        }
    }

    private void SpawnExtractionCarts()
    {
        if (extractionCartPrefab == null) return;

        float w = terrain.terrainData.size.x;
        float l = terrain.terrainData.size.z;
        int spawnedCarts = 0;

        for (int i = 0; i < 5000; i++) // 5000 спроб знайти ідеальне місце
        {
            if (spawnedCarts >= extractionCartsAmount) break; // Якщо вже 3 є, виходимо

            float px = Random.Range(30f, w - 30f);
            float pz = Random.Range(30f, l - 30f);
            float worldX = transform.position.x + px;
            float worldZ = transform.position.z + pz;

            float normalizedX = px / w;
            float normalizedZ = pz / l;
            float steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);

            // Шукаємо дуже рівне місце (нахил менше 3 градусів)
            if (steepness < 3f)
            {
                float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + transform.position.y;

                // Спавнимо воза
                Instantiate(extractionCartPrefab, new Vector3(worldX, worldY, worldZ), Quaternion.Euler(0, Random.Range(0, 360f), 0));

                spawnedCarts++; // Записуємо, що один з'явився
            }
        }

        if (spawnedCarts < extractionCartsAmount)
        {
            Debug.LogWarning($"Змогли заспавнити тільки {spawnedCarts} возів з {extractionCartsAmount}. Мало рівних місць!");
        }
    }

    private GameObject GetRandomPrefab(GameObject[] array)
    {
        if (array == null || array.Length == 0) return null;
        return array[Random.Range(0, array.Length)];
    }
}