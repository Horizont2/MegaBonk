using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public Light sunLight;
    [Tooltip("—к≥льки реальних секунд триваЇ доба")]
    public float dayDurationInSeconds = 120f;

    [Header("Sun Intensity")]
    public float dayIntensity = 1.5f;
    public float nightIntensity = 0f; // ¬ноч≥ сонце маЇ вимикатис€, щоб стало темно

    [Header("Weather Settings (“уман)")]
    public float weatherChangeInterval = 60f; // «м≥на погоди кожн≥ 60 сек
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

        // ¬микаЇмо правильний режим туману
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        currentFogDensity = sunnyFogDensity;
        currentDayFog = daySunnyFog;
        currentNightFog = nightSunnyFog;
    }

    private void Update()
    {
        if (sunLight == null) return;

        // --- 1. ќЅ≈–“јЌЌя —ќЌ÷я (ƒень/Ќ≥ч) ---
        float rotationAngle = (Time.deltaTime / dayDurationInSeconds) * 360f;
        sunLight.transform.Rotate(Vector3.right, rotationAngle);

        // –ахуЇмо час доби (1 = полудень, -1 = п≥вн≥ч)
        float timeOfDay = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        float blendFactor = Mathf.Clamp01((timeOfDay + 0.2f) / 0.5f);

        // ѕлавно вимикаЇмо €скрав≥сть сонц€ вноч≥
        sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, blendFactor);


        // --- 2. —»—“≈ћј ѕќ√ќƒ» (—он€чно <-> “уман) ---
        weatherTimer += Time.deltaTime;
        if (weatherTimer >= weatherChangeInterval)
        {
            isSunny = !isSunny;
            weatherTimer = 0f;
        }

        // ¬изначаЇмо ц≥льов≥ значенн€ дл€ поточноњ погоди
        float targetDensity = isSunny ? sunnyFogDensity : heavyFogDensity;
        Color targetDayFog = isSunny ? daySunnyFog : dayHeavyFog;
        Color targetNightFog = isSunny ? nightSunnyFog : nightHeavyFog;

        // ѕлавно переходимо м≥ж станами погоди
        currentFogDensity = Mathf.Lerp(currentFogDensity, targetDensity, weatherTransitionSpeed * Time.deltaTime);
        currentDayFog = Color.Lerp(currentDayFog, targetDayFog, weatherTransitionSpeed * Time.deltaTime);
        currentNightFog = Color.Lerp(currentNightFog, targetNightFog, weatherTransitionSpeed * Time.deltaTime);


        // --- 3. «ј—“ќ—”¬јЌЌя ---
        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = Color.Lerp(currentNightFog, currentDayFog, blendFactor);

        // ¬ј∆Ћ»¬ќ: я видалив р€док зм≥ни Camera.backgroundColor.
        // “епер камера показуватиме справжн≥й Skybox ≥з з≥рками та сонцем!
    }
}