using UnityEngine;
using System.Collections; // Потрібно для Coroutine

public class GrenadeLogic : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float delay = 2f;
    public float explosionRadius = 6f;
    public float damage = 200f;

    [Header("Effects & Loot")]
    public GameObject explosionEffect;
    public GameObject crystalPrefab;

    [Header("Game Feel (Juice)")]
    public float baseHitStopDuration = 0.05f;  // Базова зупинка часу
    public float maxHitStopDuration = 0.15f;   // Максимальна зупинка (при величезному натовпі)
    public float baseShakeMagnitude = 0.2f;    // Базова сила тряски
    public float shakeMultiplier = 0.05f;      // Наскільки сильнішає тряска за кожного моба

    private float countdown;
    private bool hasExploded = false;
    private CameraFollow mainCameraScript;

    private void Start()
    {
        countdown = delay;
        // Шукаємо скрипт камери на старті
        if (Camera.main != null)
        {
            mainCameraScript = Camera.main.GetComponent<CameraFollow>();
        }
    }

    private void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0f && !hasExploded)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        int enemyCount = 0;

        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.CompareTag("Enemy")) enemyCount++;
        }

        int multiplier = 1;
        if (enemyCount >= 20) multiplier = 4;
        else if (enemyCount >= 10) multiplier = 2;

        // --- СОКОВИТІСТЬ (GAME FEEL) ---
        if (enemyCount > 0)
        {
            // 1. Рахуємо силу ефектів
            float currentHitStop = Mathf.Clamp(baseHitStopDuration + (enemyCount * 0.005f), baseHitStopDuration, maxHitStopDuration);
            float currentShake = baseShakeMagnitude + (enemyCount * shakeMultiplier);

            // 2. Трусимо камеру (0.3 секунди тривалість)
            if (mainCameraScript != null)
            {
                mainCameraScript.TriggerShake(0.3f, currentShake);
            }

            // 3. Запускаємо зупинку часу (Hit Stop)
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                // Якщо є GameManager, краще додати функцію туди, але поки що зробимо простіше прямо тут
                StartCoroutine(HitStopRoutine(currentHitStop));
            }
            else
            {
                // Якщо немає GameManager, запускаємо прямо з гранати перед її знищенням
                StartCoroutine(HitStopRoutine(currentHitStop));
            }
        }

        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.CompareTag("Enemy"))
            {
                EnemyAI enemy = nearbyObject.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    for (int i = 0; i < multiplier - 1; i++)
                    {
                        if (crystalPrefab != null)
                        {
                            Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                            Instantiate(crystalPrefab, enemy.transform.position + offset, Quaternion.identity);
                        }
                    }
                }
            }
            else if (nearbyObject.CompareTag("Player"))
            {
                PlayerController player = nearbyObject.GetComponent<PlayerController>();
                if (player != null) player.TakeDamage(20f);
            }
        }

        // Замість Destroy робимо об'єкт невидимим і вимикаємо фізику, 
        // щоб Coroutine зупинки часу встигла відпрацювати!
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, maxHitStopDuration + 0.1f);
    }

    // Корутина для зупинки часу
    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // Уповільнюємо час майже до нуля
        yield return new WaitForSecondsRealtime(duration); // Чекаємо реальний час
        Time.timeScale = 1f; // Повертаємо нормальний час
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}