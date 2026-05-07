using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MapPanelUI : MonoBehaviour
{
    [Header("UI Elements - Text")]
    public TextMeshProUGUI regionNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recommendedPowerText;
    public TextMeshProUGUI playerPowerText;

    [Header("UI Elements - One-Time Rewards (Conquer)")]
    public GameObject conquerTitleObj;
    public GameObject conquerContainerObj;
    public TextMeshProUGUI conquerWoodText;
    public TextMeshProUGUI conquerStoneText;
    public TextMeshProUGUI conquerFoodText;
    public TextMeshProUGUI conquerDiamondText;

    [Header("UI Elements - Passive Rewards")]
    public TextMeshProUGUI passiveWoodText;
    public TextMeshProUGUI passiveStoneText;
    public TextMeshProUGUI passiveFoodText;
    public TextMeshProUGUI passiveDiamondText;

    [Header("UI Elements - Graphics & Buttons")]
    public Image illustrationImage;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button closeButton;

    [Header("Region Upgrade UI")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    public GameObject upgradeTitleObj;
    public GameObject upgradeCostRoot;
    public TextMeshProUGUI upgWoodCostText;
    public TextMeshProUGUI upgStoneCostText;
    public TextMeshProUGUI upgFoodCostText;
    public TextMeshProUGUI upgMaxLevelText;

    [Header("AAA Juice Effects")]
    public ParticleSystem upgradeSuccessVFX;
    public float shakeIntensity = 8f;
    public float shakeDuration = 0.2f;

    [Header("Animation Settings")]
    public RectTransform panelRect;
    public float animationDuration = 0.3f;
    public float startOffset = 200f;
    [Tooltip("Відстань зупинки панелі від краю екрана")]
    public float edgePadding = 15f;
    private CanvasGroup canvasGroup;

    private RegionData currentRegion;
    private Coroutine animationCoroutine;
    private bool isConfirmingUpgrade = false;

    public RegionData GetCurrentRegion() { return currentRegion; }
    public bool IsPanelOpen() { return canvasGroup.interactable; }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
    }

    public void OpenPanel(RegionData region, Vector3 regionWorldPosition)
    {
        isConfirmingUpgrade = false;
        currentRegion = region;
        PopulateData();

        Vector3 viewportPos = Camera.main.WorldToViewportPoint(regionWorldPosition);
        bool putPanelOnRight = viewportPos.x < 0.5f;

        Vector2 hiddenPos, visiblePos;

        // Зберігаємо оригінальний розмір панелі, щоб уникнути спотворень Layout'у
        Vector2 cachedSize = panelRect.rect.size;

        if (putPanelOnRight)
        {
            panelRect.anchorMin = new Vector2(1, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            visiblePos = new Vector2(-edgePadding, 0);
            // Замість 50f використовуємо startOffset
            hiddenPos = new Vector2(cachedSize.x + startOffset, 0);
        }
        else
        {
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            visiblePos = new Vector2(edgePadding, 0);
            // Замість 50f використовуємо startOffset
            hiddenPos = new Vector2(-cachedSize.x - startOffset, 0);
        }

        // Відновлюємо розмір перед появою
        panelRect.sizeDelta = cachedSize;
        panelRect.anchoredPosition = hiddenPos;

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(visiblePos, 1f, true));
    }

    public void ClosePanel()
    {
        if (currentRegion == null) return;

        float width = panelRect.rect.width;
        // Додаємо startOffset замість 50f
        Vector2 hiddenPos = panelRect.pivot.x > 0.5f
            ? new Vector2(width + startOffset, 0)
            : new Vector2(-width - startOffset, 0);

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(hiddenPos, 0f, false));
    }

    private void PopulateData()
    {
        if (regionNameText != null) regionNameText.text = currentRegion.regionName.ToUpper();
        if (descriptionText != null) descriptionText.text = currentRegion.loreDescription;
        if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";

        if (illustrationImage != null)
        {
            if (currentRegion.regionIllustration != null)
            {
                illustrationImage.sprite = currentRegion.regionIllustration;
                illustrationImage.gameObject.SetActive(true);
            }
            else illustrationImage.gameObject.SetActive(false);
        }

        int currentLevel = PlayerPrefs.GetInt("RegionLevel_" + currentRegion.regionID, 1);
        if (currentRegion.upgradeLevels == null || currentRegion.upgradeLevels.Length == 0) return;

        RegionLevelData levelData = currentRegion.upgradeLevels[currentLevel - 1];

        if (passiveWoodText != null) passiveWoodText.text = $"+{levelData.passiveWood}/hr";
        if (passiveStoneText != null) passiveStoneText.text = $"+{levelData.passiveStone}/hr";
        if (passiveFoodText != null) passiveFoodText.text = $"+{levelData.passiveFood}/hr";
        if (passiveDiamondText != null) passiveDiamondText.text = $"+{levelData.passiveDiamonds}/hr";

        switch (currentRegion.currentState)
        {
            case RegionState.Locked:
            case RegionState.Available:
                if (conquerTitleObj) conquerTitleObj.SetActive(true);
                if (conquerContainerObj) conquerContainerObj.SetActive(true);

                if (upgradeTitleObj) upgradeTitleObj.SetActive(false);
                if (upgradeCostRoot) upgradeCostRoot.SetActive(false);
                if (upgMaxLevelText) upgMaxLevelText.gameObject.SetActive(false);
                if (upgradeButton) upgradeButton.gameObject.SetActive(false);

                if (actionButtonText != null) actionButtonText.text = currentRegion.currentState == RegionState.Locked ? "AREA LOCKED" : "START JOURNEY";
                if (actionButton != null) actionButton.interactable = currentRegion.currentState == RegionState.Available;
                break;

            case RegionState.Conquered:
                if (conquerTitleObj) conquerTitleObj.SetActive(false);
                if (conquerContainerObj) conquerContainerObj.SetActive(false);

                if (actionButtonText != null) actionButtonText.text = "TRAVEL";
                if (actionButton != null) actionButton.interactable = true;
                if (upgradeButton) upgradeButton.gameObject.SetActive(true);

                UpdateUpgradeUI(currentLevel);
                break;
        }

        if (conquerWoodText != null) conquerWoodText.text = $"+{currentRegion.woodReward}";
        if (conquerStoneText != null) conquerStoneText.text = $"+{currentRegion.stoneReward}";
        if (conquerFoodText != null) conquerFoodText.text = $"+{currentRegion.foodReward}";
        if (conquerDiamondText != null) conquerDiamondText.text = $"+{currentRegion.diamondReward}";

        int currentPlayerPower = PlayerPrefs.GetInt("PlayerTotalPower", 50);
        string powerColor = (currentPlayerPower >= currentRegion.recommendedPower) ? "#4CAF50" : "#F44336";
        if (recommendedPowerText != null) recommendedPowerText.text = $"<size=50%><color=#D4AF37>RECOMMENDED</color></size>\n{currentRegion.recommendedPower}";
        if (playerPowerText != null) playerPowerText.text = $"<size=50%><color=#D4AF37>YOUR POWER</color></size>\n<color={powerColor}>{currentPlayerPower}</color>";
    }

    private void UpdateUpgradeUI(int currentLevel)
    {
        if (currentLevel < 5)
        {
            if (upgradeTitleObj) upgradeTitleObj.SetActive(true);
            if (upgradeCostRoot) upgradeCostRoot.SetActive(true);
            if (upgMaxLevelText) upgMaxLevelText.gameObject.SetActive(false);

            RegionLevelData nextLevelData = currentRegion.upgradeLevels[currentLevel];

            if (upgWoodCostText) upgWoodCostText.text = nextLevelData.costWood.ToString();
            if (upgStoneCostText) upgStoneCostText.text = nextLevelData.costStone.ToString();
            if (upgFoodCostText) upgFoodCostText.text = nextLevelData.costFood.ToString();

            bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.CanAffordStash(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);
            if (upgradeButton) upgradeButton.interactable = canAfford;

            if (ResourceManager.Instance != null)
            {
                if (upgWoodCostText) upgWoodCostText.color = ResourceManager.Instance.stashWood >= nextLevelData.costWood ? Color.white : Color.red;
                if (upgStoneCostText) upgStoneCostText.color = ResourceManager.Instance.stashStone >= nextLevelData.costStone ? Color.white : Color.red;
                if (upgFoodCostText) upgFoodCostText.color = ResourceManager.Instance.stashFood >= nextLevelData.costFood ? Color.white : Color.red;
            }
        }
        else
        {
            if (upgradeTitleObj) upgradeTitleObj.SetActive(false);
            if (upgradeCostRoot) upgradeCostRoot.SetActive(false);
            if (upgMaxLevelText)
            {
                upgMaxLevelText.gameObject.SetActive(true);
                upgMaxLevelText.text = "MAX LEVEL REACHED";
            }
            if (upgradeButton) upgradeButton.interactable = false;
        }
    }

    private void OnUpgradeButtonClicked()
    {
        int currentLevel = PlayerPrefs.GetInt("RegionLevel_" + currentRegion.regionID, 1);

        if (currentLevel < 5 && ResourceManager.Instance != null)
        {
            RegionLevelData nextLevelData = currentRegion.upgradeLevels[currentLevel];

            if (ResourceManager.Instance.CanAffordStash(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood))
            {
                if (!isConfirmingUpgrade)
                {
                    isConfirmingUpgrade = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

                    if (upgradeButtonText != null) upgradeButtonText.text = "<color=#FFD700>CONFIRM</color>";
                    ShowUpgradePreview(currentLevel);
                }
                else
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_BuildDone);
                    if (upgradeSuccessVFX != null) upgradeSuccessVFX.Play();
                    StartCoroutine(ShakePanel());

                    ResourceManager.Instance.SpendStashResources(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);
                    PlayerPrefs.SetInt("RegionLevel_" + currentRegion.regionID, currentLevel + 1);
                    PlayerPrefs.Save();

                    isConfirmingUpgrade = false;
                    PopulateData();

                    if (MapProgressionManager.Instance != null) MapProgressionManager.Instance.RefreshMapState();
                }
            }
            else
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Error);
            }
        }
    }

    private void ShowUpgradePreview(int currentLevel)
    {
        RegionLevelData currentData = currentRegion.upgradeLevels[currentLevel - 1];
        RegionLevelData nextData = currentRegion.upgradeLevels[currentLevel];

        if (passiveWoodText) passiveWoodText.text = $"+{currentData.passiveWood} <color=#00FF00>→ {nextData.passiveWood}</color>/hr";
        if (passiveStoneText) passiveStoneText.text = $"+{currentData.passiveStone} <color=#00FF00>→ {nextData.passiveStone}</color>/hr";
        if (passiveFoodText) passiveFoodText.text = $"+{currentData.passiveFood} <color=#00FF00>→ {nextData.passiveFood}</color>/hr";
        if (passiveDiamondText) passiveDiamondText.text = $"+{currentData.passiveDiamonds} <color=#00FF00>→ {nextData.passiveDiamonds}</color>/hr";
    }

    private IEnumerator ShakePanel()
    {
        Vector3 originalPos = panelRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            panelRect.anchoredPosition = new Vector2(originalPos.x + x, originalPos.y + y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panelRect.anchoredPosition = originalPos;
    }

    private void OnActionButtonClicked()
    {
        if (currentRegion.currentState == RegionState.Available || currentRegion.currentState == RegionState.Conquered)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
            if (GameManager.Instance != null) GameManager.Instance.currentRegion = currentRegion;

            PlayerPrefs.SetInt("IsRegionMission", 1);
            PlayerPrefs.SetInt("RegionBiomeType", (int)currentRegion.regionBiome);
            PlayerPrefs.SetInt("IsRunActive", 1);
            PlayerPrefs.SetInt("IsContinuing", 0);
            PlayerPrefs.Save();

            ClosePanel();
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene("GameScene");
        }
    }

    private IEnumerator AnimatePanel(Vector2 targetPos, float targetAlpha, bool state)
    {
        if (state) { canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true; }

        float elapsed = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float smoothT = 1f - Mathf.Pow(1f - (elapsed / animationDuration), 3f);
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            yield return null;
        }

        panelRect.anchoredPosition = targetPos;
        canvasGroup.alpha = targetAlpha;
        if (!state) { canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; }
    }
}