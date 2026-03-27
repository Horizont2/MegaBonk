using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("How much the camera shakes. Recommended: 0.1 to 0.5.")]
    public float shakeIntensity = 0.2f;
    
    [Tooltip("How long the shake lasts in seconds.")]
    public float shakeDuration = 0.2f;

    // Internal variables
    private float currentShakeTimer;
    private Vector3 originalLocalPosition;

    private void Start()
    {
        // Remember where the camera is relative to its parent (the player)
        originalLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (currentShakeTimer > 0)
        {
            // Shake! Generate a random point in a sphere around original position
            transform.localPosition = originalLocalPosition + Random.insideUnitSphere * shakeIntensity;
            
            currentShakeTimer -= Time.deltaTime;
        }
        else if (transform.localPosition != originalLocalPosition)
        {
            // Stop shaking and snap back
            transform.localPosition = originalLocalPosition;
        }
    }

    public void StartShake()
    {
        currentShakeTimer = shakeDuration;
    }
}