using UnityEngine;

public class WeaponOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("The center point of the orbit. (Usually the Player)")]
    public Transform pivot; 

    [Tooltip("How many degrees per second the weapon rotates when moving.")]
    public float rotationSpeed = 360f;

    [Tooltip("The height of the orbital plane above the pivot's center.")]
    public float orbitHeight = 1f;

    // Internal variables
    private float currentAngle;
    private float orbitDistance;

    private void Start()
    {
        // 1. If we forgot to assign the pivot in Inspector, try to find the player
        if (pivot == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) pivot = playerObj.transform;
        }

        if (pivot != null)
        {
            // 2. Detach this object from the parent (the Player) immediately.
            // This is the key! We are now a free object in world space.
            transform.parent = null;

            // 3. Calculate how far this object was from the pivot at the start.
            // This sets the permanent orbit radius.
            orbitDistance = Vector3.Distance(transform.position, pivot.position);
        }
    }

    private void LateUpdate()
    {
        if (pivot == null) return;

        // 4. Update the angle ONLY if WASD or Arrows are pressed
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            // Update the angle of the orbit
            currentAngle += rotationSpeed * Time.deltaTime;
            
            // Keep the angle between 0 and 360 (for clean math)
            if (currentAngle > 360f) currentAngle -= 360f;
        }

        // 5. Calculate the new position based on the center (pivot), distance, and angle
        // Pure trigonometry (X = sin, Z = cos) makes a perfect circle
        float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * orbitDistance;
        float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * orbitDistance;

        // 6. Set the final position and rotation of the hammer
        // It stays on a fixed world-space circle, ignoring player rotation
        Vector3 desiredPosition = pivot.position + new Vector3(x, orbitHeight, z);
        transform.position = desiredPosition;

        // Optionally: make the hammer look where it's going along the circle
        Vector3 orbitDirection = (desiredPosition - pivot.position).normalized;
        if (orbitDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(orbitDirection);
        }
    }
}