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
    public float nightSpeedMultiplier = 2.5f; // Швидкість плину часу вночі
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
    private bool isMissionMode = false; // НОВЕ: Флаг для місій

    [Header("AAA Transitions")]
    public float transitionDuration = 12f; // Скільки секунд триватиме плавна зміна погоди
    private Coroutine transitionCoroutine;

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
        if (isMissionMode) return; // У бойовій сцені час не йде!

        currentSeasonTimer += Time.deltaTime;
        if (currentSeasonTimer >= totalSecondsPerSeason)
        {
            AdvanceToNextSeason();
        }

        if (enableDayNight)
        {
            float speed = 1f;
            // Ніч - це коли сонце зайшло (приблизно timeOfDay < 0.2 або > 0.8)
            if (timeOfDay < 0.2f || timeOfDay > 0.8f) speed = nightSpeedMultiplier;

            timeOfDay += (Time.deltaTime * speed) / (dayDurationMinutes * 60f);
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

        // 1. Плавне увімкнення/вимкнення частинок (дозволяє їм зникнути природньо)
        SetParticlesActive(snowParticles, targetSeason == Season.Winter);
        SetParticlesActive(leavesParticles, targetSeason == Season.Autumn || targetSeason == Season.LateAutumn);
        SetParticlesActive(firefliesParticles, targetSeason == Season.Summer && !isRaining);
        SetParticlesActive(dustParticles, targetSeason == Season.EarlyAutumn && !isRaining);
        SetParticlesActive(rainParticles, isRaining);

        if (winterProps) winterProps.SetActive(false);
        if (autumnProps) autumnProps.SetActive(false);
        if (playerFootprints) playerFootprints.SetActive(targetSeason == Season.Winter);

        // 2. Готуємо цільові кольори для плавного переходу
        Color targetSun = Color.white;
        Color targetFog = Color.white;
        Texture2D targetTex = null;

        switch (targetSeason)
        {
            case Season.Summer:      targetSun = sunSummer;      targetFog = fogSummer;      targetTex = summerTexture;      break;
            case Season.EarlyAutumn: targetSun = sunEarlyAutumn; targetFog = fogEarlyAutumn; targetTex = earlyAutumnTexture; break;
            case Season.Autumn:      targetSun = sunAutumn;      targetFog = fogAutumn;      targetTex = autumnTexture;
                if (autumnProps) activePropsCoroutine = StartCoroutine(ShowPropsDelayed(autumnProps, propsDelay)); break;
            case Season.LateAutumn:  targetSun = sunLateAutumn;  targetFog = fogLateAutumn;  targetTex = lateAutumnTexture;  break;
            case Season.Winter:      targetSun = sunWinter;      targetFog = fogWinter;      targetTex = winterTexture;
                if (winterProps) activePropsCoroutine = StartCoroutine(ShowPropsDelayed(winterProps, propsDelay)); break;
            case Season.Spring:      targetSun = sunSpring;      targetFog = fogSpring;      targetTex = springTexture;      break;
        }

        if (isRaining)
        {
            targetFog = fogRain;
            // Змішуємо цільовий колір із густим темно-синім штормовим відтінком
            targetSun = Color.Lerp(targetSun, new Color(0.3f, 0.35f, 0.45f), 0.75f); 
        }

        // 3. Запускаємо плавну зміну
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(EnvironmentTransitionRoutine(targetSun, targetFog, targetTex));
    }

    private void SetParticlesActive(GameObject vfx, bool active)
    {
        if (vfx == null) return;
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            if (active)
            {
                vfx.SetActive(true);
                if (!ps.isPlaying) ps.Play();
            }
            else
            {
                // Замість миттєвого зникнення (SetActive(false)), наказуємо частинкам припинити спавн
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        else vfx.SetActive(active);
    }

    private IEnumerator EnvironmentTransitionRoutine(Color targetSun, Color targetFog, Texture2D tex)
    {
        Color startSun = currentSeasonSunColor;
        Color startFog = currentSeasonFogColor;

        if (globalMaterial != null && tex != null) globalMaterial.SetTexture("_BaseMap", tex);

        float elapsed = 0f;
        // Якщо це старт гри (кольори ще нульові), робимо перехід миттєвим, щоб не було чорного екрана
        float actualDuration = (startSun == new Color(0, 0, 0, 0)) ? 0.1f : transitionDuration;

        while (elapsed < actualDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / actualDuration;

            t = t * t * (3f - 2f * t); // SmoothStep для кінематографічної плавності

            currentSeasonSunColor = Color.Lerp(startSun, targetSun, t);
            currentSeasonFogColor = Color.Lerp(startFog, targetFog, t);

            if (!enableDayNight || isMissionMode)
            {
                if (directionalLight) directionalLight.color = currentSeasonSunColor;
                RenderSettings.fogColor = currentSeasonFogColor;
            }

            yield return null; // Чекаємо наступного кадру
        }

        currentSeasonSunColor = targetSun;
        currentSeasonFogColor = targetFog;

        if (!enableDayNight || isMissionMode)
        {
            if (directionalLight) directionalLight.color = currentSeasonSunColor;
            RenderSettings.fogColor = currentSeasonFogColor;
        }
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

    // --- НОВИЙ МЕТОД ДЛЯ ІНТЕГРАЦІЇ З БОЙОВОЮ СЦЕНОЮ ---
    public void LockSeasonForMission(int biomeIndex)
    {
        isMissionMode = true;
        enableDayNight = false; // Вимикаємо зміну дня і ночі під час бою
        timeOfDay = 0.5f; // Ставимо сонце в зеніт (день)
        isRaining = false;

        // 0 = Forest (Summer), 1 = Desert (Autumn gives warm light), 2 = Winter (Winter)
        if (biomeIndex == 0) ForceSeason(Season.Summer);
        else if (biomeIndex == 1) ForceSeason(Season.EarlyAutumn);
        else if (biomeIndex == 2) ForceSeason(Season.Winter);

        // Фіксуємо освітлення жорстко
        if (directionalLight != null)
        {
            directionalLight.intensity = defaultSunIntensity;
            directionalLight.transform.localRotation = Quaternion.Euler(50f, 170f, 0f);
        }
    }
}