using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI overlay for the shop scene. All references assigned in Inspector.
/// ShopManager calls UpdateDisplay() when the selection changes.
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    [Header("Top Right - Crystal Balance")]
    public TextMeshProUGUI crystalBalanceText;
    public Image crystalIcon;

    [Header("Left Panel - Item Info")]
    public CanvasGroup infoPanelGroup;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI passiveText;

    [Header("Stats Bars")]
    public ShopStatBar hpBar;
    public ShopStatBar speedBar;
    public ShopStatBar radiusBar;
    public ShopStatBar damageBar;

    [Header("Bottom Center - Buy Button")]
    public Button buyButton;
    public Image buyButtonImage;
    public TextMeshProUGUI buyButtonText;
    public RectTransform buyButtonRect;

    [Header("Button Colors")]
    public Color buyColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color selectColor = new Color(0.3f, 0.85f, 0.3f, 1f);
    public Color equippedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color cantAffordColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    [Header("Navigation Arrows")]
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Navigation Dots (optional)")]
    public TextMeshProUGUI pageIndicator;

    [Header("Back Button")]
    public Button backButton;

    [Header("Animation")]
    public float panelFadeSpeed = 8f;
    public float statBarAnimSpeed = 4f;

    private ShopManager shopManager;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(() => shopManager?.NavigatePrev());
        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(() => shopManager?.NavigateNext());
        if (buyButton != null)
            buyButton.onClick.AddListener(() => shopManager?.TryBuyOrEquip());
        if (backButton != null)
            backButton.onClick.AddListener(() => shopManager?.GoBack());
    }

    public void UpdateDisplay(ShopManager manager)
    {
        ShopItemData item = manager.GetCurrentItem();
        if (item == null) return;

        // Crystal balance
        if (crystalBalanceText != null)
            crystalBalanceText.text = SaveManager.GetTotalCrystals().ToString("N0");

        // Item info
        if (itemNameText != null)
            itemNameText.text = item.displayName.ToUpper();
        if (itemDescriptionText != null)
            itemDescriptionText.text = item.description;
        if (passiveText != null)
            passiveText.text = item.passiveDescription;

        // Stats (depends on item type)
        UpdateStatBars(item);

        // Buy button state
        UpdateBuyButton(item, manager.GetItemState(item));

        // Page indicator
        if (pageIndicator != null)
            pageIndicator.text = (manager.GetCurrentIndex() + 1) + " / " + manager.GetTotalCount();

        // Fade in info panel
        StartCoroutine(FadeInPanel());
    }

    private void UpdateStatBars(ShopItemData item)
    {
        if (item.itemType == ShopItemType.Character)
        {
            SetStatBar(hpBar, "HP", item.characterStats.maxHP, 1000f,
                item.characterStats.maxHP.ToString("F0") + "/" + 1000);
            SetStatBar(speedBar, "SPEED", item.characterStats.moveSpeed, 15f,
                item.characterStats.moveSpeed.ToString("F1") + " m/s");
            SetStatBar(radiusBar, "RADIUS", item.characterStats.pickupRadius, 12f,
                item.characterStats.pickupRadius.ToString("F0") + "m");
            SetStatBar(damageBar, "DAMAGE", item.characterStats.damageMultiplier, 2f,
                (item.characterStats.damageMultiplier * 100f).ToString("F0") + "%");

            ShowStatBars(true);
        }
        else if (item.itemType == ShopItemType.Weapon)
        {
            SetStatBar(hpBar, "DAMAGE", item.weaponStats.damage, 100f,
                item.weaponStats.damage.ToString("F0"));
            SetStatBar(speedBar, "SPEED", item.weaponStats.attackSpeed, 3f,
                item.weaponStats.attackSpeed.ToString("F1") + "x");
            SetStatBar(radiusBar, "RANGE", item.weaponStats.range, 10f,
                item.weaponStats.range.ToString("F1") + "m");
            SetStatBar(damageBar, "KNOCKBACK", item.weaponStats.knockback, 10f,
                item.weaponStats.knockback.ToString("F1"));

            ShowStatBars(true);
        }
        else if (item.itemType == ShopItemType.Grenade)
        {
            SetStatBar(hpBar, "DAMAGE", item.grenadeStats.damage, 500f,
                item.grenadeStats.damage.ToString("F0"));
            SetStatBar(speedBar, "RADIUS", item.grenadeStats.explosionRadius, 15f,
                item.grenadeStats.explosionRadius.ToString("F1") + "m");
            SetStatBar(radiusBar, "COOLDOWN", item.grenadeStats.cooldown, 15f,
                item.grenadeStats.cooldown.ToString("F1") + "s");
            SetStatBar(damageBar, "DELAY", item.grenadeStats.delay, 5f,
                item.grenadeStats.delay.ToString("F1") + "s");

            ShowStatBars(true);
        }
        else
        {
            ShowStatBars(false);
        }
    }

    private void SetStatBar(ShopStatBar bar, string label, float value, float maxValue, string display)
    {
        if (bar.barRoot == null) return;

        bar.barRoot.SetActive(true);
        if (bar.labelText != null) bar.labelText.text = label;
        if (bar.valueText != null) bar.valueText.text = display;
        if (bar.fillImage != null)
        {
            float targetFill = Mathf.Clamp01(value / maxValue);
            StartCoroutine(AnimateBar(bar.fillImage, targetFill));
        }
        if (bar.percentText != null)
            bar.percentText.text = Mathf.RoundToInt(Mathf.Clamp01(value / maxValue) * 100f) + "%";
    }

    private void ShowStatBars(bool show)
    {
        if (hpBar.barRoot != null) hpBar.barRoot.SetActive(show);
        if (speedBar.barRoot != null) speedBar.barRoot.SetActive(show);
        if (radiusBar.barRoot != null) radiusBar.barRoot.SetActive(show);
        if (damageBar.barRoot != null) damageBar.barRoot.SetActive(show);
    }

    private IEnumerator AnimateBar(Image fillImage, float targetFill)
    {
        float current = fillImage.fillAmount;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            fillImage.fillAmount = Mathf.Lerp(current, targetFill, t / 0.4f);
            yield return null;
        }
        fillImage.fillAmount = targetFill;
    }

    private void UpdateBuyButton(ShopItemData item, ItemPurchaseState state)
    {
        if (buyButton == null) return;

        switch (state)
        {
            case ItemPurchaseState.Locked:
                bool canAfford = SaveManager.GetTotalCrystals() >= item.price;
                buyButtonText.text = "BUY - " + item.price + " <sprite=0>";
                buyButtonImage.color = canAfford ? buyColor : cantAffordColor;
                buyButton.interactable = true;
                break;

            case ItemPurchaseState.Owned:
                buyButtonText.text = "SELECT";
                buyButtonImage.color = selectColor;
                buyButton.interactable = true;
                break;

            case ItemPurchaseState.Equipped:
                buyButtonText.text = "EQUIPPED";
                buyButtonImage.color = equippedColor;
                buyButton.interactable = false;
                break;
        }
    }

    private IEnumerator FadeInPanel()
    {
        if (infoPanelGroup == null) yield break;

        infoPanelGroup.alpha = 0.3f;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            infoPanelGroup.alpha = Mathf.Lerp(0.3f, 1f, t / 0.2f);
            yield return null;
        }
        infoPanelGroup.alpha = 1f;
    }

    public void ShakeBuyButton()
    {
        if (buyButtonRect == null) return;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShake());
    }

    private IEnumerator DoShake()
    {
        Vector2 origin = buyButtonRect.anchoredPosition;

        // Flash red
        Color originalColor = buyButtonImage.color;
        buyButtonImage.color = cantAffordColor;

        float elapsed = 0f;
        float duration = 0.4f;
        float magnitude = 12f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude * (1f - elapsed / duration);
            buyButtonRect.anchoredPosition = origin + new Vector2(x, 0f);
            yield return null;
        }

        buyButtonRect.anchoredPosition = origin;
        buyButtonImage.color = originalColor;
    }
}

[System.Serializable]
public class ShopStatBar
{
    [Tooltip("Root GameObject of this stat bar row")]
    public GameObject barRoot;
    [Tooltip("Label text (e.g. 'HP', 'SPEED')")]
    public TextMeshProUGUI labelText;
    [Tooltip("Value display (e.g. '750/1000')")]
    public TextMeshProUGUI valueText;
    [Tooltip("Fill bar Image (Image Type: Filled)")]
    public Image fillImage;
    [Tooltip("Percentage text (e.g. '75%')")]
    public TextMeshProUGUI percentText;
}
