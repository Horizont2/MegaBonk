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

    [Header("Visual Effects")]
    private CameraFollow cameraFollow;
    private BloodFlashEffect bloodEffect;

    private CharacterController characterController;
    private Vector3 velocity;
    private Vector3 moveInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;

        // Auto-find updated components
        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }
        bloodEffect = FindObjectOfType<BloodFlashEffect>();
    }

    private void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Отримуємо напрямок, куди зараз дивиться камера
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            
            // Ігноруємо нахил камери по висоті, щоб гравець не намагався втиснутися в землю
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Рухаємо гравця ВІДНОСНО камери
            moveInput = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            characterController.Move(moveInput * moveSpeed * Time.deltaTime);
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJumpAndGravity()
    {
        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f; 
        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded) velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log("Player health: " + currentHealth);

        // Call effects
        if (cameraFollow != null) cameraFollow.StartShake();
        if (bloodEffect != null) bloodEffect.Flash();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("GAME OVER.");
        gameObject.SetActive(false); 
    }
}