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

    private void Awake()
    {
        gameObject.layer = 9;
        int minimapLayer = LayerMask.NameToLayer("MinimapOnly");

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer != minimapLayer) t.gameObject.layer = 9;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

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

        Vector3 currentPos = transform.position;
        Vector3 directionToPlayer = (target.position - currentPos).normalized;

        Vector3 repulsion = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, repulsionRadius, 1 << 9);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject && !neighbor.isTrigger)
            {
                Vector3 pushDir = transform.position - neighbor.transform.position;
                float distance = pushDir.magnitude;
                if (distance < repulsionRadius && distance > 0)
                {
                    repulsion += pushDir.normalized * (repulsionRadius - distance);
                }
            }
        }

        Vector3 finalDirection = (directionToPlayer + repulsion * repulsionForce).normalized;
        finalDirection.y = 0f;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer <= attackRange)
        {
            if (animator != null) animator.SetBool("isMoving", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // Запускаємо тільки анімацію та оновлюємо кулдаун
                lastAttackTime = Time.time;
                if (animator != null) animator.SetTrigger("Attack");
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

    // Цей метод ми викличемо через Animation Event у момент удару!
    public void ExecuteAttackDamage()
    {
        if (isDead || target == null || playerController == null) return;

        // Перевіряємо, чи гравець все ще стоїть поруч, коли зброя опускається
        // (даємо трохи запасу +0.5f до рейнджу, щоб ворог не "мазав", якщо гравець зробив крок назад)
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);
        if (distanceToPlayer <= attackRange + 0.5f)
        {
            playerController.TakeDamage(damage);
        }
    }
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position, Quaternion.identity);
            DamagePopup popupScript = popup.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damageAmount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // --- ДОДАНО ТРИГЕР АНІМАЦІЇ ОТРИМАННЯ ШКОДИ ---
            if (animator != null) animator.SetTrigger("Hit");
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = false;

        if (xpCrystalPrefab != null) Instantiate(xpCrystalPrefab, transform.position, Quaternion.identity);
        if (diamondPrefab != null && Random.value <= diamondDropChance) Instantiate(diamondPrefab, transform.position, Quaternion.identity);

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.AddProgress(MissionType.KillEnemies, 1);
        }

        Destroy(gameObject, 2f);
    }
}