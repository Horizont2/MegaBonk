using UnityEngine;
using System.Collections.Generic;

public class HammerDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float baseDamage = 15f;
    public float knockbackForce = 6f;
    public float hitCooldown = 0.4f; // Захист від багаторазового влучання в один кадр

    private PlayerController player;
    private CameraFollow cameraFollow;

    // Словник для запам'ятовування, коли ми востаннє били конкретного ворога
    private Dictionary<Collider, float> lastHitTimes = new Dictionary<Collider, float>();

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<PlayerController>();

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Перевіряємо кулдаун для цього конкретного ворога
            if (lastHitTimes.TryGetValue(other, out float lastTime))
            {
                if (Time.time < lastTime + hitCooldown) return; // Ще зарано бити знову
            }

            lastHitTimes[other] = Time.time;

            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                float actualDamage = baseDamage;
                bool isCrit = false;

                if (player != null)
                {
                    actualDamage *= player.globalDamageMultiplier;
                    isCrit = Random.value <= player.globalCritChance;
                    if (isCrit) actualDamage *= 2.5f; // Крит завдає 2.5х шкоди
                }

                enemy.TakeDamage(actualDamage, isCrit);

                // Відкидання від молота (від центру гравця)
                if (player != null)
                {
                    Vector3 pushDir = (enemy.transform.position - player.transform.position).normalized;
                    pushDir.y = 0;
                    enemy.ApplyKnockback(pushDir, isCrit ? knockbackForce * 1.5f : knockbackForce, isCrit ? 0.6f : 0.3f);
                }

                // Візуальна віддача (Соковитість)
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Player_HitEnemy);
                if (cameraFollow != null) cameraFollow.TriggerShake(isCrit ? 0.2f : 0.05f, 0.1f);
            }
        }
    }
}