using UnityEngine;

public class MissionInitializer : MonoBehaviour
{
    public static MissionInitializer Instance;

    [Header("Debug Info (Read Only)")]
    public string currentRegionName;
    public int currentBiomeIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("IsRegionMission", 0) == 1)
        {
            SetupMission();
        }
        else
        {
            Debug.Log("<color=yellow>[Mission]</color> Звичайний запуск сцени (не з Мапи).");
        }
    }

    private void SetupMission()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentRegion == null)
        {
            Debug.LogError("[Mission] RegionData не знайдено! Завантаження дефолтного рівня.");
            return;
        }

        RegionData activeRegion = GameManager.Instance.currentRegion;
        currentRegionName = activeRegion.regionName;

        // Читаємо біом (твій WorldGenerator робить те саме для створення землі)
        currentBiomeIndex = PlayerPrefs.GetInt("RegionBiomeType", 0);

        Debug.Log($"<color=#00FF00>[Mission]</color> Генерація місії: {currentRegionName}. Біом ID: {currentBiomeIndex}");

        // ІНТЕГРАЦІЯ З SmartSeasonManager (Налаштовуємо небо, світло і туман)
        SmartSeasonManager seasonManager = FindFirstObjectByType<SmartSeasonManager>();
        if (seasonManager != null)
        {
            seasonManager.LockSeasonForMission(currentBiomeIndex);
        }
    }
}