using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 20f;
    public float moveSpeed = 4f;
    public float damage = 10f;

    [Header("Combat Settings")]
    public float attackRange = 1.6f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Drops & UI")]
    public GameObject xpCrystalPrefab;
    public GameObject damagePopupPrefab;

    [Header("Targeting")]
    public Transform target;

    [Header("Ground Settings")]
    public float verticalOffset = 0.5f;

    [HideInInspector] public float xpRewardMultiplier = 1f;

    private float currentHealth;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private PlayerController playerController;

    private void Awake()
    {
        // 100% ФІКС: Примусово переносимо ворога ТА ВСІ ЙОГО ЧАСТИНИ на 9 шар (шар привидів)
        gameObject.layer = 9;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = 9;
        }

        Collider[] enemyCols = GetComponentsInChildren<Collider>(true);
        foreach (Collider eCol in enemyCols)
        {
            eCol.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        currentHealth = maxHealth;

        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 currentPos = transform.position;
        Vector3 direction = (target.position - currentPos).normalized;
        direction.y = 0f;

        Vector3 nextPos = currentPos + direction * moveSpeed * Time.deltaTime;

        if (Terrain.activeTerrain != null)
        {
            float terrainHeight = Terrain.activeTerrain.SampleHeight(nextPos) + Terrain.activeTerrain.transform.position.y;
            nextPos.y = terrainHeight + verticalOffset;
        }

        transform.position = nextPos;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (playerController != null)
                {
                    playerController.TakeDamage(damage);
                    lastAttackTime = Time.time;
                }
            }
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
            Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}