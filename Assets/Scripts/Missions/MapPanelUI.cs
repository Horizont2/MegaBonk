using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MapPanelUI : MonoBehaviour
{
    [Header("UI Elements - Text")]
    public TextMeshProUGUI regionNameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recommendedPowerText;
    public TextMeshProUGUI playerPowerText;

    [Header("UI Elements - One-Time Rewards")]
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

    [Header("Animation Settings")]
    public RectTransform panelRect;
    public float animationDuration = 0.3f;
    private CanvasGroup canvasGroup;

    private RegionData currentRegion;
    private Coroutine animationCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    public void OpenPanel(RegionData region, Vector3 regionWorldPosition)
    {
        currentRegion = region;
        PopulateData();

        Vector3 viewportPos = Camera.main.WorldToViewportPoint(regionWorldPosition);
        bool putPanelOnRight = viewportPos.x < 0.5f;

        Vector2 hiddenPos, visiblePos;
        float paddingX = 40f;

        if (putPanelOnRight)
        {
            panelRect.anchorMin = new Vector2(1, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            visiblePos = new Vector2(-paddingX, 0);
            hiddenPos = new Vector2(panelRect.rect.width + 50f, 0);
        }
        else
        {
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            visiblePos = new Vector2(paddingX, 0);
            hiddenPos = new Vector2(-panelRect.rect.width - 50f, 0);
        }

        panelRect.anchoredPosition = hiddenPos;

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(visiblePos, 1f, true));
    }

    public void ClosePanel()
    {
        if (currentRegion == null) return;
        Vector2 hiddenPos = panelRect.pivot.x > 0.5f
            ? new Vector2(panelRect.rect.width + 50f, 0)
            : new Vector2(-panelRect.rect.width - 50f, 0);

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(hiddenPos, 0f, false));
    }

    private void PopulateData()
    {
        if (regionNameText != null) regionNameText.text = currentRegion.regionName.ToUpper();
        if (descriptionText != null) descriptionText.text = currentRegion.loreDescription;

        if (illustrationImage != null)
        {
            if (currentRegion.regionIllustration != null)
            {
                illustrationImage.sprite = currentRegion.regionIllustration;
                illustrationImage.gameObject.SetActive(true);
            }
            else illustrationImage.gameObject.SetActive(false);
        }

        switch (currentRegion.currentState)
        {
            case RegionState.Locked:
                if (actionButtonText != null) actionButtonText.text = "AREA LOCKED";
                if (actionButton != null) actionButton.interactable = false;
                break;
            case RegionState.Available:
                if (actionButtonText != null) actionButtonText.text = "START JOURNEY";
                if (actionButton != null) actionButton.interactable = true;
                break;
            case RegionState.Conquered:
                if (actionButtonText != null) actionButtonText.text = "TRAVEL (SAFE)";
                if (actionButton != null) actionButton.interactable = true;
                break;
        }

        int currentPlayerPower = PlayerPrefs.GetInt("PlayerTotalPower", 50);
        string powerColor = (currentPlayerPower >= currentRegion.recommendedPower) ? "#4CAF50" : "#F44336";

        if (recommendedPowerText != null) recommendedPowerText.text = $"<size=50%><color=#D4AF37>RECOMMENDED</color></size>\n{currentRegion.recommendedPower}";
        if (playerPowerText != null) playerPowerText.text = $"<size=50%><color=#D4AF37>YOUR POWER</color></size>\n<color={powerColor}>{currentPlayerPower}</color>";

        if (conquerWoodText != null) conquerWoodText.text = $"+{currentRegion.woodReward}";
        if (conquerStoneText != null) conquerStoneText.text = $"+{currentRegion.stoneReward}";
        if (conquerFoodText != null) conquerFoodText.text = $"+{currentRegion.foodReward}";
        if (conquerDiamondText != null) conquerDiamondText.text = $"+{currentRegion.diamondReward}";

        if (passiveWoodText != null) passiveWoodText.text = $"+{currentRegion.passiveWood}/hr";
        if (passiveStoneText != null) passiveStoneText.text = $"+{currentRegion.passiveStone}/hr";
        if (passiveFoodText != null) passiveFoodText.text = $"+{currentRegion.passiveFood}/hr";
        if (passiveDiamondText != null) passiveDiamondText.text = $"+{currentRegion.passiveDiamonds}/hr";
    }

    private void OnActionButtonClicked()
    {
        if (currentRegion.currentState == RegionState.Available || currentRegion.currentState == RegionState.Conquered)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

            if (GameManager.Instance != null) GameManager.Instance.currentRegion = currentRegion;

            // --- ФІКС: Залізобетонно зберігаємо тип місії та біом ---
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