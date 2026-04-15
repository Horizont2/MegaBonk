using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public Light sunLight;
    public float dayDurationInSeconds = 120f;

    [Header("Sun Intensity")]
    public float dayIntensity = 1.5f;
    public float nightIntensity = 0f;

    [Header("Night Sky")]
    public ParticleSystem starsParticles;
    public Transform moonTransform; // НОВЕ: Сюди перетягнеш свій об'єкт Moon

    [Header("Weather Settings (Туман)")]
    public float weatherChangeInterval = 60f;
    public float weatherTransitionSpeed = 0.5f;

    [Header("Sunny Atmosphere")]
    public Color daySunnyFog = new Color(0.8f, 0.9f, 1f);
    public Color nightSunnyFog = new Color(0.05f, 0.05f, 0.1f);
    public float sunnyFogDensity = 0.002f;

    [Header("Foggy Atmosphere")]
    public Color dayHeavyFog = new Color(0.6f, 0.6f, 0.65f);
    public Color nightHeavyFog = new Color(0.02f, 0.02f, 0.02f);
    public float heavyFogDensity = 0.015f;

    private bool isSunny = true;
    private float weatherTimer = 0f;

    private float currentFogDensity;
    private Color currentDayFog;
    private Color currentNightFog;

    private void Start()
    {
        if (sunLight == null) sunLight = GetComponent<Light>();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        currentFogDensity = sunnyFogDensity;
        currentDayFog = daySunnyFog;
        currentNightFog = nightSunnyFog;
    }

    private void Update()
    {
        if (sunLight == null) return;

        // --- 1. ОБЕРТАННЯ СОНЦЯ ---
        float rotationAngle = (Time.deltaTime / dayDurationInSeconds) * 360f;
        sunLight.transform.Rotate(Vector3.right, rotationAngle);

        float timeOfDay = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        float blendFactor = Mathf.Clamp01((timeOfDay + 0.2f) / 0.5f);

        sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, blendFactor);

        // --- 2. РОЗУМНА ОРБІТА МІСЯЦЯ ---
        if (moonTransform != null && Camera.main != null)
        {
            Vector3 moonDir = sunLight.transform.forward;

            // СЕКРЕТНИЙ ТРЮК: Штучно виштовхуємо місяць високо в небо, 
            // щоб він завжди був вище за найвищі гори (мінімум 0.6 по Y)
            moonDir.y = Mathf.Max(moonDir.y, 0.6f);

            // Відсуваємо трохи далі
            moonTransform.position = Camera.main.transform.position + moonDir.normalized * 80f;
            moonTransform.LookAt(Camera.main.transform);
        }

        // --- 3. ЗОРІ (Далекий космос) ---
        if (starsParticles != null && Camera.main != null)
        {
            var mainModule = starsParticles.main;
            Color starColor = new Color(1f, 1f, 1f, 1f - blendFactor);
            mainModule.startColor = starColor;

            // Секретний трюк: Зорі завжди літають за гравцем, 
            starsParticles.transform.position = Camera.main.transform.position;
            // АЛЕ ми забороняємо їм крутитися! Тому при повороті камери виникає ефект реального неба
            starsParticles.transform.rotation = Quaternion.identity;
        }

        // --- 4. ПОГОДА ТА ТУМАН ---
        weatherTimer += Time.deltaTime;
        if (weatherTimer >= weatherChangeInterval)
        {
            isSunny = !isSunny;
            weatherTimer = 0f;
        }

        float targetDensity = isSunny ? sunnyFogDensity : heavyFogDensity;
        Color targetDayFog = isSunny ? daySunnyFog : dayHeavyFog;
        Color targetNightFog = isSunny ? nightSunnyFog : nightHeavyFog;

        currentFogDensity = Mathf.Lerp(currentFogDensity, targetDensity, weatherTransitionSpeed * Time.deltaTime);
        currentDayFog = Color.Lerp(currentDayFog, targetDayFog, weatherTransitionSpeed * Time.deltaTime);
        currentNightFog = Color.Lerp(currentNightFog, targetNightFog, weatherTransitionSpeed * Time.deltaTime);

        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = Color.Lerp(currentNightFog, currentDayFog, blendFactor);
    }
}