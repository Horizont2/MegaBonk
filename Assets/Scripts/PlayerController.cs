using UnityEngine;

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

    [Header("RPG Stats")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 50f;

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

    private void Update()
    {
        // 1. Рахуємо вектор руху по землі
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

        // 2. Рахуємо гравітацію та стрибок
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        // 3. ОБ'ЄДНУЄМО І РУХАЄМОСЯ ОДИН РАЗ
        Vector3 finalMove = movement + velocity;
        characterController.Move(finalMove * Time.deltaTime);
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("GAME OVER.");

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.TriggerGameOver();

        // Знаходимо нашу орбітальну зброю і ховаємо її разом із нами
        WeaponOrbit weapon = FindObjectOfType<WeaponOrbit>();
        if (weapon != null) weapon.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void GainXP(float amount)
    {
        currentXP += amount;
        Debug.Log($"Collected XP! Current: {currentXP} / {xpToNextLevel}");

        if (currentXP >= xpToNextLevel) LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;
        xpToNextLevel *= 1.5f;
        Debug.Log($"LEVEL UP! You are now level {currentLevel}!");

        // Знаходимо менеджер і викликаємо меню
        LevelUpManager lum = FindObjectOfType<LevelUpManager>();
        if (lum != null) lum.ShowMenu();
    }
}