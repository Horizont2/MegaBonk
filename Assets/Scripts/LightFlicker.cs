using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float minIntensity = 80f;  // Мінімальна яскравість (коли вітер приглушує вогонь)
    public float maxIntensity = 130f; // Максимальна яскравість (коли вогонь спалахує)
    public float flickerSpeed = 3f;   // Швидкість мерехтіння

    private Light targetLight;
    private float randomOffset;       // Щоб різні ліхтарі не блимали синхронно

    private void Start()
    {
        targetLight = GetComponent<Light>();

        // Генеруємо випадкове зміщення для кожного ліхтаря, 
        // щоб вони всі мерехтіли в різному ритмі
        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // PerlinNoise генерує плавне випадкове число від 0 до 1.
        // Ми множимо час на швидкість, щоб контролювати ритм.
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, randomOffset);

        // Lerp плавно переміщує яскравість між мінімумом і максимумом на основі шуму
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}