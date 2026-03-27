using UnityEngine;

public class HammerDamage : MonoBehaviour
{
    [Header("Weapon Stats")]
    [Tooltip("How much damage the hammer deals to enemies.")]
    public float damage = 25f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}