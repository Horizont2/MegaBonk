using UnityEngine;

// 6 станів для максимальної реалістичності
public enum Season { Summer, EarlyAutumn, Autumn, LateAutumn, Winter, Spring }

public class SeasonManager : MonoBehaviour
{
    [Header("Current State")]
    public Season currentSeason = Season.Summer;
    public Material globalMaterial; // Сюди rpgpp_lt_mat_a

    [Header("Generated Textures")]
    public Texture2D summerTexture;       // 1. Літо (Оригінал)
    public Texture2D earlyAutumnTexture;  // 2. Рання осінь
    public Texture2D autumnTexture;       // 3. Золота осінь
    public Texture2D lateAutumnTexture;   // 4. Пізня осінь
    public Texture2D winterTexture;       // 5. Зима
    public Texture2D springTexture;       // 6. Весна

    [Header("Lighting - Sun")]
    public Light directionalLight;
    public Color sunSummer = new Color(1f, 0.95f, 0.8f);
    public Color sunEarlyAutumn = new Color(1f, 0.9f, 0.7f);
    public Color sunAutumn = new Color(1f, 0.7f, 0.4f);
    public Color sunLateAutumn = new Color(0.8f, 0.75f, 0.7f); // Тьмяне сонце
    public Color sunWinter = new Color(0.7f, 0.8f, 1f);        // Холодне
    public Color sunSpring = new Color(0.9f, 0.95f, 0.9f);

    [Header("Lighting - Fog")]
    public Color fogSummer = new Color(0.4f, 0.5f, 0.4f);
    public Color fogEarlyAutumn = new Color(0.5f, 0.5f, 0.35f);
    public Color fogAutumn = new Color(0.6f, 0.4f, 0.3f);
    public Color fogLateAutumn = new Color(0.45f, 0.4f, 0.4f); // Сірий, похмурий
    public Color fogWinter = new Color(0.6f, 0.7f, 0.8f);
    public Color fogSpring = new Color(0.5f, 0.6f, 0.5f);

    [Header("VFX & Particles")]
    public GameObject snowParticles;      // Зима
    public GameObject leavesParticles;    // Осінь
    public GameObject firefliesParticles; // Літо, Весна, Рання осінь
    public GameObject dustParticles;      // Осінь, Пізня осінь

    [Header("Props (Physical Objects)")]
    public GameObject winterProps;  // Кучугури снігу
    public GameObject autumnProps;  // Гарбузи, бочки з яблуками

    private void Start()
    {
        ApplySeason(currentSeason);
    }

    private void Update()
    {
        // Керування цифрами 1-6 на клавіатурі
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplySeason(Season.Summer);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplySeason(Season.EarlyAutumn);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplySeason(Season.Autumn);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplySeason(Season.LateAutumn);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplySeason(Season.Winter);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ApplySeason(Season.Spring);
    }

    public void ApplySeason(Season targetSeason)
    {
        currentSeason = targetSeason;

        // 1. Спочатку вимикаємо АБСОЛЮТНО ВСЕ
        if (snowParticles) snowParticles.SetActive(false);
        if (leavesParticles) leavesParticles.SetActive(false);
        if (firefliesParticles) firefliesParticles.SetActive(false);
        if (dustParticles) dustParticles.SetActive(false);
        if (winterProps) winterProps.SetActive(false);
        if (autumnProps) autumnProps.SetActive(false);

        // 2. Вмикаємо тільки те, що треба для конкретного сезону
        switch (targetSeason)
        {
            case Season.Summer:
                SetLightingAndTexture(sunSummer, fogSummer, summerTexture);
                if (firefliesParticles) firefliesParticles.SetActive(true);
                break;

            case Season.EarlyAutumn:
                SetLightingAndTexture(sunEarlyAutumn, fogEarlyAutumn, earlyAutumnTexture);
                if (firefliesParticles) firefliesParticles.SetActive(true);
                if (dustParticles) dustParticles.SetActive(true);
                break;

            case Season.Autumn:
                SetLightingAndTexture(sunAutumn, fogAutumn, autumnTexture);
                if (leavesParticles) leavesParticles.SetActive(true);
                if (dustParticles) dustParticles.SetActive(true);
                if (autumnProps) autumnProps.SetActive(true);
                break;

            case Season.LateAutumn:
                SetLightingAndTexture(sunLateAutumn, fogLateAutumn, lateAutumnTexture);
                if (leavesParticles) leavesParticles.SetActive(true);
                break;

            case Season.Winter:
                SetLightingAndTexture(sunWinter, fogWinter, winterTexture);
                if (snowParticles) snowParticles.SetActive(true);
                if (winterProps) winterProps.SetActive(true);
                break;

            case Season.Spring:
                SetLightingAndTexture(sunSpring, fogSpring, springTexture);
                if (firefliesParticles) firefliesParticles.SetActive(true);
                break;
        }
    }

    // Допоміжний метод, щоб код був чистим
    private void SetLightingAndTexture(Color sunColor, Color fogColor, Texture2D tex)
    {
        if (directionalLight) directionalLight.color = sunColor;
        RenderSettings.fogColor = fogColor;

        if (globalMaterial != null && tex != null)
        {
            globalMaterial.SetTexture("_BaseMap", tex);
        }
    }

    // Запобіжник: повертаємо літо при виході з гри
    private void OnApplicationQuit()
    {
        if (globalMaterial != null && summerTexture != null)
        {
            globalMaterial.SetTexture("_BaseMap", summerTexture);
        }
    }
}