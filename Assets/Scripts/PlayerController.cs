using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Character & Weapon Spawning")]
    public GameObject[] heroPrefabs;
    public GameObject[] weaponPrefabs;
    public float visualYOffset = -1f;
    private GameObject currentVisual;
    private GameObject currentWeapon;

    [Header("Debug")]
    public float noclipSpeed = 30f;
    private bool isNoclip = false;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 15f;

    [Header("MegaBoom Inertia")]
    public float normalAcceleration = 15f;
    public float dragAcceleration = 3f;
    private Vector3 currentVelocityMove;

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

    [Header("Melee Combat")]
    public float meleeDamage = 25f;
    public float meleeRadius = 2.5f;
    public Transform meleePoint;

    [Header("Grenade & Trajectory (NEW)")]
    public GameObject grenadePrefab;
    public Transform throwPoint;
    public LineRenderer trajectoryLine;
    public int linePoints = 30;
    public float timeBetweenPoints = 0.1f;
    public float minThrowForce = 5f;
    public float maxThrowForce = 30f;
    public float chargeRate = 15f;
    public float upwardAngle = 0.5f;

    private float currentThrowForce;
    private bool isAimingGrenade = false;
    private Vector3 savedThrowVelocity; // Зберігає вектор сили під час анімації

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

    [Header("MegaBoom Settings")]
    public float stackRadius = 7f;
    public TextMeshProUGUI stackText;
    public float criticalDamagePerSec = 5f;
    [HideInInspector] public int currentStack = 0;
    [HideInInspector] public int currentMultiplier = 1;

    private CameraFollow cameraFollow;
    private BloodFlashEffect bloodEffect;
    private CharacterController characterController;
    private Vector3 velocity;

    private Animator anim;

    private void Awake()
    {
        gameObject.layer = 8;
        Physics.IgnoreLayerCollision(8, 9, true);

        characterController = GetComponent<CharacterController>();

        int selectedHeroID = PlayerPrefs.GetInt("SelectedHeroID", 0);
        int selectedWeaponID = PlayerPrefs.GetInt("SelectedWeaponID", 0);

        if (heroPrefabs != null && heroPrefabs.Length > selectedHeroID && heroPrefabs[selectedHeroID] != null)
        {
            currentVisual = Instantiate(heroPrefabs[selectedHeroID], transform.position, transform.rotation, transform);
            currentVisual.transform.localPosition = new Vector3(0, visualYOffset, 0);

            anim = currentVisual.GetComponent<Animator>();
            if (anim != null) anim.applyRootMotion = false;

            Transform socket = FindDeepChild(currentVisual.transform, "handslot.r");
            if (socket != null && weaponPrefabs != null && weaponPrefabs.Length > selectedWeaponID && weaponPrefabs[selectedWeaponID] != null)
            {
                currentWeapon = Instantiate(weaponPrefabs[selectedWeaponID], socket.position, socket.rotation, socket);
            }
        }
        else
        {
            anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        bloodEffect = FindFirstObjectByType<BloodFlashEffect>();

        // Ховаємо лінію траєкторії на старті
        if (trajectoryLine != null) trajectoryLine.positionCount = 0;
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

    private void CheckStack()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, stackRadius, 1 << 9);
        currentStack = 0;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy")) currentStack++;
        }

        if (currentStack >= 30) currentMultiplier = 5;
        else if (currentStack >= 20) currentMultiplier = 4;
        else if (currentStack >= 15) currentMultiplier = 2;
        else currentMultiplier = 1;

        if (stackText != null)
        {
            stackText.text = "STACK: " + currentStack + "  |  x" + currentMultiplier;
            if (currentStack >= 30) stackText.color = Color.red;
            else if (currentStack >= 15) stackText.color = Color.yellow;
            else stackText.color = Color.white;
        }
    }

    private void Update()
    {
        CheckStack();

        if (currentVisual != null)
        {
            currentVisual.transform.localRotation = Quaternion.identity;
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            isNoclip = !isNoclip;
            if (characterController != null) characterController.enabled = !isNoclip;
        }

        if (isNoclip)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float up = 0f;
            if (Input.GetKey(KeyCode.Space)) up = 1f;
            if (Input.GetKey(KeyCode.LeftControl)) up = -1f;

            Vector3 ncForward = Camera.main.transform.forward;
            Vector3 ncRight = Camera.main.transform.right;

            Vector3 dir = (ncForward * v + ncRight * h + Vector3.up * up).normalized;
            transform.position += dir * noclipSpeed * Time.deltaTime;
            return;
        }

        Vector3 movement = Vector3.zero;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

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

        float currentAccel = normalAcceleration;

        if (currentStack >= 30)
        {
            currentAccel = dragAcceleration;
            currentHealth -= criticalDamagePerSec * Time.deltaTime;
            UpdateHUD();
            if (currentHealth <= 0) Die();
        }
        else if (currentStack >= 15)
        {
            currentAccel = dragAcceleration;
        }

        float actualSpeed = isAimingGrenade ? moveSpeed * 0.5f : moveSpeed;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 targetMove = (camForward * inputDir.z + camRight * inputDir.x).normalized * actualSpeed;
            currentVelocityMove = Vector3.Lerp(currentVelocityMove, targetMove, currentAccel * Time.deltaTime);
        }
        else
        {
            currentVelocityMove = Vector3.Lerp(currentVelocityMove, Vector3.zero, currentAccel * Time.deltaTime);
        }

        movement = currentVelocityMove;

        float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.05f);

        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * safeDeltaTime;

        Vector3 finalMove = movement + velocity;
        characterController.Move(finalMove * safeDeltaTime);

        // --- УПРАВЛІННЯ АНІМАТОРОМ І БОЄМ ---
        if (anim != null)
        {
            anim.SetFloat("Speed", currentVelocityMove.magnitude);
            anim.SetBool("IsGrounded", characterController.isGrounded);

            // МЕЧ (ЛІВА кнопка миші = 0)
            if (Input.GetMouseButtonDown(0))
            {
                if (!isAimingGrenade) // Якщо ми не тримаємо гранату, б'ємо мечем
                {
                    anim.SetTrigger("Attack");
                }
            }

            // ГРАНАТА (ПРАВА кнопка миші = 1)
            if (Input.GetMouseButtonDown(1))
            {
                isAimingGrenade = true;
                currentThrowForce = minThrowForce;
                if (trajectoryLine != null) trajectoryLine.positionCount = 0;
            }

            // Накопичення сили (тримаємо ПРАВУ кнопку)
            if (Input.GetMouseButton(1) && isAimingGrenade)
            {
                currentThrowForce += chargeRate * Time.deltaTime;
                if (currentThrowForce > maxThrowForce) currentThrowForce = maxThrowForce;
                DrawTrajectory();
            }

            if (Input.GetMouseButtonUp(1))
            {
                if (isAimingGrenade)
                {
                    isAimingGrenade = false;
                    savedThrowVelocity = GetThrowVelocity(); // Запам'ятовуємо силу і напрямок ДО анімації
                    if (trajectoryLine != null) trajectoryLine.positionCount = 0; // Ховаємо лінію
                    anim.SetTrigger("Throw"); // Запускаємо анімацію
                }
            }
        }

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

    // --- ЛОГІКА ПРИЦІЛЮВАННЯ ТА ТРАЄКТОРІЇ ---
    // --- ЛОГІКА ПРИЦІЛЮВАННЯ ТА ТРАЄКТОРІЇ ---
    // --- ЛОГІКА ПРИЦІЛЮВАННЯ ТА ТРАЄКТОРІЇ ---
    private Vector3 GetThrowVelocity()
    {
        // Беремо напрямок рівно туди, куди дивиться гравець
        Vector3 aimDir = transform.forward;

        // Додаємо кут вгору, щоб граната летіла гарною дугою
        Vector3 throwDir = (aimDir + Vector3.up * upwardAngle).normalized;

        // Множимо на поточну накопичену силу
        return throwDir * currentThrowForce;
    }

    private void DrawTrajectory()
    {
        if (trajectoryLine == null || throwPoint == null) return;

        trajectoryLine.positionCount = linePoints;
        Vector3 startPosition = throwPoint.position;
        Vector3 startVelocity = GetThrowVelocity();

        for (int i = 0; i < linePoints; i++)
        {
            float t = i * timeBetweenPoints;
            Vector3 point = startPosition + startVelocity * t + Physics.gravity * 0.5f * t * t;

            trajectoryLine.SetPosition(i, point);

            if (point.y < 0f && i > 5)
            {
                trajectoryLine.positionCount = i + 1;
                break;
            }
        }
    }

    // --- ФІЗИЧНА ЛОГІКА ---
    public void ExecuteAttack()
    {
        if (meleePoint == null) return;

        Collider[] hitEnemies = Physics.OverlapSphere(meleePoint.position, meleeRadius, 1 << 9);

        foreach (Collider enemyCol in hitEnemies)
        {
            if (enemyCol.CompareTag("Enemy"))
            {
                Debug.Log("Вдарили ворога: " + enemyCol.name);
            }
        }
    }

    public void ExecuteThrow()
    {
        if (grenadePrefab != null && throwPoint != null)
        {
            GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Застосовуємо силу, яку ми розрахували ДО початку анімації
                rb.linearVelocity = savedThrowVelocity;
            }
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

        if (anim != null) anim.SetTrigger("Hit");

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
        if (currentXP >= xpToNextLevel) LevelUp();
        UpdateHUD();
    }

    public void GainDiamond(int amount = 1)
    {
        crystalsCollected += amount;
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

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHUD();
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRadius);
        }
    }
}