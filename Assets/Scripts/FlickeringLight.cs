using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    private Light lightSource;
    private float baseIntensity;

    [Header("Налаштування мигання")]
    [Range(0.1f, 1f)]
    public float rangePercent = 0.2f; // На скільки відсотків (0.2 = 20%) змінюється яскравість
    public float speed = 0.1f;        // Швидкість зміни

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (lightSource != null)
        {
            baseIntensity = lightSource.intensity;
        }
    }

    void Update()
    {
        if (lightSource == null) return;

        // Використовуємо шум Перліна для плавного, але хаотичного мигання
        float noise = Mathf.PerlinNoise(Time.time * speed * 10f, 0f);

        // Розраховуємо коефіцієнт від (1 - range) до (1 + range)
        float multiplier = Mathf.Lerp(1f - rangePercent, 1f + rangePercent, noise);

        lightSource.intensity = baseIntensity * multiplier;
    }
}