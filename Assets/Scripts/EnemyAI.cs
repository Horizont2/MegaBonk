using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Base Enemy Stats (Level 1)")]
    public float maxHealth = 20f;
    public float moveSpeed = 4f;
    public float damage = 10f;

    [Header("Cinematic Settings")]
    public bool isCinematicFrozen = false;

    [Header("Combat Settings")]
    public float attackRange = 1.6f;
    public float attackCooldown = 1.5f;
    public float attackTelegraphTime = 0.5f;

    [Header("Drops & Economy")]
    public GameObject xpCrystalPrefab;
    public GameObject diamondPrefab;
    [Range(0f, 1f)] public float diamondDropChance = 0.1f;
    public GameObject damagePopupPrefab;

    [Header("Targeting & Swarm")]
    public Transform target;
    public float verticalOffset = 0.0f;
    public float repulsionRadius = 1.5f;
    public float repulsionForce = 4f;

    [HideInInspector] public float xpRewardMultiplier = 1f;

    public bool isInvincible = false;
    private bool isEnraged = false;

    private float currentHealth;
    private float actualMoveSpeed;
    private float randomOffset;

    private MeshRenderer[] meshRenderers;
    private Color[] originalColors;
    private PlayerController playerController;
    private Animator animator;
    private bool isDead = false;

    private Vector3 knockbackVelocity = Vector3.zero;
    private float stunTimer = 0f;
    private float lastAttackTime;
    private bool isPreparingAttack = false;

    private void Awake()
    {
        gameObject.layer = 9;
        int minimapLayer = LayerMask.NameToLayer("MinimapOnly");

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalColors = new Color[meshRenderers.Length];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i].gameObject.layer != minimapLayer) meshRenderers[i].gameObject.layer = 9;
            originalColors[i] = meshRenderers[i].material.color;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        randomOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentRegion != null)
        {
            float hpMult = GameManager.Instance.currentRegion.enemyHpMultiplier;
            float dmgMult = GameManager.Instance.currentRegion.enemyDamageMultiplier;

            maxHealth *= hpMult;
            damage *= dmgMult;
            xpRewardMultiplier = hpMult * 0.5f;
        }

        currentHealth = maxHealth;
        actualMoveSpeed = moveSpeed * Random.Range(0.8f, 1.2f);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (isDead || target == null) return;

        if (knockbackVelocity.magnitude > 0.1f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 10f);
        }

        if (isCinematicFrozen)
        {
            if (animator != null) animator.SetBool("isMoving", true);
            return;
        }

        if (stunTimer > 0 && !isEnraged)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (isPreparingAttack) return;

        Vector3 currentPos = transform.position;
        Vector3 directionToPlayer = (target.position - currentPos).normalized;

        Vector3 repulsion = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(currentPos, repulsionRadius, 1 << 9);
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject && !neighbor.isTrigger)
            {
                Vector3 pushDir = currentPos - neighbor.transform.position;
                float distance = pushDir.magnitude;
                if (distance < repulsionRadius && distance > 0)
                {
                    repulsion += pushDir.normalized * (repulsionRadius - distance);
                }
            }
        }

        float sway = Mathf.PerlinNoise(Time.time * 0.5f, randomOffset) * 2f - 1f;
        Vector3 rightDir = Vector3.Cross(Vector3.up, directionToPlayer).normalized;
        Vector3 swayDirection = rightDir * (sway * 0.5f);

        Vector3 finalDirection = (directionToPlayer + repulsion * repulsionForce + swayDirection).normalized;
        finalDirection.y = 0f;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer <= attackRange)
        {
            if (animator != null) animator.SetBool("isMoving", false);
            if (directionToPlayer != Vector3.zero)
            {
                Vector3 lookDir = directionToPlayer;
                lookDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 15f * Time.deltaTime);
            }
            if (Time.time >= lastAttackTime + attackCooldown && !isPreparingAttack) StartCoroutine(AttackRoutine());
        }
        else
        {
            Vector3 nextPos = currentPos + finalDirection * actualMoveSpeed * Time.deltaTime;
            if (Terrain.activeTerrain != null)
            {
                float terrainHeight = Terrain.activeTerrain.SampleHeight(nextPos) + Terrain.activeTerrain.transform.position.y;
                nextPos.y = terrainHeight + verticalOffset;
            }
            transform.position = nextPos;

            if (finalDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalDirection), 10f * Time.deltaTime);
                if (animator != null) animator.SetBool("isMoving", true);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isPreparingAttack = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Enemy_Telegraph);

        Color tempColor = isEnraged ? Color.black : Color.red;
        SetColor(tempColor);

        yield return new WaitForSeconds(attackTelegraphTime);
        ResetColor();

        if (!isDead && Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            lastAttackTime = Time.time;
            if (animator != null) animator.SetTrigger("Attack");
        }
        isPreparingAttack = false;
    }

    public void ExecuteAttackDamage()
    {
        if (isDead || target == null || playerController == null) return;
        if (Vector3.Distance(transform.position, target.position) <= attackRange + 1f) playerController.TakeDamage(damage);
    }

    public void ApplyKnockback(Vector3 direction, float force, float stunDuration)
    {
        if (isDead || isEnraged) return;
        knockbackVelocity = direction * force;
        stunTimer = stunDuration;
        isPreparingAttack = false;
        ResetColor();
        if (animator != null) animator.SetTrigger("Hit");
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damageAmount;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Enemy_Hurt);
        StartCoroutine(HitFlashRoutine());

        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
            popup.GetComponent<DamagePopup>()?.Setup(damageAmount);
        }

        if (currentHealth <= 0) Die();
    }

    public void MakeInvincibleAndFurious()
    {
        isInvincible = true;
        isEnraged = true;
        actualMoveSpeed = moveSpeed * 1.8f;

        for (int i = 0; i < originalColors.Length; i++)
        {
            originalColors[i] = new Color(0.2f, 0f, 0f);
        }
        ResetColor();
    }

    private IEnumerator HitFlashRoutine()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.1f);
        if (!isPreparingAttack) ResetColor();
    }

    private void SetColor(Color c)
    {
        if (meshRenderers == null) return;
        foreach (var r in meshRenderers) if (r != null && r.material != null) r.material.color = c;
    }

    private void ResetColor()
    {
        if (meshRenderers == null || originalColors == null) return;
        for (int i = 0; i < meshRenderers.Length; i++)
            if (meshRenderers[i] != null && meshRenderers[i].material != null) meshRenderers[i].material.color = originalColors[i];
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Enemy_Die);

        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        ResetColor();

        if (xpCrystalPrefab != null) Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        if (diamondPrefab != null && Random.value <= diamondDropChance) Instantiate(diamondPrefab, transform.position, Quaternion.identity);

        if (MissionManager.Instance != null) MissionManager.Instance.AddProgress(MissionType.KillEnemies, 1);
        if (Level1_QuestManager.Instance != null) Level1_QuestManager.Instance.EnemyDefeated();

        Destroy(gameObject, 2f);
    }
}