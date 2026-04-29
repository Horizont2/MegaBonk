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
    public float attackCooldown = 1.5f; // «б≥льшено дл€ кращого ритму
    public float attackTelegraphTime = 0.5f; // „ас на "замах" (попередженн€ гравц€)
    private float lastAttackTime;
    private bool isPreparingAttack = false;

    [Header("Drops & Economy")]
    public GameObject xpCrystalPrefab;
    public GameObject diamondPrefab;
    [Range(0f, 1f)]
    public float diamondDropChance = 0.1f;
    public GameObject damagePopupPrefab;

    [Header("Targeting")]
    public Transform target;

    [Header("Ground Settings")]
    public float verticalOffset = 0.0f;

    [Header("Swarm Settings (MegaBoom)")]
    public float repulsionRadius = 1.2f;
    public float repulsionForce = 3f;

    [HideInInspector] public float xpRewardMultiplier = 1f;

    private float currentHealth;
    private MeshRenderer[] meshRenderers;
    private Color[] originalColors;
    private PlayerController playerController;
    private Animator animator;
    private bool isDead = false;

    // --- Ќќ¬≈: Ћог≥ка бою ---
    private Vector3 knockbackVelocity = Vector3.zero;
    private float stunTimer = 0f;

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
    }

    private void Start()
    {
        currentHealth = maxHealth;

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

        // --- ¬≤ƒ »ƒјЌЌя “ј ќ√Ћ”Ў≈ЌЌя ---
        if (knockbackVelocity.magnitude > 0.1f)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 10f); // —ила терт€
        }

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            return; // ¬орог оглушений, не може рухатись чи атакувати
        }

        if (isPreparingAttack) return; // —тоњть на м≥сц≥ п≥д час замаху

        Vector3 currentPos = transform.position;
        Vector3 directionToPlayer = (target.position - currentPos).normalized;

        // Swarm logic (без зм≥н)
        Vector3 repulsion = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, repulsionRadius, 1 << 9);
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject && !neighbor.isTrigger)
            {
                Vector3 pushDir = transform.position - neighbor.transform.position;
                float distance = pushDir.magnitude;
                if (distance < repulsionRadius && distance > 0) repulsion += pushDir.normalized * (repulsionRadius - distance);
            }
        }

        Vector3 finalDirection = (directionToPlayer + repulsion * repulsionForce).normalized;
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

            // --- “≈Ћ≈√–ј‘”¬јЌЌя ј“ј » ---
            if (Time.time >= lastAttackTime + attackCooldown && !isPreparingAttack)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            Vector3 nextPos = currentPos + finalDirection * moveSpeed * Time.deltaTime;
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

        // ¬орог завмираЇ ≥ блимаЇ червоним перед ударом (даЇмо гравцю час зреагувати!)
        SetColor(Color.red);
        yield return new WaitForSeconds(attackTelegraphTime);
        ResetColor();

        // ѕерев≥р€Їмо, чи гравець дос≥ близько (можливо, в≥н ухиливс€ через Dash)
        if (!isDead && stunTimer <= 0 && Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            lastAttackTime = Time.time;
            if (animator != null) animator.SetTrigger("Attack");
        }

        isPreparingAttack = false;
    }

    public void ExecuteAttackDamage()
    {
        if (isDead || target == null || playerController == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);
        if (distanceToPlayer <= attackRange + 1f)
        {
            playerController.TakeDamage(damage);
        }
    }

    // --- Ќќ¬≈: ћетод дл€ прийому в≥дкиданн€ в≥д гравц€ ---
    public void ApplyKnockback(Vector3 direction, float force, float stunDuration)
    {
        if (isDead) return;

        knockbackVelocity = direction * force;
        stunTimer = stunDuration;
        isPreparingAttack = false; // ѕерериваЇмо атаку гравц€!
        ResetColor();

        if (animator != null) animator.SetTrigger("Hit");
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        StartCoroutine(HitFlashRoutine()); // «апускаЇмо всплеск б≥лого кольору

        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up, Quaternion.identity);
            DamagePopup popupScript = popup.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damageAmount);
        }

        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.1f);
        if (!isPreparingAttack) ResetColor(); // якщо в≥н не готуЇтьс€ до атаки, повертаЇмо нормальний кол≥р
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
        {
            if (meshRenderers[i] != null && meshRenderers[i].material != null) meshRenderers[i].material.color = originalColors[i];
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = false;

        ResetColor();

        if (xpCrystalPrefab != null) Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        if (diamondPrefab != null && Random.value <= diamondDropChance) Instantiate(diamondPrefab, transform.position, Quaternion.identity);

        if (MissionManager.Instance != null) MissionManager.Instance.AddProgress(MissionType.KillEnemies, 1);

        Destroy(gameObject, 2f);
    }
}