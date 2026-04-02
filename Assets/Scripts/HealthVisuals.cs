using UnityEngine;
using UnityEngine.UI;

public class HealthVisuals : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Image bloodVignette;

    [Header("Settings")]
    public float dangerThreshold = 0.3f; // 30% health
    public float pulseSpeed = 4f;
    public float maxAlpha = 0.6f;

    private void Update()
    {
        if (player == null || bloodVignette == null) return;

        // Calculate health percentage
        float healthPercent = player.currentHealth / player.maxHealth;

        if (healthPercent <= dangerThreshold)
        {
            // Calculate how deep we are in the "danger zone"
            // 1.0 = exactly at threshold, 0.0 = almost dead
            float intensity = 1f - (healthPercent / dangerThreshold);

            // Create a pulsing effect using Sine wave
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            // Combine intensity and pulse for the final transparency
            float finalAlpha = intensity * maxAlpha * pulse;

            SetVignetteAlpha(finalAlpha);
        }
        else
        {
            // If health is fine, quickly fade out the red effect
            SetVignetteAlpha(Mathf.MoveTowards(bloodVignette.color.a, 0f, Time.deltaTime));
        }
    }

    private void SetVignetteAlpha(float alpha)
    {
        Color c = bloodVignette.color;
        c.a = alpha;
        bloodVignette.color = c;
    }
}