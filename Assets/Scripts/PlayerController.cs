using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug")]
    public float noclipSpeed = 30f;
    private bool isNoclip = false;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 15f;

    [Header("Jump Settings")]
    public bool canJump = true;
    public float jumpHeight = 2f;

    [Header("Gravity Settings")]
    public float gravity = -25f;

    [Header("Player Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float healthRegenRate = 0f;
    public float pickupRadius = 4f;

    [Header("RPG Stats")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 50f;
    public int crystalsCollected = 0;

    [Header("Visual Effects")]
    public Image damageFlashImage;

    [Header("HUD UI References")]
    public Slider hpSlider;
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI crystalText;
    public TextMeshProUGUI hpText;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.5f;
    private bool isDashing = false;
    private float lastDashTime = -100f;

    [Header("Meta Upgrades")]
    [HideInInspector] public float globalDamageMultiplier = 1f;
    private float damageReduction = 0f;

    private CameraFollow cameraFollow;
    private BloodFlashEffect bloodEffect;
    private CharacterController characterController;
    private Vector3 velocity;

    private void Awake()
    {
        gameObject.layer = 8;
        Physics.IgnoreLayerCollision(8, 9, true);

        characterController = GetComponent<CharacterController>();

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();

        // Оновлено для нових версій Unity (вирішує жовті попередження)
        bloodEffect = FindFirstObjectByType<BloodFlashEffect>();
    }

    private void Start()
    {
        ApplyMetaUpgrades();
        currentHealth = maxHealth;
        UpdateHUD();

        UIIconGlimmer glimmer = FindFirstObjectByType<UIIconGlimmer>();
        if (glimmer != null) glimmer.StartEffect();

        StartCoroutine(SpawnSafely());
    }

    private System.Collections.IEnumerator SpawnSafely()
    {
        if (characterController != null) characterController.enabled = false;

        yield return null;
        yield return null;

        if (PlayerPrefs.GetInt("IsContinuing", 0) == 1)
        {
            float savedX = PlayerPrefs.GetFloat("PlayerPosX", transform.position.x);
            float savedY = PlayerPrefs.GetFloat("PlayerPosY", transform.position.y);
            float savedZ = PlayerPrefs.GetFloat("PlayerPosZ", transform.position.z);
            transform.position = new Vector3(savedX, savedY, savedZ);
        }
        else
        {
            float spawnX = 0f;
            float spawnZ = 0f;
            float spawnY = 20f;

            Vector3 skyPos = new Vector3(spawnX, 1000f, spawnZ);

            if (Physics.Raycast(skyPos, Vector3.down, out RaycastHit hit, 2000f))
            {
                spawnY = hit.point.y + 2f;
            }
            else if (Terrain.activeTerrain != null)
            {
                spawnY = Terrain.activeTerrain.SampleHeight(new Vector3(spawnX, 0, spawnZ)) + Terrain.activeTerrain.transform.position.y + 2f;
            }

            transform.position = new Vector3(spawnX, spawnY, spawnZ);
        }

        if (characterController != null) characterController.enabled = true;
    }

    private void ApplyMetaUpgrades()
    {
        int healthLvl = SaveManager.GetUpgradeLevel("MetaHealth");
        maxHealth += maxHealth * (healthLvl * 0.1f);
        int speedLvl = SaveManager.GetUpgradeLevel("MetaSpeed");
        moveSpeed += moveSpeed * (speedLvl * 0.05f);
        int magnetLvl = SaveManager.GetUpgradeLevel("MetaMagnet");
        pickupRadius += pickupRadius * (magnetLvl * 0.2f);
        int armorLvl = SaveManager.GetUpgradeLevel("MetaArmor");
        damageReduction = armorLvl * 0.05f;
        int dmgLvl = SaveManager.GetUpgradeLevel("MetaDamage");
        globalDamageMultiplier = 1f + (dmgLvl * 0.1f);
    }

    private void Update()
    {
        // --- NOCLIP ТАГЛ ---
        if (Input.GetKeyDown(KeyCode.F10))
        {
            isNoclip = !isNoclip;
            if (characterController != null) characterController.enabled = !isNoclip;
            Debug.Log("Noclip: " + isNoclip);
        }

        if (isNoclip)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float up = 0f;

            if (Input.GetKey(KeyCode.Space)) up = 1f;
            if (Input.GetKey(KeyCode.LeftControl)) up = -1f; // Спуск на Ctrl

            // Перейменовано змінні, щоб уникнути помилки CS0136
            Vector3 ncForward = Camera.main.transform.forward;
            Vector3 ncRight = Camera.main.transform.right;

            Vector3 dir = (ncForward * v + ncRight * h + Vector3.up * up).normalized;
            transform.position += dir * noclipSpeed * Time.deltaTime;

            return; // Зупиняємо виконання решти коду
        }

        Vector3 movement = Vector3.zero;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // --- DASH MECHANIC ---
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine(inputDir));
        }

        if (isDashing) return;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (inputDir.magnitude >= 0.1f)
        {
            movement = (camForward * inputDir.z + camRight * inputDir.x).normalized * moveSpeed;
        }

        float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.05f);

        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * safeDeltaTime;

        Vector3 finalMove = movement + velocity;
        characterController.Move(finalMove * safeDeltaTime);

        if (currentHealth < maxHealth && healthRegenRate > 0)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHUD();
        }

        if (transform.position.y < -20f)
        {
            if (characterController != null) characterController.enabled = false;

            float safeY = 100f;
            if (Terrain.activeTerrain != null)
            {
                safeY = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y + 20f;
            }

            transform.position = new Vector3(transform.position.x, safeY, transform.position.z);
            velocity = Vector3.zero;

            if (characterController != null) characterController.enabled = true;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        float finalDamage = damageAmount * (1f - damageReduction);

        currentHealth -= finalDamage;
        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();

        if (damageFlashImage != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        UpdateHUD();
        if (currentHealth <= 0) Die();
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        float t = 0.4f;
        Color c = damageFlashImage.color;

        c.a = 0.5f;
        damageFlashImage.color = c;

        while (c.a > 0)
        {
            c.a -= Time.deltaTime / t;
            damageFlashImage.color = c;
            yield return null;
        }
    }

    private void Die()
    {
        SaveManager.AddCrystals(crystalsCollected);
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.TriggerGameOver();
        WeaponOrbit weapon = FindFirstObjectByType<WeaponOrbit>();
        if (weapon != null) weapon.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void GainXP(float amount)
    {
        currentXP += amount;
        crystalsCollected++;
        if (currentXP >= xpToNextLevel) LevelUp();
        UpdateHUD();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;
        xpToNextLevel *= 1.5f;
        LevelUpManager lum = FindFirstObjectByType<LevelUpManager>();
        if (lum != null) lum.ShowMenu();
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (hpSlider != null) { hpSlider.maxValue = maxHealth; hpSlider.value = currentHealth; }
        if (hpText != null) hpText.text = Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
        if (xpSlider != null) { xpSlider.maxValue = xpToNextLevel; xpSlider.value = currentXP; }
        if (levelText != null) levelText.text = "LVL: " + currentLevel;
        if (crystalText != null) crystalText.text = crystalsCollected.ToString();
    }

    private System.Collections.IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;
        lastDashTime = Time.time;
        float startTime = Time.time;

        float originalFOV = Camera.main.fieldOfView;
        float targetFOV = originalFOV + 12f;

        if (direction == Vector3.zero) direction = transform.forward;
        else
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f; camRight.y = 0f;
            direction = (camForward * direction.z + camRight * direction.x).normalized;
        }

        while (Time.time < startTime + dashDuration)
        {
            float normalizedTime = (Time.time - startTime) / dashDuration;
            float curve = Mathf.Sin(normalizedTime * Mathf.PI);

            characterController.Move(direction * dashSpeed * curve * Time.deltaTime);
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, normalizedTime);

            yield return null;
        }

        isDashing = false;

        float elapsed = 0f;
        float returnTime = 0.3f;
        while (elapsed < returnTime)
        {
            elapsed += Time.deltaTime;
            Camera.main.fieldOfView = Mathf.Lerp(targetFOV, originalFOV, elapsed / returnTime);
            yield return null;
        }

        Camera.main.fieldOfView = originalFOV;
    }
    // Функція лікування гравця
    // Функція лікування гравця
    public void Heal(float amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // ОНОВЛЮЄМО ІНТЕРФЕЙС! Без цього рядка смужка здоров'я не буде рухатись
        UpdateHUD();
    }
}