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

    [Header("HUD UI References")]
    public Slider hpSlider;
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI crystalText;
    public TextMeshProUGUI hpText;

    [Header("Meta Upgrades")]
    [HideInInspector] public float globalDamageMultiplier = 1f; // Weapons will read this
    private float damageReduction = 0f; // Armor percentage

    private CameraFollow cameraFollow;
    private BloodFlashEffect bloodEffect;
    private CharacterController characterController;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        bloodEffect = FindObjectOfType<BloodFlashEffect>();
    }

    private void Start()
    {
        // Apply permanent stats from Main Menu before setting current health
        ApplyMetaUpgrades();

        currentHealth = maxHealth;
        UpdateHUD();

        // Find the diamond glimmer script and start it
        UIIconGlimmer glimmer = FindObjectOfType<UIIconGlimmer>();
        if (glimmer != null) glimmer.StartEffect();
    }

    private void ApplyMetaUpgrades()
    {
        // Health: +10% per level
        int healthLvl = SaveManager.GetUpgradeLevel("MetaHealth");
        maxHealth += maxHealth * (healthLvl * 0.1f);

        // Speed: +5% per level
        int speedLvl = SaveManager.GetUpgradeLevel("MetaSpeed");
        moveSpeed += moveSpeed * (speedLvl * 0.05f);

        // Magnet: +20% radius per level
        int magnetLvl = SaveManager.GetUpgradeLevel("MetaMagnet");
        pickupRadius += pickupRadius * (magnetLvl * 0.2f);

        // Armor: 5% damage reduction per level
        int armorLvl = SaveManager.GetUpgradeLevel("MetaArmor");
        damageReduction = armorLvl * 0.05f;

        // Damage: +10% global damage per level
        int dmgLvl = SaveManager.GetUpgradeLevel("MetaDamage");
        globalDamageMultiplier = 1f + (dmgLvl * 0.1f);
    }

    private void Update()
    {
        Vector3 movement = Vector3.zero;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

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

        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = movement + velocity;
        characterController.Move(finalMove * Time.deltaTime);

        // Health regeneration
        if (currentHealth < maxHealth && healthRegenRate > 0)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHUD();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // Apply Armor reduction
        float finalDamage = damageAmount * (1f - damageReduction);

        currentHealth -= finalDamage;
        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();

        UpdateHUD();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        // SAVE METAPROGRESSION: Add collected crystals to the global bank
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

        LevelUpManager lum = FindObjectOfType<LevelUpManager>();
        if (lum != null) lum.ShowMenu();

        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }

        if (hpText != null)
        {
            hpText.text = Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
        }

        if (xpSlider != null)
        {
            xpSlider.maxValue = xpToNextLevel;
            xpSlider.value = currentXP;
        }

        if (levelText != null) levelText.text = "LVL: " + currentLevel;
        if (crystalText != null) crystalText.text = crystalsCollected.ToString();
    }
}