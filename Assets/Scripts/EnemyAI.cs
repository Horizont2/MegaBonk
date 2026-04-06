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

    // Base stats (set from prefab, used for reset)
    private float baseMaxHealth;
    private float baseMoveSpeed;
    private float baseDamage;

    private float currentHealth;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private PlayerController playerController;

    private void Awake()
    {
        // 100% FIX: Force all child objects to Layer 9 (enemy layer)
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

        // Cache base stats from prefab defaults
        baseMaxHealth = maxHealth;
        baseMoveSpeed = moveSpeed;
        baseDamage = damage;
    }

    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
    }

    private void Init()
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

        lastAttackTime = -attackCooldown;
    }

    /// <summary>
    /// Reset stats to prefab defaults before applying new scaling.
    /// Called by EnemySpawner before applying difficulty multipliers.
    /// </summary>
    public void ResetStats()
    {
        maxHealth = baseMaxHealth;
        moveSpeed = baseMoveSpeed;
        damage = baseDamage;
        xpRewardMultiplier = 1f;
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

        // Track total damage dealt
        GameStats.totalDamageDealt += damageAmount;

        if (damagePopupPrefab != null)
        {
            GameObject popup;
            if (ObjectPool.Instance != null)
                popup = ObjectPool.Instance.Get(damagePopupPrefab, transform.position, Quaternion.identity);
            else
                popup = Instantiate(damagePopupPrefab, transform.position, Quaternion.identity);

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
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Get(xpCrystalPrefab, transform.position, Quaternion.identity);
            else
                Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        }

        // Track kill stats
        GameStats.totalKills++;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}