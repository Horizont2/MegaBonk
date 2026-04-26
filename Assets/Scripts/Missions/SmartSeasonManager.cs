using UnityEngine;
using System.Collections;

public enum Season { Summer, EarlyAutumn, Autumn, LateAutumn, Winter, Spring }

public class SmartSeasonManager : MonoBehaviour
{
    [Header("Time & Save System")]
    public float minutesPerSeason = 20f;
    private float totalSecondsPerSeason;
    public float currentSeasonTimer = 0f;

    [Header("Current State")]
    public Season currentSeason = Season.Summer;
    public Material globalMaterial;
    public bool isRaining = false;

    [Header("Day & Night Cycle (NEW)")]
    public bool enableDayNight = true;
    public float dayDurationMinutes = 15f; // 15 хвилин на одну добу
    [Range(0f, 1f)] public float timeOfDay = 0.4f; // 0 = Схід, 0.5 = Південь, 1 = Наступний схід
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);
    public Color nightFogColor = new Color(0.02f, 0.02f, 0.08f);
    private float defaultSunIntensity;

    // Збереження кольорів поточного сезону для блендінгу з ніччю
    private Color currentSeasonSunColor;
    private Color currentSeasonFogColor;

    [Header("Realism Settings")]
    public float propsDelay = 15f;

    [Header("Generated Textures")]
    public Texture2D summerTexture;
    public Texture2D earlyAutumnTexture;
    public Texture2D autumnTexture;
    public Texture2D lateAutumnTexture;
    public Texture2D winterTexture;
    public Texture2D springTexture;

    [Header("Lighting - Sun (Base Colors)")]
    public Light directionalLight;
    public Color sunSummer = new Color(1f, 0.95f, 0.8f);
    public Color sunEarlyAutumn = new Color(1f, 0.9f, 0.7f);
    public Color sunAutumn = new Color(1f, 0.7f, 0.4f);
    public Color sunLateAutumn = new Color(0.8f, 0.75f, 0.7f);
    public Color sunWinter = new Color(0.7f, 0.8f, 1f);
    public Color sunSpring = new Color(0.9f, 0.95f, 0.9f);

    [Header("Lighting - Fog (Base Colors)")]
    public Color fogSummer = new Color(0.4f, 0.5f, 0.4f);
    public Color fogEarlyAutumn = new Color(0.5f, 0.5f, 0.35f);
    public Color fogAutumn = new Color(0.6f, 0.4f, 0.3f);
    public Color fogLateAutumn = new Color(0.45f, 0.4f, 0.4f);
    public Color fogWinter = new Color(0.6f, 0.7f, 0.8f);
    public Color fogSpring = new Color(0.5f, 0.6f, 0.5f);
    public Color fogRain = new Color(0.3f, 0.35f, 0.4f);

    [Header("VFX & Particles")]
    public GameObject snowParticles;
    public GameObject leavesParticles;
    public GameObject firefliesParticles;
    public GameObject dustParticles;
    public GameObject rainParticles;
    public GameObject playerFootprints;

    [Header("Props (Physical Objects)")]
    public GameObject winterProps;
    public GameObject autumnProps;

    private Coroutine activePropsCoroutine;

    private void Start()
    {
        totalSecondsPerSeason = minutesPerSeason * 60f;

        if (directionalLight != null) defaultSunIntensity = directionalLight.intensity;

        LoadProgress();
        InvokeRepeating("UpdateDynamicWeather", 10f, 180f);
        ApplySeason(currentSeason);
    }

    private void Update()
    {
        // 1. Таймер Сезонів
        currentSeasonTimer += Time.deltaTime;
        if (currentSeasonTimer >= totalSecondsPerSeason)
        {
            AdvanceToNextSeason();
        }

        // 2. Таймер Дня і Ночі
        if (enableDayNight)
        {
            timeOfDay += Time.deltaTime / (dayDurationMinutes * 60f);
            if (timeOfDay >= 1f) timeOfDay -= 1f; // Скидаємо добу на новий день
            UpdateDayNightVisuals();
        }

        // Ручне керування
        if (Input.GetKeyDown(KeyCode.Alpha1)) ForceSeason(Season.Summer);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ForceSeason(Season.EarlyAutumn);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ForceSeason(Season.Autumn);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ForceSeason(Season.LateAutumn);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ForceSeason(Season.Winter);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ForceSeason(Season.Spring);
    }

    private void UpdateDayNightVisuals()
    {
        if (directionalLight == null) return;

        // Обертання Сонця (360 градусів за добу)
        float sunAngle = (timeOfDay * 360f) - 90f; // -90 щоб 0.0 був сходом сонця
        directionalLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Інтенсивність Сонця (Вночі воно "вимикається")
        float intensityMultiplier = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI));
        directionalLight.intensity = defaultSunIntensity * intensityMultiplier;

        // Плавне змішування кольорів між поточним сезоном і ніччю
        directionalLight.color = Color.Lerp(nightAmbientColor, currentSeasonSunColor, intensityMultiplier);
        RenderSettings.fogColor = Color.Lerp(nightFogColor, currentSeasonFogColor, intensityMultiplier);

        // Вмикаємо світлячків вночі (якщо це літо і немає дощу)
        if (currentSeason == Season.Summer && !isRaining && firefliesParticles)
        {
            firefliesParticles.SetActive(intensityMultiplier < 0.2f);
        }
    }

    private void AdvanceToNextSeason()
    {
        currentSeasonTimer = 0f;
        currentSeason = (Season)(((int)currentSeason + 1) % 6);
        UpdateDynamicWeather();
        ApplySeason(currentSeason);
        SaveProgress();
    }

    private void ForceSeason(Season newSeason)
    {
        currentSeason = newSeason;
        currentSeasonTimer = 0f;
        ApplySeason(currentSeason);
        SaveProgress();
    }

    private void UpdateDynamicWeather()
    {
        isRaining = false;
        if (currentSeason == Season.Autumn || currentSeason == Season.LateAutumn || currentSeason == Season.Spring)
        {
            if (Random.Range(0, 100) < 40) isRaining = true;
        }
        ApplySeason(currentSeason);
    }

    public void ApplySeason(Season targetSeason)
    {
        if (activePropsCoroutine != null) StopCoroutine(activePropsCoroutine);

        if (snowParticles) snowParticles.SetActive(false);
        if (leavesParticles) leavesParticles.SetActive(false);
        if (firefliesParticles) firefliesParticles.SetActive(false);
        if (dustParticles) dustParticles.SetActive(false);
        if (rainParticles) rainParticles.SetActive(false);
        if (winterProps) winterProps.SetActive(false);
        if (autumnProps) autumnProps.SetActive(false);
        if (playerFootprints) playerFootprints.SetActive(false);

        switch (targetSeason)
        {
            case Season.Summer:
                SetEnvironment(sunSummer, fogSummer, summerTexture);
                break;
            case Season.EarlyAutumn:
                SetEnvironment(sunEarlyAutumn, fogEarlyAutumn, earlyAutumnTexture);
                if (dustParticles && !isRaining) dustParticles.SetActive(true);
                break;
            case Season.Autumn:
                SetEnvironment(sunAutumn, fogAutumn, autumnTexture);
                if (leavesParticles) leavesParticles.SetActive(true);
                if (autumnProps) activePropsCoroutine = StartCoroutine(ShowPropsDelayed(autumnProps, propsDelay));
                break;
            case Season.LateAutumn:
                SetEnvironment(sunLateAutumn, fogLateAutumn, lateAutumnTexture);
                if (leavesParticles) leavesParticles.SetActive(true);
                break;
            case Season.Winter:
                SetEnvironment(sunWinter, fogWinter, winterTexture);
                if (snowParticles) snowParticles.SetActive(true);
                if (playerFootprints) playerFootprints.SetActive(true);
                if (winterProps) activePropsCoroutine = StartCoroutine(ShowPropsDelayed(winterProps, propsDelay));
                break;
            case Season.Spring:
                SetEnvironment(sunSpring, fogSpring, springTexture);
                break;
        }

        if (isRaining)
        {
            if (rainParticles) rainParticles.SetActive(true);
            currentSeasonFogColor = fogRain;
            currentSeasonSunColor = Color.Lerp(currentSeasonSunColor, Color.gray, 0.5f);
        }

        // Оновлюємо день/ніч одразу після зміни сезону
        if (enableDayNight) UpdateDayNightVisuals();
    }

    private IEnumerator ShowPropsDelayed(GameObject propsObject, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (propsObject != null) propsObject.SetActive(true);
    }

    private void SetEnvironment(Color sunColor, Color fogColor, Texture2D tex)
    {
        // Зберігаємо кольори сезону, щоб змішувати їх з ніччю
        currentSeasonSunColor = sunColor;
        currentSeasonFogColor = fogColor;

        if (!enableDayNight)
        {
            if (directionalLight) directionalLight.color = sunColor;
            RenderSettings.fogColor = fogColor;
        }

        if (globalMaterial != null && tex != null) globalMaterial.SetTexture("_BaseMap", tex);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("SavedSeason", (int)currentSeason);
        PlayerPrefs.SetFloat("SavedTimer", currentSeasonTimer);
        PlayerPrefs.SetFloat("SavedTimeOfDay", timeOfDay);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        if (PlayerPrefs.HasKey("SavedSeason"))
        {
            currentSeason = (Season)PlayerPrefs.GetInt("SavedSeason");
            currentSeasonTimer = PlayerPrefs.GetFloat("SavedTimer");
            timeOfDay = PlayerPrefs.GetFloat("SavedTimeOfDay", 0.4f);
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
        if (globalMaterial != null && summerTexture != null) globalMaterial.SetTexture("_BaseMap", summerTexture);
    }
}