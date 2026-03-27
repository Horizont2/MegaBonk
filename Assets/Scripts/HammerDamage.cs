using UnityEngine;

public class HammerDamage : MonoBehaviour
{
    public float damage = 10f;

    // This method is called automatically when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit has the tag "Enemy"
        if (other.CompareTag("Enemy"))
        {
            // Try to get the EnemyAI script from the hit object
            EnemyAI enemy = other.GetComponent<EnemyAI>();

            if (enemy != null)
            {
                // Apply damage
                enemy.TakeDamage(damage);
            }
        }
    }
}