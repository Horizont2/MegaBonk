using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Generation Settings")]
    [Tooltip("Maximum height of the mountains")]
    public float depth = 30f;

    [Tooltip("How 'zoomed in' the map is. Higher value = more hills, lower value = larger mountains")]
    public float scale = 5f;

    [Header("Random Seed")]
    public float offsetX = 100f;
    public float offsetZ = 100f;

    private void Start()
    {
        // Randomize the offset so the map is different every time you play (like Rust!)
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);

        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        // Get the resolution of the terrain (usually 513x513)
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;

        // Apply the generated heights
        terrainData.SetHeights(0, 0, GenerateHeights(width, height));

        // Adjust the total physical size/depth of the terrain
        terrainData.size = new Vector3(terrainData.size.x, depth, terrainData.size.z);

        return terrainData;
    }

    private float[,] GenerateHeights(int width, int height)
    {
        float[,] heights = new float[width, height];

        // Loop through every single point on the terrain grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Calculate Perlin Noise coordinates
                float xCoord = (float)x / width * scale + offsetX;
                float yCoord = (float)y / height * scale + offsetZ;

                // Mathf.PerlinNoise returns a value between 0.0 and 1.0
                heights[x, y] = Mathf.PerlinNoise(xCoord, yCoord);
            }
        }

        return heights;
    }
}