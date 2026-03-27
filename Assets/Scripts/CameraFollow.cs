using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The player or object the camera should follow.")]
    public Transform target;

    [Tooltip("The positional offset from the target. (Y is height, Z is distance backwards)")]
    public Vector3 offset = new Vector3(0f, 10f, -10f);

    [Tooltip("The starting rotation of the camera (X is tilt down).")]
    public Vector3 startRotation = new Vector3(45f, 0f, 0f);

    [Header("Movement Settings")]
    [Tooltip("How smoothly the camera catches up to the target. Higher is faster.")]
    public float smoothSpeed = 5f;

    private void Start()
    {
        // Detach camera from any parent to prevent weird movement bugs
        transform.parent = null;

        // Snap the camera to the correct position and rotation immediately when the game starts
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(startRotation);
        }
        else
        {
            Debug.LogWarning("CameraFollow: Target is not assigned in the Inspector!");
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate where the camera should be in world space
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move the camera to that position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}