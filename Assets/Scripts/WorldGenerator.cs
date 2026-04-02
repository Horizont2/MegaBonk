using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class WorldGenerator : MonoBehaviour
{
    [Header("Mountain & Arena Settings")]
    public float depth = 50f; // Max height of mountains
    public float scale = 6f;  // Zoom level of the noise

    [Tooltip("Makes valleys flatter and peaks sharper (Try 2 or 3)")]
    [Range(1f, 5f)] public float peakSharpness = 2.5f;

    [Tooltip("Forces mountains to grow around the edges of the map to create an arena")]
    public float edgeMountainMultiplier = 1.5f;

    private float offsetX;
    private float offsetZ;

    [Header("Environment Prefabs")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] grassPrefabs;

    [Header("Spawn Densities")]
    public int treeCount = 300;
    public int rockCount = 150;
    public int grassCount = 1000;

    private Terrain terrain;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();

        // NEW: Check if we are continuing or starting fresh
        if (PlayerPrefs.GetInt("IsContinuing", 0) == 1)
        {
            // Load the saved map seed so the mountains look exactly the same
            offsetX = PlayerPrefs.GetFloat("MapSeedX", 0f);
            offsetZ = PlayerPrefs.GetFloat("MapSeedZ", 0f);
        }
        else
        {
            // Generate a brand new map seed and save it
            offsetX = Random.Range(0f, 9999f);
            offsetZ = Random.Range(0f, 9999f);
            PlayerPrefs.SetFloat("MapSeedX", offsetX);
            PlayerPrefs.SetFloat("MapSeedZ", offsetZ);
            PlayerPrefs.Save();
        }

        terrain.terrainData = GenerateHeights(terrain.terrainData);

        SpawnObjects(treePrefabs, treeCount, "TreesContainer");
        SpawnObjects(rockPrefabs, rockCount, "RocksContainer");
        SpawnObjects(grassPrefabs, grassCount, "GrassContainer");
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
                // 1. Base Perlin Noise
                float xCoord = (float)x / width * scale + offsetX;
                float yCoord = (float)y / height * scale + offsetZ;
                float rawNoise = Mathf.PerlinNoise(xCoord, yCoord);

                // Make valleys flat and peaks sharp using a Power function
                float sharpenedNoise = Mathf.Pow(rawNoise, peakSharpness);

                // 2. Arena Edge Logic (Distance from center)
                // Calculate how far this pixel is from the center (0 = center, 1 = edge)
                float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float normalizedDist = distFromCenter / centerX;

                // Make the edges curve upwards dramatically
                float edgeWall = Mathf.Pow(normalizedDist, 4f) * edgeMountainMultiplier;

                // Combine normal noise with the edge wall
                heights[x, y] = Mathf.Clamp01(sharpenedNoise + edgeWall);
            }
        }

        terrainData.SetHeights(0, 0, heights);
        terrainData.size = new Vector3(terrainData.size.x, depth, terrainData.size.z);
        return terrainData;
    }

    private void SpawnObjects(GameObject[] prefabs, int count, string containerName)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        GameObject container = new GameObject(containerName);
        container.transform.SetParent(this.transform);

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(0, terrainWidth);
            float randomZ = Random.Range(0, terrainLength);

            Vector3 localPos = new Vector3(randomX, 0, randomZ);
            float y = terrain.SampleHeight(localPos + transform.position);

            // OPTIONAL: Don't spawn objects on very steep mountains
            // If you want trees only in the flat arena, you can check the height here.

            Vector3 spawnPos = new Vector3(randomX, y, randomZ) + transform.position;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Quaternion randomRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject obj = Instantiate(prefab, spawnPos, randomRot);
            obj.transform.SetParent(container.transform);

            float randomScale = Random.Range(0.8f, 1.2f);
            obj.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
        }
    }
}