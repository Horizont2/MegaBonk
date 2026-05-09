using UnityEngine;
using UnityEngine.UI;

public class HealthVisuals : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    [Tooltip("Це має бути темна, м'яка віньєтка по краях екрана, а не просто червоний квадрат")]
    public Image bloodVignette;

    [Header("Low Health Settings (AAA Quality)")]
    public float dangerThreshold = 0.3f; // Вмикається нижче 30%
    public float breathSpeed = 1.5f;     // Повільне, "важке" дихання
    public float maxAlpha = 0.85f;       // Густота ефекту на мінімумі HP

    [Header("Hit Flash Settings")]
    public float hitFlashSpeed = 5f;     // Швидкість зникнення спалаху при ударі
    public Color hitColor = new Color(0.8f, 0f, 0f, 0.4f); // Різкий червоний для удару
    public Color dangerColor = new Color(0.15f, 0f, 0f, 1f); // Дуже темно-бордовий для стану при смерті

    private float currentHitAlpha = 0f;

    private void Awake()
    {
        if (bloodVignette != null)
        {
            Color c = bloodVignette.color;
            c.a = 0f;
            bloodVignette.color = c;
        }
    }

    private void Update()
    {
        if (player == null || bloodVignette == null) return;

        float healthPercent = player.currentHealth / player.maxHealth;
        float targetVignetteAlpha = 0f;
        Color targetColor = hitColor;

        // 1. СТАН: ПРИ СМЕРТІ (Повільна пульсація темної віньєтки)
        if (healthPercent <= dangerThreshold)
        {
            float intensity = 1f - (healthPercent / dangerThreshold);
            float slowPulse = (Mathf.Sin(Time.unscaledTime * breathSpeed) + 1f) / 2f;
            slowPulse = Mathf.Lerp(0.4f, 1f, slowPulse);

            targetVignetteAlpha = intensity * maxAlpha * slowPulse;
            targetColor = Color.Lerp(hitColor, dangerColor, intensity);
        }

        // 2. СТАН: ОТРИМАННЯ УДАРУ (Швидкий спалах)
        if (currentHitAlpha > 0f)
        {
            currentHitAlpha -= hitFlashSpeed * Time.deltaTime;
            currentHitAlpha = Mathf.Max(currentHitAlpha, 0f);
        }

        float finalAlpha = Mathf.Max(targetVignetteAlpha, currentHitAlpha);
        Color finalColor = targetColor;
        finalColor.a = finalAlpha;
        bloodVignette.color = finalColor;
    }

    public void TriggerHitFlash()
    {
        currentHitAlpha = 0.5f;
    }
}