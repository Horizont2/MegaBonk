using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MapPanelUI : MonoBehaviour
{
    [Header("UI Components")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI regionNameText;
    public TextMeshProUGUI loreText;
    public TextMeshProUGUI powerLevelText;
    public Image regionIconImage;

    [Header("One-Time Rewards UI")]
    public TextMeshProUGUI woodRewardText;
    public TextMeshProUGUI stoneRewardText;
    public TextMeshProUGUI foodRewardText;
    public TextMeshProUGUI diamondRewardText;

    [Header("Passive Income UI")]
    [Tooltip("Одне текстове поле, куди скрипт сам гарно впише всі ресурси")]
    public TextMeshProUGUI passiveIncomeText;

    [Header("Action Button")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    private RegionData currentRegion;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        ClosePanel(true); // Ховаємо панель миттєво при старті сцени
    }

    public void OpenPanel(RegionData region)
    {
        currentRegion = region;

        // 1. Заповнюємо базову інформацію
        if (regionNameText != null) regionNameText.text = region.regionName;
        if (loreText != null) loreText.text = region.loreDescription;
        if (powerLevelText != null) powerLevelText.text = "Rec. Power: " + region.recommendedPowerLevel.ToString();

        if (regionIconImage != null)
        {
            if (region.regionIcon != null)
            {
                regionIconImage.sprite = region.regionIcon;
                regionIconImage.gameObject.SetActive(true);
            }
            else
            {
                regionIconImage.gameObject.SetActive(false); // Ховаємо іконку, якщо її немає
            }
        }

        // 2. Заповнюємо одноразові нагороди
        if (woodRewardText != null) woodRewardText.text = region.woodReward.ToString();
        if (stoneRewardText != null) stoneRewardText.text = region.stoneReward.ToString();
        if (foodRewardText != null) foodRewardText.text = region.foodReward.ToString();
        if (diamondRewardText != null) diamondRewardText.text = region.diamondReward.ToString();

        // 3. Генеруємо красивий текст пасивного доходу
        if (passiveIncomeText != null)
        {
            string passiveText = "";

            if (region.passiveWood > 0) passiveText += $"+{region.passiveWood} Wood/min\n";
            if (region.passiveStone > 0) passiveText += $"+{region.passiveStone} Stone/min\n";
            if (region.passiveFood > 0) passiveText += $"+{region.passiveFood} Food/min\n";
            if (region.passiveDiamonds > 0) passiveText += $"+{region.passiveDiamonds} Diamonds/min\n";

            if (string.IsNullOrEmpty(passiveText)) passiveText = "None";

            // Видаляємо останній перенос рядка (TrimEnd), щоб текст стояв рівно
            passiveIncomeText.text = passiveText.TrimEnd('\n');
        }

        // 4. Логіка кнопки "В бій" (Змінюється залежно від стану регіону)
        if (actionButton != null && actionButtonText != null)
        {
            actionButton.onClick.RemoveAllListeners(); // Очищаємо попередні дії

            switch (region.currentState)
            {
                case RegionState.Locked:
                    actionButton.interactable = false;
                    actionButtonText.text = "Locked";
                    break;
                case RegionState.Available:
                    actionButton.interactable = true;
                    actionButtonText.text = "Start Journey";
                    actionButton.onClick.AddListener(StartJourney);
                    break;
                case RegionState.Conquered:
                    actionButton.interactable = false;
                    actionButtonText.text = "Conquered";
                    break;
            }
        }

        // 5. Анімація прояву
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(1f, 0.3f));
    }

    public void ClosePanel(bool instant = false)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (instant)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            fadeCoroutine = StartCoroutine(FadeCanvas(0f, 0.25f));
        }
    }

    private void StartJourney()
    {
        if (currentRegion == null) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        Debug.Log($"Starting journey to: {currentRegion.regionName}");

        // Тут буде ваш код для переходу на рівень. Наприклад:
        // GlobalHUD.Instance.FadeAndLoadScene("ProceduralLevelScene");
    }

    private IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        if (targetAlpha > 0.5f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // unscaledDeltaTime дозволяє малювати UI навіть на паузі
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha < 0.5f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}