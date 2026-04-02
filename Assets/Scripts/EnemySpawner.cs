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

        // Spawn becomes 20% faster every minute. Minimum interval is 0.3 sec (heavy swarm)
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
        float spawnX = player.position.x + randomCircle.x;
        float spawnZ = player.position.z + randomCircle.y;

        float spawnY = 0.5f; // Fallback height

        if (Terrain.activeTerrain != null)
        {
            // spawnX and spawnZ are already world coordinates! Just pass them directly.
            Vector3 worldPos = new Vector3(spawnX, 0, spawnZ);

            // SampleHeight returns local Y. We add terrain's world Y, plus 1.5f so they drop safely onto the ground
            spawnY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y + 1.5f;
        }

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // --- ECONOMY (MULTIPLIER SYSTEM) ---
        EnemyAI enemyScript = newEnemy.GetComponent<EnemyAI>();
        if (enemyScript != null)
        {
            // Health increases by 40% every minute
            enemyScript.maxHealth *= (1f + minutesSurvived * 0.4f);

            // Damage increases by 15% every minute
            enemyScript.damage *= (1f + minutesSurvived * 0.15f);

            // Speed increases by 5% every minute (capped at 1.5x base speed)
            enemyScript.moveSpeed *= Mathf.Min(1.5f, 1f + minutesSurvived * 0.05f);

            // Crystals from this enemy will give 20% more XP every minute
            enemyScript.xpRewardMultiplier = 1f + (minutesSurvived * 0.2f);
        }
    }
}