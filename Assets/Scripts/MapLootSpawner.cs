using UnityEngine;

public class MapLootSpawner : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject xpCrystalPrefab;
    public int amountToSpawn = 150; // Кількість луту на карті
    public float scatterRadius = 150f; // На якій відстані розкидати лут

    private void Start()
    {
        // Чекаємо півсекунди, щоб ландшафт 100% встиг згенеруватися, а потім розкидаємо лут
        Invoke(nameof(ScatterLoot), 0.5f);
    }

    private void ScatterLoot()
    {
        if (xpCrystalPrefab == null) return;

        for (int i = 0; i < amountToSpawn; i++)
        {
            // Вибираємо випадкову точку в межах великого кола
            Vector2 randomPoint = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = new Vector3(randomPoint.x, 0, randomPoint.y);

            // Знаходимо точну висоту гори в цій точці
            if (Terrain.activeTerrain != null)
            {
                float terrainY = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;
                spawnPos.y = terrainY + 0.8f; // Трохи піднімаємо над землею
            }

            // Створюємо кристал
            Instantiate(xpCrystalPrefab, spawnPos, Quaternion.identity);
        }
    }
}