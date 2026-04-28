using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class BuildingLevel
{
    public int costWood;
    public int costStone;
    public int costFood;
    public int productionValue;
    public string productionDescription;
}

public class CampBuilding : MonoBehaviour
{
    [Header("Unique ID")]
    public string buildingID = "Building_01";

    [Header("Building Objects")]
    public GameObject ghostModel;
    public GameObject realModel;

    [Header("Visual Production Piles (NEW)")]
    [Tooltip("Поклади сюди об'єкти колод/каменів, які будуть з'являтися по черзі")]
    public GameObject[] resourceVisuals;
    private int currentVisualIndex = 0;

    [Header("Building Info")]
    public string buildingName = "BUILDING";
    [TextArea] public string description = "Building description.";
    public bool isStorageVault = false;

    [Header("Levels & Upgrades")]
    public int currentLevel = 0;
    public BuildingLevel[] levels;

    [Header("Hold To Build Mechanic")]
    public float holdTimeRequired = 5f;
    private float currentHoldTime = 0f;

    [Header("UI References")]
    public GameObject uiCanvas;
    public Image holdFillImage;
    public Image holdKeyIconImage;
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("UI Text References")]
    public TextMeshProUGUI titleTMP;
    public TextMeshProUGUI descTMP;
    public TextMeshProUGUI prodTMP;
    public TextMeshProUGUI costWoodTMP;
    public TextMeshProUGUI costStoneTMP;
    public TextMeshProUGUI costFoodTMP;
    public TextMeshProUGUI buildHintTMP;

    [Header("3D Effects & Seasons")]
    public GameObject upgradeGlimmer;
    public GameObject snowClumps;

    [Header("Cinematic Effects")]
    public ParticleSystem buildDustVFX;
    public AudioSource buildAudio;
    public float buildDuration = 2.5f;
    public float spawnDepth = 12f;
    public float upgradeBounceAmount = 1.15f;

    private bool playerInRange = false;
    private bool isAnimating = false;
    private float glimmerCheckTimer = 0f;

    private SmartSeasonManager seasonManager;

    private void Start()
    {
        seasonManager = FindFirstObjectByType<SmartSeasonManager>();

        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (holdFillImage != null) holdFillImage.fillAmount = 0f;
        if (holdKeyIconImage != null) holdKeyIconImage.color = normalColor;

        StopDustEffect();
        HideAllVisualResources();

        currentLevel = PlayerPrefs.GetInt("SaveBld_" + buildingID, 0);

        if (currentLevel == 0)
        {
            ghostModel.SetActive(true);
            realModel.SetActive(false);
        }
        else
        {
            ghostModel.SetActive(false);
            realModel.SetActive(true);
            ApplyBuildingEffects();
        }

        UpdateGlimmerState();
    }

    private void Update()
    {
        if (snowClumps != null && seasonManager != null)
        {
            bool isWinter = (seasonManager.currentSeason == Season.Winter);
            bool isBuilt = (currentLevel > 0);
            snowClumps.SetActive(isBuilt && isWinter);
        }

        if (isAnimating) return;

        glimmerCheckTimer += Time.deltaTime;
        if (glimmerCheckTimer >= 1f)
        {
            glimmerCheckTimer = 0f;
            UpdateGlimmerState();
            if (playerInRange) UpdateUIData();
        }

        if (!playerInRange) return;

        if (currentLevel >= levels.Length)
        {
            if (uiCanvas.activeSelf) uiCanvas.SetActive(false);
            return;
        }

        BuildingLevel nextLevelData = levels[currentLevel];
        bool canAfford = ResourceManager.Instance.CanAffordStash(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);

        if (Input.GetKey(KeyCode.E) && canAfford)
        {
            currentHoldTime += Time.deltaTime;
            if (holdFillImage != null) holdFillImage.fillAmount = currentHoldTime / holdTimeRequired;
            if (holdKeyIconImage != null) holdKeyIconImage.color = pressedColor;

            StartDustEffect();

            if (currentHoldTime >= holdTimeRequired)
            {
                currentHoldTime = 0f;
                ResourceManager.Instance.SpendStashResources(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);

                currentLevel++;
                PlayerPrefs.SetInt("SaveBld_" + buildingID, currentLevel);
                PlayerPrefs.Save();

                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.AddProgress(MissionType.BuildStructures, 1);
                }

                if (holdKeyIconImage != null) holdKeyIconImage.color = normalColor;

                StartCoroutine(BuildSequence());
            }
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.E) && holdKeyIconImage != null)
            {
                holdKeyIconImage.color = normalColor;
            }

            if (currentHoldTime > 0)
            {
                currentHoldTime -= Time.deltaTime * 2f;
                currentHoldTime = Mathf.Max(0, currentHoldTime);
                if (holdFillImage != null) holdFillImage.fillAmount = currentHoldTime / holdTimeRequired;
            }

            StopDustEffect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentLevel < levels.Length)
        {
            playerInRange = true;
            UpdateUIData();

            if (holdKeyIconImage != null) holdKeyIconImage.color = normalColor;
            if (uiCanvas != null)
            {
                uiCanvas.SetActive(true);
                StartCoroutine(PopUpUI());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            currentHoldTime = 0f;
            if (uiCanvas != null) uiCanvas.SetActive(false);
            if (holdKeyIconImage != null) holdKeyIconImage.color = normalColor;
            StopDustEffect();
        }
    }

    private void UpdateUIData()
    {
        if (currentLevel >= levels.Length) return;

        BuildingLevel nextLevelData = levels[currentLevel];

        if (titleTMP) titleTMP.text = buildingName + $" (LVL {currentLevel})";
        if (descTMP) descTMP.text = description;

        if (currentLevel == 0)
        {
            if (prodTMP) prodTMP.text = nextLevelData.productionDescription;
            if (buildHintTMP) buildHintTMP.text = "Press E to Build";
        }
        else
        {
            BuildingLevel currentData = levels[currentLevel - 1];
            if (prodTMP) prodTMP.text = $"{currentData.productionDescription}\n<color=#00FF00>-> {nextLevelData.productionDescription}</color>";
            if (buildHintTMP) buildHintTMP.text = "Press E to Upgrade";
        }

        if (ResourceManager.Instance != null)
        {
            string woodColor = ResourceManager.Instance.stashWood >= nextLevelData.costWood ? "#FFFFFF" : "#FF4444";
            string stoneColor = ResourceManager.Instance.stashStone >= nextLevelData.costStone ? "#FFFFFF" : "#FF4444";
            string foodColor = ResourceManager.Instance.stashFood >= nextLevelData.costFood ? "#FFFFFF" : "#FF4444";

            if (costWoodTMP) costWoodTMP.text = $"<color={woodColor}>Logs: {nextLevelData.costWood}</color>";
            if (costStoneTMP) costStoneTMP.text = $"<color={stoneColor}>Stones: {nextLevelData.costStone}</color>";
            if (costFoodTMP) costFoodTMP.text = $"<color={foodColor}>Food: {nextLevelData.costFood}</color>";
        }
    }

    private IEnumerator PopUpUI()
    {
        uiCanvas.transform.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            uiCanvas.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(0.005f, 0.005f, 0.005f), Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }

    private IEnumerator BuildSequence()
    {
        isAnimating = true;
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (upgradeGlimmer != null) upgradeGlimmer.SetActive(false);

        ghostModel.SetActive(false);
        realModel.SetActive(true);

        Vector3 finalPos = realModel.transform.position;
        Vector3 originalScale = realModel.transform.localScale;

        if (currentLevel == 1) realModel.transform.position -= new Vector3(0, spawnDepth, 0);

        StartDustEffect();
        if (buildAudio != null) buildAudio.Play();

        float timer = 0f;
        Vector3 startPos = realModel.transform.position;
        Vector3 bounceScale = originalScale * upgradeBounceAmount;

        while (timer < buildDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / buildDuration;

            if (currentLevel == 1)
            {
                realModel.transform.position = Vector3.Lerp(startPos, finalPos, Mathf.SmoothStep(0f, 1f, progress));
            }
            else
            {
                float scaleCurve = Mathf.PingPong(progress * 2f, 1f);
                realModel.transform.localScale = Vector3.Lerp(originalScale, bounceScale, Mathf.SmoothStep(0f, 1f, scaleCurve));
            }

            yield return null;
        }

        realModel.transform.position = finalPos;
        realModel.transform.localScale = originalScale;

        StopDustEffect();

        ApplyBuildingEffects();
        UpdateGlimmerState();

        if (playerInRange && currentLevel < levels.Length)
        {
            UpdateUIData();
            uiCanvas.SetActive(true);
            StartCoroutine(PopUpUI());
        }

        isAnimating = false;
    }

    // --- ЛОГІКА ВІЗУАЛЬНИХ РЕСУРСІВ (НОВЕ) ---
    public void ShowNextVisualResource()
    {
        if (resourceVisuals == null || resourceVisuals.Length == 0) return;

        if (currentVisualIndex < resourceVisuals.Length)
        {
            if (resourceVisuals[currentVisualIndex] != null)
            {
                resourceVisuals[currentVisualIndex].SetActive(true);
            }
            currentVisualIndex++;
        }
    }

    private void HideAllVisualResources()
    {
        currentVisualIndex = 0;
        if (resourceVisuals == null) return;
        foreach (var vis in resourceVisuals)
        {
            if (vis != null) vis.SetActive(false);
        }
    }

    private void ApplyBuildingEffects()
    {
        BuildingLevel currentData = levels[currentLevel - 1];
        if (isStorageVault && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetExtraCapacity(currentData.productionValue);
        }
        else if (!isStorageVault)
        {
            StopAllCoroutines();
            StartCoroutine(ProductionRoutine(currentData.productionValue));
        }
    }

    private IEnumerator ProductionRoutine(int amountPerMinute)
    {
        if (amountPerMinute <= 0) yield break;
        while (true)
        {
            yield return new WaitForSeconds(60f); // Кожні 60 секунд

            // 1. Нараховуємо ресурси
            if (buildingID.Contains("Lumberjack")) ResourceManager.Instance.AddStashResources(amountPerMinute, 0, 0);
            else if (buildingID.Contains("Stone")) ResourceManager.Instance.AddStashResources(0, amountPerMinute, 0);
            else if (buildingID.Contains("Hunter")) ResourceManager.Instance.AddStashResources(0, 0, amountPerMinute);

            // 2. Ховаємо всі колоди/камені (ніби гравець їх забрав)
            HideAllVisualResources();

            // Тут можна додати якийсь ефект пилу чи звук "Ching!", щоб гравець зрозумів, що лут додано
        }
    }

    private void UpdateGlimmerState()
    {
        if (upgradeGlimmer != null)
        {
            bool canBeUpgraded = (currentLevel > 0 && currentLevel < levels.Length);
            bool hasResources = false;

            if (canBeUpgraded && ResourceManager.Instance != null)
            {
                BuildingLevel nextLevelData = levels[currentLevel];
                hasResources = ResourceManager.Instance.CanAffordStash(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);
            }

            upgradeGlimmer.SetActive(canBeUpgraded && hasResources);
        }
    }

    private void StartDustEffect()
    {
        if (buildDustVFX != null)
        {
            buildDustVFX.gameObject.SetActive(true);
            if (!buildDustVFX.isPlaying) buildDustVFX.Play();
        }
    }

    private void StopDustEffect()
    {
        if (buildDustVFX != null)
        {
            buildDustVFX.Stop();
            buildDustVFX.gameObject.SetActive(false);
        }
    }
}