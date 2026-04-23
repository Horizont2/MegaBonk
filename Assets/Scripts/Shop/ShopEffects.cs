using UnityEngine;
using System.Collections;

/// <summary>
/// Juicy effects for the shop: purchase particle burst, camera shake, error feedback.
/// Assign references in Inspector.
/// </summary>
public class ShopEffects : MonoBehaviour
{
    [Header("Purchase Effects")]
    [Tooltip("Particle system prefab for purchase celebration (gold sparks, confetti)")]
    public GameObject purchaseParticlePrefab;
    [Tooltip("Camera shake on purchase")]
    public float purchaseShakeDuration = 0.3f;
    public float purchaseShakeMagnitude = 0.15f;

    [Header("Error Effects")]
    public float errorShakeDuration = 0.2f;
    public float errorShakeMagnitude = 0.05f;

    [Header("Camera Reference")]
    public Transform shopCamera;

    private Vector3 cameraOriginalPos;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        if (shopCamera != null)
            cameraOriginalPos = shopCamera.localPosition;
    }

    public void PlayPurchaseEffect(Vector3 position)
    {
        // Spawn particles
        if (purchaseParticlePrefab != null)
        {
            GameObject particles = Instantiate(purchaseParticlePrefab, position, Quaternion.identity);
            Destroy(particles, 4f);
        }

        // Camera shake
        if (shopCamera != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShake(purchaseShakeDuration, purchaseShakeMagnitude));
        }
    }

    public void PlayErrorEffect()
    {
        if (shopCamera != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShake(errorShakeDuration, errorShakeMagnitude));
        }
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        // Re-capture the current target position (since camera lerps)
        Vector3 basePos = shopCamera.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / duration);

            float x = Random.Range(-1f, 1f) * magnitude * decay;
            float y = Random.Range(-1f, 1f) * magnitude * decay;

            shopCamera.localPosition = basePos + new Vector3(x, y, 0f);
            yield return null;
        }

        shopCamera.localPosition = basePos;
    }
}
