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
    public GameObject damagePopupPrefab;

    [Header("Targeting")]
    public Transform target;

    [HideInInspector] public float xpRewardMultiplier = 1f;

    private float currentHealth;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Rigidbody rb;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        // IMPORTANT: Must be Kinematic to avoid physics glitches
        rb.isKinematic = true;

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

        // 1. Direction to player
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        // 2. Calculate next horizontal move
        Vector3 nextPosition = transform.position + direction * moveSpeed * Time.fixedDeltaTime;

        // 3. GROUND SNAPPING (Fixes floating and climbing)
        if (Terrain.activeTerrain != null)
        {
            // Sample the terrain height at the next point
            float terrainHeight = Terrain.activeTerrain.SampleHeight(nextPosition) + Terrain.activeTerrain.transform.position.y;

            // Snap Y to terrain height + a small offset (0.5 to 1.0 depending on your model)
            nextPosition.y = terrainHeight + 0.7f;
        }

        // 4. Move and Rotate using Rigidbody (Kinematic mode)
        rb.MovePosition(nextPosition);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

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
            if (crystalScript != null) crystalScript.xpAmount *= xpRewardMultiplier;
        }
        Destroy(gameObject);
    }

    // Trigger detection for damage (since we are kinematic/trigger now)
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    lastAttackTime = Time.time;
                }
            }
        }
    }
}