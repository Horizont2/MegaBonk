using UnityEngine;
using TMPro;

public class NeonFlicker : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private Material textMaterial;

    [Header("Налаштування мерехтіння")]
    public float baseGlowPower = 0.4f; // Твоє стандартне значення Outer Glow
    public float flickerAmount = 0.15f; // Наскільки сильно падає яскравість
    public float speed = 10f;          // Швидкість "шуму"

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        // Створюємо копію матеріалу, щоб не міняти всі тексти одночасно
        textMaterial = textComponent.fontSharedMaterial;
    }

    void Update()
    {
        // Випадкові "стрибки" напруги
        if (Random.value > 0.9f) // 10% шанс на різке мерехтіння в кожному кадрі
        {
            float flicker = Random.Range(baseGlowPower - flickerAmount, baseGlowPower);
            textComponent.fontMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, flicker);
        }
        else
        {
            // Повертаємо до базового значення
            float smoothGlow = Mathf.Lerp(textComponent.fontMaterial.GetFloat(ShaderUtilities.ID_GlowOuter), baseGlowPower, Time.deltaTime * speed);
            textComponent.fontMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, smoothGlow);
        }
    }
}