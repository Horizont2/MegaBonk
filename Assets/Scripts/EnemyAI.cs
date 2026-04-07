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

    [Header("Juice Effects")]
    public float spawnBounceTime = 0.35f;
    public float deathShrinkTime = 0.3f;
    public float deathFloatHeight = 2f;

    // Base stats (set from prefab, used for reset)
    private float baseMaxHealth;
    private float baseMoveSpeed;
    private float baseDamage;

    [HideInInspector] public float currentHealth;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    private PlayerController playerController;
    private bool isDying = false;
    private bool isSpawning = false;

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
        isDying = false;

        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        lastAttackTime = -attackCooldown;

        // Spawn bounce animation
        StartCoroutine(SpawnBounce());
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
        if (target == null || isDying || isSpawning) return;

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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("hit");

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
        if (isDying) return;
        isDying = true;

        if (xpCrystalPrefab != null)
        {
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Get(xpCrystalPrefab, transform.position, Quaternion.identity);
            else
                Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        }

        GameStats.totalKills++;

        // Boss death callback (extra drops, stats, sound)
        BossEnemy boss = GetComponent<BossEnemy>();
        if (boss != null)
            boss.OnBossDeath();

        // Hit freeze micro-pause for impact feel
        if (HitFreezeEffect.Instance != null)
            HitFreezeEffect.Instance.Freeze();

        // Play kill sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(boss != null ? "bossDeath" : "enemyDeath");

        StartCoroutine(DeathAnimation());
    }

    private IEnumerator SpawnBounce()
    {
        isSpawning = true;
        float t = 0f;
        transform.localScale = Vector3.zero;

        while (t < spawnBounceTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / spawnBounceTime);
            // Elastic overshoot: goes to ~1.2 then settles
            float elastic = 1f + Mathf.Sin(p * Mathf.PI) * 0.3f * (1f - p);
            float scale = p * elastic;
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
        isSpawning = false;
    }

    private IEnumerator DeathAnimation()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        Color startColor = (meshRenderer != null) ? meshRenderer.material.color : Color.white;

        while (t < deathShrinkTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / deathShrinkTime);

            // Shrink with accelerating curve
            float scale = Mathf.Lerp(1f, 0f, p * p);
            transform.localScale = startScale * scale;

            // Float upward
            transform.position = startPos + Vector3.up * (deathFloatHeight * p);

            // Fade to white
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.Lerp(startColor, Color.white, p);
            }

            yield return null;
        }

        // Reset visual state before returning to pool
        transform.localScale = startScale;
        if (meshRenderer != null)
            meshRenderer.material.color = originalColor;

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}