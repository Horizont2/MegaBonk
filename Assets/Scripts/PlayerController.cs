using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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
    public Image damageFlashImage; // Reference to the same BloodVignette

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
        // --- 100% Բ�� Բ����: ��������� ������������ ���� ����� ��� ---
        // ������� ������ �� 8 ���, � ��� ���� �� 9, � �������� �� ���������� ���� ������
        gameObject.layer = 8;
        Physics.IgnoreLayerCollision(8, 9, true);

        characterController = GetComponent<CharacterController>();

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        bloodEffect = FindObjectOfType<BloodFlashEffect>();
    }

    private void Start()
    {
        ApplyMetaUpgrades();
        currentHealth = maxHealth;
        UpdateHUD();

        UIIconGlimmer glimmer = FindObjectOfType<UIIconGlimmer>();
        if (glimmer != null) glimmer.StartEffect();

        // FIX: Start a coroutine to wait for the terrain to generate before dropping the player
        StartCoroutine(SpawnSafely());
    }

    private System.Collections.IEnumerator SpawnSafely()
    {
        // 1. Disable CharacterController so it doesn't block our teleportation
        if (characterController != null) characterController.enabled = false;

        // 2. Wait exactly 2 frames for Unity to fully bake the Terrain physics collider
        yield return null;
        yield return null;

        if (PlayerPrefs.GetInt("IsContinuing", 0) == 1)
        {
            // Load saved coordinates if continuing
            float savedX = PlayerPrefs.GetFloat("PlayerPosX", transform.position.x);
            float savedY = PlayerPrefs.GetFloat("PlayerPosY", transform.position.y);
            float savedZ = PlayerPrefs.GetFloat("PlayerPosZ", transform.position.z);
            transform.position = new Vector3(savedX, savedY, savedZ);
        }
        else
        {
            // New Run: Shoot a raycast from the sky (Y = 1000) straight down to find the true ground
            float spawnX = 0f;
            float spawnZ = 0f;
            float spawnY = 20f; // Fallback height

            Vector3 skyPos = new Vector3(spawnX, 1000f, spawnZ);

            if (Physics.Raycast(skyPos, Vector3.down, out RaycastHit hit, 2000f))
            {
                // Ground found! Add 2 meters so the player drops safely
                spawnY = hit.point.y + 2f;
            }
            else if (Terrain.activeTerrain != null)
            {
                // Fallback method just in case
                spawnY = Terrain.activeTerrain.SampleHeight(new Vector3(spawnX, 0, spawnZ)) + Terrain.activeTerrain.transform.position.y + 2f;
            }

            transform.position = new Vector3(spawnX, spawnY, spawnZ);
        }

        // 3. Re-enable CharacterController after teleporting
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
        Vector3 movement = Vector3.zero;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // --- DASH MECHANIC ---
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine(inputDir));
        }

        // If we are currently dashing, block normal movement
        if (isDashing) return;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f; camRight.y = 0f;
            camForward.Normalize(); camRight.Normalize();

            movement = (camForward * inputDir.z + camRight * inputDir.x).normalized * moveSpeed;

            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- ������ ²� ��ò� (Lag Spike Fix) ---
        // ���� ��� ������� �� ��� ������ ��������, ������ �� ���� � ������
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

        // --- ULTIMATE FAILSAFE: Anti-Void Protection ---
        // If the player somehow falls through the ground (e.g., terrain hasn't baked yet),
        // we catch them at Y = -20 and teleport them safely back into the sky.
        if (transform.position.y < -20f)
        {
            if (characterController != null) characterController.enabled = false;

            // Find a safe height, or just drop them from 100m if terrain is still loading
            float safeY = 100f;
            if (Terrain.activeTerrain != null)
            {
                safeY = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y + 20f;
            }

            transform.position = new Vector3(transform.position.x, safeY, transform.position.z);
            velocity = Vector3.zero; // Reset falling speed so they don't slam into the ground

            if (characterController != null) characterController.enabled = true;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        float finalDamage = damageAmount * (1f - damageReduction);

        currentHealth -= finalDamage;
        GameStats.totalDamageTaken += finalDamage;

        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("playerHurt");

        if (damageFlashImage != null)
        {
            StopAllCoroutines(); // Reset previous flashes
            StartCoroutine(FlashRoutine());
        }

        // ֳ ��� ����� ����� ���� �������Ͳ ������ TakeDamage!
        UpdateHUD();
        if (currentHealth <= 0) Die();
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        // Quickly show red, then fade out
        float t = 0.4f; // Flash duration
        Color c = damageFlashImage.color;

        c.a = 0.5f; // Initial flash transparency
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("playerDeath");
        SaveManager.AddCrystals(crystalsCollected);
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.TriggerGameOver();
        WeaponOrbit weapon = FindObjectOfType<WeaponOrbit>();
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("levelUp");
        LevelUpManager lum = FindObjectOfType<LevelUpManager>();
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

        // If the player isn't pressing any movement keys, dash straight forward
        if (direction == Vector3.zero) direction = transform.forward;
        else
        {
            // Convert input direction to world space camera direction
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f; camRight.y = 0f;
            direction = (camForward * direction.z + camRight * direction.x).normalized;
        }

        // FOV punch + sound on dash start
        if (cameraFollow != null) cameraFollow.PunchFOV();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("dash");

        while (Time.time < startTime + dashDuration)
        {
            characterController.Move(direction * dashSpeed * Time.deltaTime);
            yield return null;
        }

        // Restore FOV
        if (cameraFollow != null) cameraFollow.ReleaseFOV();

        isDashing = false;
    }
}