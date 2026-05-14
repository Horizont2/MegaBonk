using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public SpawnableEnemy[] enemyPool;
    public Transform player;
    public float baseSpawnInterval = 1.5f;

    [Header("Spawn Area")]
    [Tooltip("Мінімальна відстань від гравця (безпечна зона)")]
    public float minSpawnRadius = 10f;
    [Tooltip("Максимальна відстань від гравця")]
    public float maxSpawnRadius = 20f;

    private float timer;
    private WorldGenerator worldGen;

    private void Start()
    {
        // Знаходимо генератор світу при старті (якщо він є на сцені)
        worldGen = FindFirstObjectByType<WorldGenerator>();
    }

    private void Update()
    {
        // --- ФІКС ЗАВАНТАЖЕННЯ: Чекаємо, поки світ повністю побудується ---
        if (worldGen != null && !WorldGenerator.IsGenerationDone) return;

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
        List<GameObject> availableEnemies = new List<GameObject>();
        foreach (SpawnableEnemy se in enemyPool)
        {
            if (minutesSurvived >= se.spawnAtMinute)
            {
                availableEnemies.Add(se.enemyPrefab);
            }
        }

        if (availableEnemies.Count == 0) return;

        // Спавн у кільці (не впритул, і не занадто далеко)
        float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector2 randomCircle = Random.insideUnitCircle.normalized * randomDist;

        float spawnX = player.position.x + randomCircle.x;
        float spawnZ = player.position.z + randomCircle.y;
        float spawnY = 0.5f;

        if (Terrain.activeTerrain != null)
        {
            Vector3 worldPos = new Vector3(spawnX, 0, spawnZ);
            spawnY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y;
        }

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);

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

        // АНІМАЦІЯ ПОЯВИ З-ПІД ЗЕМЛІ
        StartCoroutine(RiseFromGroundRoutine(newEnemy));
    }

    private IEnumerator RiseFromGroundRoutine(GameObject enemy)
    {
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null) ai.isCinematicFrozen = true; // Заморожуємо ШІ

        Vector3 finalPos = enemy.transform.position;
        enemy.transform.position = finalPos - new Vector3(0, 2.5f, 0); // Ставимо під землю

        float duration = 1.5f; // Скільки секунд вилазить
        float elapsed = 0f;

        while (elapsed < duration && enemy != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // SmoothStep для плавності

            enemy.transform.position = Vector3.Lerp(finalPos - new Vector3(0, 2.5f, 0), finalPos, t);
            yield return null;
        }

        if (enemy != null)
        {
            enemy.transform.position = finalPos;
            if (ai != null) ai.isCinematicFrozen = false; // Розморожуємо, тепер може бити
        }
    }
}