using UnityEngine;

public class MapLootSpawner : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject xpCrystalPrefab;
    public int amountToSpawn = 150;
    public float scatterRadius = 150f;

    private void Start()
    {
        // Pre-warm crystal pool
        if (ObjectPool.Instance != null && xpCrystalPrefab != null)
            ObjectPool.Instance.Prewarm(xpCrystalPrefab, amountToSpawn);

        // Delay spawn to ensure terrain is fully generated
        Invoke(nameof(ScatterLoot), 0.5f);
    }

    private void ScatterLoot()
    {
        if (xpCrystalPrefab == null) return;

        for (int i = 0; i < amountToSpawn; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = new Vector3(randomPoint.x, 0, randomPoint.y);

            if (Terrain.activeTerrain != null)
            {
                float terrainY = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;
                spawnPos.y = terrainY + 0.8f;
            }

            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Get(xpCrystalPrefab, spawnPos, Quaternion.identity);
            else
                Instantiate(xpCrystalPrefab, spawnPos, Quaternion.identity);
        }
    }
}