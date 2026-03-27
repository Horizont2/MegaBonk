using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    public float baseSpawnInterval = 1.5f;
    public float spawnRadius = 15f;

    private float timer;

    private void Update()
    {
        if (player == null || enemyPrefab == null) return;

        timer += Time.deltaTime;

        // Кожну хвилину спавн стає на 20% швидшим. Мінімальний інтервал - 0.3 сек (дуже багато ворогів)
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
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(player.position.x + randomCircle.x, 0.5f, player.position.z + randomCircle.y);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // --- ЕКОНОМІКА (СИСТЕМА МНОЖНИКІВ) ---
        EnemyAI enemyScript = newEnemy.GetComponent<EnemyAI>();
        if (enemyScript != null)
        {
            // Здоров'я росте на 40% щохвилини
            enemyScript.maxHealth *= (1f + minutesSurvived * 0.4f);

            // Шкода росте на 15% щохвилини
            enemyScript.damage *= (1f + minutesSurvived * 0.15f);

            // Швидкість росте на 5% щохвилини (але не більше ніж в 1.5 рази від базової)
            enemyScript.moveSpeed *= Mathf.Min(1.5f, 1f + minutesSurvived * 0.05f);

            // Кристали з цього ворога будуть давати на 20% більше досвіду щохвилини
            enemyScript.xpRewardMultiplier = 1f + (minutesSurvived * 0.2f);
        }
    }
}