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
    public float dayDurationMinutes = 15f;
    [Range(0f, 1f)] public float timeOfDay = 0.4f;
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);
    public Color nightFogColor = new Color(0.02f, 0.02f, 0.08f);
    private float defaultSunIntensity;

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
    private bool isMissionMode = false; // ÍÎÂÅ: Ôëàã äëÿ ì³ñ³é

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
        if (isMissionMode) return; // Ó áîéîâ³é ñöåí³ ÷àñ íå éäå!

        currentSeasonTimer += Time.deltaTime;
        if (currentSeasonTimer >= totalSecondsPerSeason)
        {
            AdvanceToNextSeason();
        }

        if (enableDayNight)
        {
            timeOfDay += Time.deltaTime / (dayDurationMinutes * 60f);
            if (timeOfDay >= 1f) timeOfDay -= 1f;
            UpdateDayNightVisuals();
        }
    }

    private void UpdateDayNightVisuals()
    {
        if (directionalLight == null) return;

        float sunAngle = (timeOfDay * 360f) - 90f;
        directionalLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

        float intensityMultiplier = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI));
        directionalLight.intensity = defaultSunIntensity * intensityMultiplier;

        directionalLight.color = Color.Lerp(nightAmbientColor, currentSeasonSunColor, intensityMultiplier);
        RenderSettings.fogColor = Color.Lerp(nightFogColor, currentSeasonFogColor, intensityMultiplier);

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
        if (!isMissionMode) SaveProgress();
    }

    private void UpdateDynamicWeather()
    {
        if (isMissionMode) return;
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

        if (enableDayNight && !isMissionMode) UpdateDayNightVisuals();
    }

    private IEnumerator ShowPropsDelayed(GameObject propsObject, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (propsObject != null) propsObject.SetActive(true);
    }

    private void SetEnvironment(Color sunColor, Color fogColor, Texture2D tex)
    {
        currentSeasonSunColor = sunColor;
        currentSeasonFogColor = fogColor;

        if (!enableDayNight || isMissionMode)
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

    // --- ÍÎÂÈÉ ÌÅÒÎÄ ÄËß ²ÍÒÅÃÐÀÖ²¯ Ç ÁÎÉÎÂÎÞ ÑÖÅÍÎÞ ---
    public void LockSeasonForMission(int biomeIndex)
    {
        isMissionMode = true;
        enableDayNight = false; // Âèìèêàºìî çì³íó äíÿ ³ íî÷³ ï³ä ÷àñ áîþ
        timeOfDay = 0.5f; // Ñòàâèìî ñîíöå â çåí³ò (äåíü)
        isRaining = false;

        // 0 = Forest (Summer), 1 = Desert (Autumn gives warm light), 2 = Winter (Winter)
        if (biomeIndex == 0) ForceSeason(Season.Summer);
        else if (biomeIndex == 1) ForceSeason(Season.EarlyAutumn);
        else if (biomeIndex == 2) ForceSeason(Season.Winter);

        // Ô³êñóºìî îñâ³òëåííÿ æîðñòêî
        if (directionalLight != null)
        {
            directionalLight.intensity = defaultSunIntensity;
            directionalLight.transform.localRotation = Quaternion.Euler(50f, 170f, 0f);
        }
    }
}