using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    [Tooltip("Maximum health of the enemy.")]
    public float maxHealth = 20f;
    
    [Tooltip("How fast the enemy moves towards the player.")]
    public float moveSpeed = 4f;
    
    [Tooltip("How much damage this enemy deals to the player on contact.")]
    public float damage = 10f;

    [Header("Combat Settings")]
    [Tooltip("How many seconds between attacks if touching the player.")]
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Targeting")]
    [Tooltip("The target the enemy will follow. It will auto-find the Player if left empty.")]
    public Transform target;

    // Internal variables
    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("EnemyAI: Cannot find the Player! Make sure the player has the 'Player' tag.");
            }
        }
    }

    private void Update()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; 

        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    // --- COLLISION & DAMAGE ---
    private void OnCollisionStay(Collision collision)
    {
        // Check if we are touching the Player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if enough time has passed since the last attack
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    lastAttackTime = Time.time; // Reset the attack timer
                }
            }
        }
    }
}