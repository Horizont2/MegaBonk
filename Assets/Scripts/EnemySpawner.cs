using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab; // Кого спавнити
    public Transform player;       // Навколо кого спавнити
    
    [Tooltip("How often to spawn an enemy (in seconds)")]
    public float spawnInterval = 1.5f; 
    
    [Tooltip("How far from the player the enemies will appear")]
    public float spawnRadius = 15f; 

    private float timer;

    private void Update()
    {
        if (player == null || enemyPrefab == null) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f; // Скидаємо таймер
        }
    }

    private void SpawnEnemy()
    {
        // Вибираємо випадкову точку на колі навколо гравця
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;

        // Переводимо 2D коло в 3D координати (X та Z)
        Vector3 spawnPos = new Vector3(player.position.x + randomCircle.x, 0.5f, player.position.z + randomCircle.y);

        // Створюємо ворога у цій точці
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}