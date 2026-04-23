using UnityEngine;

public class POISpawner : MonoBehaviour
{
    [Header("POI Settings")]
    [Tooltip("Префаби локацій (Табір, Руїни і т.д.)")]
    public GameObject[] locationPrefabs;

    [Tooltip("Скільки локацій створити на мапі")]
    public int amountToSpawn = 15;

    [Tooltip("Розмір зони спавну (наприклад, 200 означає від -100 до +100)")]
    public float mapSize = 250f;

    [Header("Placement Rules")]
    [Tooltip("Максимальний кут нахилу землі (в градусах), де може з'явитися локація")]
    public float maxSlopeAngle = 10f;

    private void Start()
    {
        SpawnLocations();
    }

    private void SpawnLocations()
    {
        if (locationPrefabs == null || locationPrefabs.Length == 0 || Terrain.activeTerrain == null) return;

        int spawnedCount = 0;
        int maxAttempts = 2000; // Захист від зависання гри, якщо рівних місць замало
        int currentAttempt = 0;

        // Крутимо цикл, поки не заспавнимо потрібну кількість (або поки не вичерпаємо спроби)
        while (spawnedCount < amountToSpawn && currentAttempt < maxAttempts)
        {
            currentAttempt++;

            // Генеруємо випадкові координати X та Z
            float randomX = Random.Range(-mapSize / 2f, mapSize / 2f);
            float randomZ = Random.Range(-mapSize / 2f, mapSize / 2f);

            // Кидаємо промінь з неба вниз
            Vector3 skyPos = new Vector3(randomX, 1000f, randomZ);

            if (Physics.Raycast(skyPos, Vector3.down, out RaycastHit hit, 2000f))
            {
                // ВИМІРЮЄМО КУТ НАХИЛУ ПОВЕРХНІ
                // hit.normal - це вектор, який дивиться перпендикулярно від землі
                float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

                // Якщо земля достатньо рівна
                if (slopeAngle <= maxSlopeAngle)
                {
                    GameObject prefab = locationPrefabs[Random.Range(0, locationPrefabs.Length)];
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    Instantiate(prefab, hit.point, randomRotation, transform);

                    // Успішно заспавнили, збільшуємо лічильник
                    spawnedCount++;
                }
            }
        }

        if (spawnedCount < amountToSpawn)
        {
            Debug.LogWarning($"Змогли заспавнити лише {spawnedCount} локацій з {amountToSpawn}. Не вистачило рівного місця!");
        }
    }
}