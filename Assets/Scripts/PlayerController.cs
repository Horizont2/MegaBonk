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

    private CameraFollow cameraFollow;
    private BloodFlashEffect bloodEffect;
    private CharacterController characterController;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;

        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        bloodEffect = FindObjectOfType<BloodFlashEffect>();
    }

    private void Start()
    {
        UpdateHUD(); // Your existing code

        // Find the diamond glimmer script and start it
        UIIconGlimmer glimmer = FindObjectOfType<UIIconGlimmer>();
        if (glimmer != null) glimmer.StartEffect();
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

        // 2. Регенерація здоров'я
        if (currentHealth < maxHealth && healthRegenRate > 0)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHUD();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();

        UpdateHUD();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
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