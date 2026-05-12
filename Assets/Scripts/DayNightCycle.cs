using UnityEngine;
using System.Collections;

public enum WeatherState { Clear, Precipitation, Storm }

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Тривалість однієї ігрової доби в реальних секундах (напр. 300 = 5 хвилин)")]
    public float dayDurationInSeconds = 300f;
    [Range(0f, 24f)] public float timeOfDay = 8f;

    [Header("Light Sources")]
    public Light sunLight;
    public Light moonLight;
    public Light lightningLight;

    [Header("Atmosphere (Gradients)")]
    public Gradient sunColor;
    public Gradient fogColorClear;
    public Gradient fogColorStorm;

    [Header("Intensity Curves")]
    public AnimationCurve sunIntensity;
    public AnimationCurve moonIntensity;

    [Header("Weather System")]
    public WeatherState currentWeather = WeatherState.Clear;
    public float weatherChangeInterval = 15f;
    public float weatherTransitionSpeed = 0.05f;

    [Header("Fog Densities")]
    public float clearFogDensity = 0.005f;
    public float stormFogDensity = 0.02f;

    [Header("Standard Wind System")]
    public WindZone windZone;
    [Tooltip("Сила вітру в ясну погоду")]
    public float clearWindMain = 0.2f;
    [Tooltip("Сила вітру під час шторму")]
    public float stormWindMain = 1.5f;
    public float clearWindTurbulence = 0.1f;
    public float stormWindTurbulence = 1.2f;
    [Tooltip("Швидкість зміни напрямку вітру")]
    public float windRotationSpeed = 2f;

    private float currentFogDensity;
    private float weatherBlend = 0f;
    private float weatherTimer = 0f;
    private int currentBiome = 0;

    [Header("VFX & Particles")]
    public ParticleSystem starsParticles;
    public GameObject firefliesVFX;
    public ParticleSystem rainVFX;
    public ParticleSystem snowVFX;
    public ParticleSystem dustVFX;

    private Coroutine lightningCoroutine;

    private void Start()
    {
        currentBiome = PlayerPrefs.GetInt("RegionBiomeType", 0);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        currentFogDensity = clearFogDensity;

        if (lightningLight != null) lightningLight.intensity = 0f;
        if (moonLight != null) moonLight.color = new Color(0.6f, 0.7f, 1f);

        UpdateWeatherVFX();
    }

    private void Update()
    {
        timeOfDay += (Time.deltaTime / dayDurationInSeconds) * 24f;
        if (timeOfDay >= 24f) timeOfDay = 0f;

        float timePercent = timeOfDay / 24f;

        float sunAngle = ((timeOfDay - 6f) / 12f) * 180f;
        if (sunLight != null)
        {
            sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sunLight.color = sunColor.Evaluate(timePercent);
            float baseIntensity = sunIntensity.Evaluate(timePercent);
            sunLight.intensity = Mathf.Lerp(baseIntensity, baseIntensity * 0.2f, weatherBlend);
        }

        if (moonLight != null)
        {
            moonLight.transform.localRotation = Quaternion.Euler(sunAngle - 180f, 170f, 0f);
            moonLight.intensity = moonIntensity.Evaluate(timePercent) * (1f - weatherBlend);
        }

        ManageNightVFX(timePercent);

        weatherTimer += Time.deltaTime;
        if (weatherTimer >= weatherChangeInterval)
        {
            ChangeWeatherRandomly();
            weatherTimer = 0f;
        }

        float targetBlend = (currentWeather == WeatherState.Clear) ? 0f : (currentWeather == WeatherState.Storm ? 1f : 0.5f);
        weatherBlend = Mathf.Lerp(weatherBlend, targetBlend, Time.deltaTime * weatherTransitionSpeed);

        currentFogDensity = Mathf.Lerp(clearFogDensity, stormFogDensity, weatherBlend);
        Color clearFog = fogColorClear.Evaluate(timePercent);
        Color stormFog = fogColorStorm.Evaluate(timePercent);

        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = Color.Lerp(clearFog, stormFog, weatherBlend);

        // --- НОВЕ: ДИНАМІЧНИЙ СКАЙБОКС ТА АТМОСФЕРА ---
        if (RenderSettings.skybox != null)
        {
            // Плавно зменшуємо яскравість неба під час шторму (Exposure)
            float exposure = Mathf.Lerp(0.7f, 0.35f, weatherBlend);
            if (RenderSettings.skybox.HasProperty("_Exposure"))
            {
                RenderSettings.skybox.SetFloat("_Exposure", exposure);
            }

            // Забарвлюємо небо у сірий відтінок
            if (RenderSettings.skybox.HasProperty("_Tint"))
            {
                RenderSettings.skybox.SetColor("_Tint", Color.Lerp(Color.white, new Color(0.4f, 0.4f, 0.45f), weatherBlend));
            }
        }

        // Робимо глобальне освітлення сцени (Gradient Ambient) похмурим
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.42f, 0.70f, 0.96f), new Color(0.2f, 0.25f, 0.3f), weatherBlend);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.76f, 0.79f, 0.84f), new Color(0.15f, 0.2f, 0.25f), weatherBlend);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.23f, 0.30f, 0.23f), new Color(0.1f, 0.1f, 0.1f), weatherBlend);

        UpdateVFXPositions();
    }

    private void UpdateVFXPositions()
    {
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;

        if (starsParticles != null)
        {
            starsParticles.transform.position = camPos;
            starsParticles.transform.rotation = Quaternion.identity;
        }

        ParticleSystem[] weatherVFX = { rainVFX, snowVFX, dustVFX };
        foreach (var vfx in weatherVFX)
        {
            if (vfx != null && vfx.gameObject.activeSelf)
            {
                vfx.transform.position = camPos + Vector3.up * 12f;
                vfx.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }

    private void ManageNightVFX(float timePercent)
    {
        bool isNight = timeOfDay < 5f || timeOfDay > 19f;

        if (starsParticles != null && Camera.main != null)
        {
            var main = starsParticles.main;
            float starAlpha = isNight ? (1f - weatherBlend) : 0f;
            main.startColor = new Color(1f, 1f, 1f, starAlpha);
        }

        if (firefliesVFX != null)
        {
            bool showFireflies = isNight && currentBiome == 0 && currentWeather == WeatherState.Clear;
            firefliesVFX.SetActive(showFireflies);
        }
    }

    private void ChangeWeatherRandomly()
    {
        float roll = Random.value;

        if (currentWeather == WeatherState.Clear)
        {
            if (roll < 0.4f) currentWeather = WeatherState.Precipitation;
            else if (roll < 0.6f) currentWeather = WeatherState.Storm;
            else currentWeather = WeatherState.Clear;
        }
        else
        {
            if (roll < 0.7f) currentWeather = WeatherState.Clear;
            else currentWeather = WeatherState.Precipitation;
        }

        UpdateWeatherVFX();

        if (currentWeather == WeatherState.Storm && currentBiome == 0)
        {
            if (lightningCoroutine == null) lightningCoroutine = StartCoroutine(LightningRoutine());
        }
        else
        {
            if (lightningCoroutine != null) { StopCoroutine(lightningCoroutine); lightningCoroutine = null; }
        }
    }

    private void UpdateWeatherVFX()
    {
        if (rainVFX != null) rainVFX.gameObject.SetActive(false);
        if (snowVFX != null) snowVFX.gameObject.SetActive(false);
        if (dustVFX != null) dustVFX.gameObject.SetActive(false);

        if (currentWeather != WeatherState.Clear)
        {
            float emissionMultiplier = (currentWeather == WeatherState.Storm) ? 2f : 1f;

            if (currentBiome == 0 && rainVFX != null)
            {
                rainVFX.gameObject.SetActive(true);
                var em = rainVFX.emission; em.rateOverTimeMultiplier *= emissionMultiplier;
            }
            else if (currentBiome == 1 && dustVFX != null)
            {
                dustVFX.gameObject.SetActive(true);
                var em = dustVFX.emission; em.rateOverTimeMultiplier *= emissionMultiplier;
            }
            else if (currentBiome == 2 && snowVFX != null)
            {
                snowVFX.gameObject.SetActive(true);
                var em = snowVFX.emission; em.rateOverTimeMultiplier *= emissionMultiplier;
            }
        }
    }

    private IEnumerator LightningRoutine()
    {
        while (currentWeather == WeatherState.Storm)
        {
            yield return new WaitForSeconds(Random.Range(5f, 15f));

            if (lightningLight != null)
            {
                lightningLight.intensity = Random.Range(3f, 6f);
                yield return new WaitForSeconds(0.05f);
                lightningLight.intensity = 0f;
                yield return new WaitForSeconds(0.1f);

                lightningLight.intensity = Random.Range(1f, 3f);
                yield return new WaitForSeconds(0.05f);
                lightningLight.intensity = 0f;
            }
        }
        lightningCoroutine = null;
    }
}