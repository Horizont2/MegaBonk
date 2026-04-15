using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CampfireInteract : MonoBehaviour
{
    [Header("Heal Settings")]
    public float healPerSecond = 10f;
    public float healRadius = 5f;

    [Header("Visual Effects")]
    public ParticleSystem healEffect;

    private PlayerController playerInZone;

    private void Awake()
    {
        SphereCollider triggerZone = GetComponent<SphereCollider>();
        triggerZone.isTrigger = true;
        triggerZone.radius = healRadius;

        if (healEffect != null) healEffect.Stop();
    }

    private void Update()
    {
        if (playerInZone != null)
        {
            // 1. Перевірка на відстань (якщо OnTriggerExit чомусь не спрацював)
            float distance = Vector3.Distance(transform.position, playerInZone.transform.position);

            if (distance <= healRadius + 1f) // +1 для запасу
            {
                // 2. Лікуємо
                playerInZone.Heal(healPerSecond * Time.deltaTime);

                // 3. ПЕРЕМІЩУЄМО ЕФЕКТ ДО ГРАВЦЯ
                if (healEffect != null)
                {
                    // Ефект літає за гравцем, але трохи вище землі
                    healEffect.transform.position = playerInZone.transform.position + Vector3.up * 0.1f;

                    if (!healEffect.isPlaying) healEffect.Play();
                }
            }
            else
            {
                // Гравець далеко, але OnTriggerExit не спрацював - примусово чистимо
                StopHealing();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = other.GetComponent<PlayerController>();
            if (playerInZone == null) playerInZone = other.GetComponentInParent<PlayerController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopHealing();
        }
    }

    private void StopHealing()
    {
        playerInZone = null;
        if (healEffect != null) healEffect.Stop();
    }
}