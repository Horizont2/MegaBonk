using UnityEngine;

public class HammerDamage : MonoBehaviour
{
    public float baseDamage = 10f;
    private PlayerController player;

    private void Start()
    {
        // Find the player once to read the damage multiplier
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Calculate total damage: Base Weapon Damage * Meta Progression Multiplier
                float actualDamage = baseDamage;
                if (player != null) actualDamage *= player.globalDamageMultiplier;

                enemy.TakeDamage(actualDamage);
            }
        }
    }
}