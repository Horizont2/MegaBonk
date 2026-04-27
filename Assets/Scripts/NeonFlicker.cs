using UnityEngine;
using TMPro;

public class NeonFlicker : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private float currentGlow;

    [Header("Налаштування мерехтіння")]
    public float baseGlowPower = 0.4f;
    public float flickerAmount = 0.15f;
    public float speed = 10f;

    void Start()
    {
        // Шукаємо текст на цьому об'єкті або всередині нього
        textComponent = GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null)
        {
            currentGlow = baseGlowPower;
        }
    }

    void Update()
    {
        // Якщо тексту немає - просто нічого не робимо (захист від помилок)
        if (textComponent == null) return;

        if (Random.value > 0.9f)
        {
            currentGlow = Random.Range(baseGlowPower - flickerAmount, baseGlowPower);
        }
        else
        {
            currentGlow = Mathf.Lerp(currentGlow, baseGlowPower, Time.deltaTime * speed);
        }

        // Безпечно застосовуємо зміну
        textComponent.fontSharedMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, currentGlow);
    }
}