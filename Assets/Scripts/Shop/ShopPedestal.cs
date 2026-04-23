using UnityEngine;
using System.Collections;

/// <summary>
/// A single pedestal in the shop scene. Holds a display model and a spotlight.
/// ShopManager controls which pedestal is active.
/// </summary>
public class ShopPedestal : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ShopItemData this pedestal displays")]
    public ShopItemData itemData;
    [Tooltip("The spotlight above this pedestal")]
    public Light spotlight;
    [Tooltip("Transform where the display model spawns (center of pedestal top)")]
    public Transform modelSpawnPoint;
    [Tooltip("Optional: particle system under the pedestal (rune glow, etc.)")]
    public ParticleSystem pedestalParticles;

    [Header("Spotlight Settings")]
    public float normalIntensity = 1f;
    public float focusedIntensity = 3f;
    public Color normalColor = new Color(0.6f, 0.7f, 1f);
    public Color focusedColor = new Color(0.8f, 0.9f, 1f);
    public float lightTransitionSpeed = 4f;

    [Header("Model Animation")]
    public float idleRotationSpeed = 20f;
    public float focusBobAmplitude = 0.05f;
    public float focusBobFrequency = 1.5f;

    private GameObject spawnedModel;
    private Animator modelAnimator;
    private bool isFocused = false;
    private float bobTimer = 0f;
    private Vector3 modelBasePos;
    private float currentIntensity;
    private Color currentColor;

    private void Start()
    {
        SpawnDisplayModel();

        if (spotlight != null)
        {
            currentIntensity = normalIntensity;
            currentColor = normalColor;
            spotlight.intensity = normalIntensity;
            spotlight.color = normalColor;
        }

        if (pedestalParticles != null)
            pedestalParticles.Stop();
    }

    private void SpawnDisplayModel()
    {
        if (itemData == null || itemData.displayPrefab == null) return;

        Transform parent = modelSpawnPoint != null ? modelSpawnPoint : transform;
        spawnedModel = Instantiate(itemData.displayPrefab, parent.position, parent.rotation, parent);

        modelAnimator = spawnedModel.GetComponentInChildren<Animator>();
        modelBasePos = spawnedModel.transform.localPosition;
    }

    private void Update()
    {
        // Smooth spotlight transition
        if (spotlight != null)
        {
            float targetIntensity = isFocused ? focusedIntensity : normalIntensity;
            Color targetColor = isFocused ? focusedColor : normalColor;

            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, lightTransitionSpeed * Time.deltaTime);
            currentColor = Color.Lerp(currentColor, targetColor, lightTransitionSpeed * Time.deltaTime);

            spotlight.intensity = currentIntensity;
            spotlight.color = currentColor;
        }

        // Model idle rotation + focus bob
        if (spawnedModel != null)
        {
            spawnedModel.transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.World);

            if (isFocused)
            {
                bobTimer += Time.deltaTime * focusBobFrequency;
                float bobOffset = Mathf.Sin(bobTimer * Mathf.PI * 2f) * focusBobAmplitude;
                spawnedModel.transform.localPosition = modelBasePos + Vector3.up * bobOffset;
            }
        }
    }

    public void SetFocused(bool focused)
    {
        isFocused = focused;

        if (focused)
        {
            bobTimer = 0f;

            if (pedestalParticles != null)
                pedestalParticles.Play();

            // Trigger focus animation if available
            if (modelAnimator != null)
                modelAnimator.SetTrigger("Focus");
        }
        else
        {
            if (pedestalParticles != null)
                pedestalParticles.Stop();

            // Reset bob position
            if (spawnedModel != null)
                spawnedModel.transform.localPosition = modelBasePos;
        }
    }

    public void PlayPurchaseAnimation()
    {
        if (modelAnimator != null)
            modelAnimator.SetTrigger("Victory");

        StartCoroutine(PurchaseFlash());
    }

    private IEnumerator PurchaseFlash()
    {
        if (spotlight == null) yield break;

        float originalIntensity = focusedIntensity;
        spotlight.intensity = focusedIntensity * 3f;
        spotlight.color = new Color(1f, 0.9f, 0.5f);

        yield return new WaitForSeconds(0.15f);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            spotlight.intensity = Mathf.Lerp(focusedIntensity * 3f, focusedIntensity, t / 0.5f);
            spotlight.color = Color.Lerp(new Color(1f, 0.9f, 0.5f), focusedColor, t / 0.5f);
            yield return null;
        }
    }

    public Transform GetCameraTarget()
    {
        return modelSpawnPoint != null ? modelSpawnPoint : transform;
    }
}
