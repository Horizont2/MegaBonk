using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The current movement speed of the player.")]
    public float moveSpeed = 8f;
    
    [Tooltip("How fast the player character model rotates to face the moving direction.")]
    public float rotationSpeed = 15f;

    [Header("Jump Settings")]
    [Tooltip("Can the player jump? Toggle off if you want a strict top-down experience.")]
    public bool canJump = true;

    [Tooltip("How high the player can jump in meters.")]
    public float jumpHeight = 2f;

    [Header("Gravity Settings")]
    [Tooltip("The gravity force. Kept higher than Earth's (-9.81) for snappier, less floaty jumps.")]
    public float gravity = -25f; 

    [Header("Player Stats (Preview)")]
    [Tooltip("Maximum health of the player.")]
    public float maxHealth = 100f;
    
    [Tooltip("Current health of the player.")]
    public float currentHealth;

    // Component references
    private CharacterController characterController;
    
    // Internal variables
    private Vector3 velocity;
    private Vector3 moveInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Initialize health
        currentHealth = maxHealth;
    }

    private void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
    }

    private void HandleMovement()
    {
        // Read input from WASD or Arrows
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Normalize so diagonal movement isn't faster
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveInput.magnitude >= 0.1f)
        {
            // Move the player horizontally
            characterController.Move(moveInput * moveSpeed * Time.deltaTime);

            // Rotate the player model to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJumpAndGravity()
    {
        // Reset downward velocity if grounded to prevent gravity from building up infinitely
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // Check for jump input (Spacebar by default)
        if (canJump && Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            // Physics formula to calculate the exact velocity needed to reach 'jumpHeight'
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity over time
        velocity.y += gravity * Time.deltaTime;
        
        // Apply vertical movement to the controller
        characterController.Move(velocity * Time.deltaTime);
    }

    // --- DAMAGE SYSTEM ---
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log("Player hit! Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("PLAYER IS DEAD! Game Over.");
        // We will add UI and game restart logic here later
        gameObject.SetActive(false); // Temporarily hide the player to simulate death
    }
}