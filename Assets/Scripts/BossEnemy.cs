using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Boss component. Add alongside EnemyAI on the boss prefab.
/// Creates its own world-space health bar and handles boss-specific behavior:
/// stomp AoE, charge attack, larger scale, more XP crystals on death.
/// </summary>
public class BossEnemy : MonoBehaviour
{
    [Header("Boss Settings")]
    public float bossScaleMultiplier = 2.5f;
    public int crystalDropCount = 8;
    public Color bossColor = new Color(0.6f, 0.1f, 0.1f);

    [Header("Stomp Attack")]
    public float stompRange = 4f;
    public float stompDamage = 25f;
    public float stompCooldown = 4f;
    public float stompWindupTime = 0.5f;

    [Header("Charge Attack")]
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;
    public float chargeCooldown = 6f;
    public float chargeDamage = 20f;

    [Header("Health Bar")]
    public float healthBarWidth = 2.5f;
    public float healthBarHeight = 0.25f;
    public float healthBarYOffset = 2.8f;

    private EnemyAI enemyAI;
    private Transform playerTransform;
    private float stompTimer;
    private float chargeTimer;
    private bool isCharging = false;
    private bool isStomping = false;
    private Vector3 chargeDirection;
    private float originalMoveSpeed;

    // Health bar
    private GameObject healthBarObj;
    private Image healthBarFill;
    private Image healthBarBg;
    private Canvas healthBarCanvas;

    private void OnEnable()
    {
        enemyAI = GetComponent<EnemyAI>();
        stompTimer = stompCooldown * 0.5f; // First stomp comes sooner
        chargeTimer = chargeCooldown * 0.3f;
        isCharging = false;
        isStomping = false;

        StartCoroutine(InitBoss());
    }

    private IEnumerator InitBoss()
    {
        yield return null; // Wait one frame for EnemyAI.Init()

        // Scale up
        transform.localScale = Vector3.one * bossScaleMultiplier;

        // Apply boss color
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.material.color = bossColor;

        // Cache player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (enemyAI != null)
            originalMoveSpeed = enemyAI.moveSpeed;

        CreateHealthBar();

        // Boss announcement shake
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.StartShake();

        // Play boss spawn sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("bossSpawn");
    }

    private void Update()
    {
        if (enemyAI == null || playerTransform == null) return;

        UpdateHealthBar();

        // Don't run boss attacks during death/spawn animations
        if (!gameObject.activeInHierarchy) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Stomp attack (close range AoE)
        stompTimer += Time.deltaTime;
        if (!isCharging && !isStomping && dist <= stompRange && stompTimer >= stompCooldown)
        {
            StartCoroutine(StompAttack());
        }

        // Charge attack (long range dash)
        chargeTimer += Time.deltaTime;
        if (!isCharging && !isStomping && dist > stompRange && dist < 20f && chargeTimer >= chargeCooldown)
        {
            StartCoroutine(ChargeAttack());
        }

        // During charge, move fast in charge direction
        if (isCharging)
        {
            Vector3 nextPos = transform.position + chargeDirection * chargeSpeed * Time.deltaTime;
            if (Terrain.activeTerrain != null)
            {
                float terrainH = Terrain.activeTerrain.SampleHeight(nextPos) + Terrain.activeTerrain.transform.position.y;
                nextPos.y = terrainH + enemyAI.verticalOffset;
            }
            transform.position = nextPos;
        }
    }

    private IEnumerator StompAttack()
    {
        isStomping = true;
        stompTimer = 0f;

        // Windup: boss jumps up
        float originalSpeed = enemyAI.moveSpeed;
        enemyAI.moveSpeed = 0f;

        Vector3 startPos = transform.position;
        float t = 0f;
        while (t < stompWindupTime)
        {
            t += Time.deltaTime;
            float jumpHeight = Mathf.Sin((t / stompWindupTime) * Mathf.PI) * 3f;
            transform.position = startPos + Vector3.up * jumpHeight;
            yield return null;
        }

        // STOMP: damage all nearby + visual shake
        transform.position = startPos;
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.StartShake();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("bossAttack");

        // Check if player is in stomp range
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= stompRange)
            {
                PlayerController pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(stompDamage);
            }
        }

        enemyAI.moveSpeed = originalSpeed;
        isStomping = false;
    }

    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        chargeTimer = 0f;

        // Brief pause before charging (telegraph)
        float origSpeed = enemyAI.moveSpeed;
        enemyAI.moveSpeed = 0f;

        // Flash red to telegraph
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        Color origColor = bossColor;
        if (mr != null) mr.material.color = Color.red;

        yield return new WaitForSeconds(0.4f);

        if (mr != null) mr.material.color = origColor;

        // Calculate charge direction towards player
        if (playerTransform != null)
            chargeDirection = (playerTransform.position - transform.position).normalized;
        chargeDirection.y = 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("bossAttack");

        // Charge for duration
        float chargeTime = 0f;
        while (chargeTime < chargeDuration)
        {
            chargeTime += Time.deltaTime;

            // Check if we hit the player during charge
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= 2.5f)
                {
                    PlayerController pc = playerTransform.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(chargeDamage);
                    break; // Stop charging on hit
                }
            }

            yield return null;
        }

        enemyAI.moveSpeed = origSpeed;
        isCharging = false;
    }

    /// <summary>
    /// Called when the boss dies (override in EnemyAI.Die via integration).
    /// Drops multiple crystals and tracks boss kill.
    /// </summary>
    public void OnBossDeath()
    {
        GameStats.bossKills++;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("bossDeath");

        // Drop extra crystals in a circle
        if (enemyAI != null && enemyAI.xpCrystalPrefab != null)
        {
            for (int i = 0; i < crystalDropCount - 1; i++) // -1 because EnemyAI already drops one
            {
                float angle = (360f / crystalDropCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.5f;
                Vector3 dropPos = transform.position + offset;

                if (ObjectPool.Instance != null)
                    ObjectPool.Instance.Get(enemyAI.xpCrystalPrefab, dropPos, Quaternion.identity);
                else
                    Instantiate(enemyAI.xpCrystalPrefab, dropPos, Quaternion.identity);
            }
        }

        // Destroy health bar
        if (healthBarObj != null) Destroy(healthBarObj);
    }

    // ─── HEALTH BAR ───

    private void CreateHealthBar()
    {
        healthBarObj = new GameObject("BossHealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = Vector3.up * healthBarYOffset;

        healthBarCanvas = healthBarObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 10;

        RectTransform canvasRect = healthBarObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        canvasRect.localScale = Vector3.one * 0.02f;

        // Background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(healthBarObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        healthBarBg = bgObj.AddComponent<Image>();
        healthBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0, 0.5f);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = new Color(0.9f, 0.15f, 0.15f, 1f);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || enemyAI == null) return;

        // Billboard: always face camera
        if (Camera.main != null && healthBarObj != null)
        {
            healthBarObj.transform.rotation = Quaternion.LookRotation(
                healthBarObj.transform.position - Camera.main.transform.position
            );
        }

        // Update fill bar
        float healthPercent = Mathf.Clamp01(enemyAI.currentHealth / enemyAI.maxHealth);
        healthBarFill.rectTransform.anchorMax = new Vector2(healthPercent, 1f);

        // Color transitions: green > yellow > red
        if (healthPercent > 0.5f)
            healthBarFill.color = Color.Lerp(Color.yellow, new Color(0.9f, 0.15f, 0.15f), (1f - healthPercent) * 2f);
        else
            healthBarFill.color = Color.Lerp(new Color(0.5f, 0f, 0f), Color.yellow, healthPercent * 2f);
    }

    private void OnDisable()
    {
        if (healthBarObj != null) Destroy(healthBarObj);
        isCharging = false;
        isStomping = false;
    }
}
