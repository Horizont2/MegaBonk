using UnityEngine;
using System.Collections.Generic; // Потрібно для роботи зі списками

// Створюємо нову структуру для налаштування ворогів в Інспекторі
[System.Serializable]
public class SpawnableEnemy
{
    public GameObject enemyPrefab;
    [Tooltip("З якої хвилини цей ворог почне з'являтися")]
    public float spawnAtMinute = 0f;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    // Тепер це список, де можна вказати час появи для кожного!
    public SpawnableEnemy[] enemyPool;
    public Transform player;
    public float baseSpawnInterval = 1.5f;
    public float spawnRadius = 15f;

    private float timer;

    private void Update()
    {
        if (player == null || enemyPool == null || enemyPool.Length == 0) return;

        timer += Time.deltaTime;

        float minutes = GameManager.survivalTime / 60f;
        float currentSpawnInterval = Mathf.Max(0.3f, baseSpawnInterval / (1f + minutes * 0.2f));

        if (timer >= currentSpawnInterval)
        {
            SpawnEnemy(minutes);
            timer = 0f;
        }
    }

    private void SpawnEnemy(float minutesSurvived)
    {
        // 1. ШУКАЄМО, ХТО ВЖЕ МОЖЕ З'ЯВЛЯТИСЯ
        List<GameObject> availableEnemies = new List<GameObject>();
        foreach (SpawnableEnemy se in enemyPool)
        {
            if (minutesSurvived >= se.spawnAtMinute)
            {
                availableEnemies.Add(se.enemyPrefab);
            }
        }

        // Якщо масив порожній (наприклад, всі стоять з 1 хвилини, а гра тільки почалась)
        if (availableEnemies.Count == 0) return;

        // 2. Рахуємо позицію
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        float spawnX = player.position.x + randomCircle.x;
        float spawnZ = player.position.z + randomCircle.y;
        float spawnY = 0.5f;

        if (Terrain.activeTerrain != null)
        {
            Vector3 worldPos = new Vector3(spawnX, 0, spawnZ);
            spawnY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y + 1.5f;
        }

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);

        // 3. ВИБИРАЄМО ВИПАДКОВОГО ДОСТУПНОГО ВОРОГА
        int randomIndex = Random.Range(0, availableEnemies.Count);
        GameObject selectedPrefab = availableEnemies[randomIndex];

        GameObject newEnemy = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

        EnemyAI enemyScript = newEnemy.GetComponent<EnemyAI>();
        if (enemyScript != null)
        {
            enemyScript.maxHealth *= (1f + minutesSurvived * 0.4f);
            enemyScript.damage *= (1f + minutesSurvived * 0.15f);
            enemyScript.moveSpeed *= Mathf.Min(1.5f, 1f + minutesSurvived * 0.05f);
            enemyScript.xpRewardMultiplier = 1f + (minutesSurvived * 0.2f);
        }
    }
}