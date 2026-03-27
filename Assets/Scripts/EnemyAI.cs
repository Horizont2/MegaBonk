using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 20f;
    public float moveSpeed = 4f;
    public float damage = 10f;

    [Header("Combat Settings")]
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Drops & UI")]
    public GameObject xpCrystalPrefab;
    public GameObject damagePopupPrefab; // NEW: Reference for the floating text

    [Header("Targeting")]
    public Transform target;

    [HideInInspector] public float xpRewardMultiplier = 1f; // Controlled by EnemySpawner

    private float currentHealth;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Rigidbody rb;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        // Move using physics velocity
        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        // --- SPAWN DAMAGE POPUP ---
        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position, Quaternion.identity);
            DamagePopup popupScript = popup.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damageAmount);
        }

        StartCoroutine(FlashEffect());
        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashEffect()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            meshRenderer.material.color = originalColor;
        }
    }

    private void Die()
    {
        if (xpCrystalPrefab != null)
        {
            GameObject crystalObj = Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);

            XpCrystal crystalScript = crystalObj.GetComponent<XpCrystal>();
            if (crystalScript != null)
            {
                crystalScript.xpAmount *= xpRewardMultiplier; // Apply economy scaling
            }
        }
        Destroy(gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    lastAttackTime = Time.time;
                }
            }
        }
    }
}