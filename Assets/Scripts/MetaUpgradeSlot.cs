using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// A single meta upgrade slot in the main menu.
/// Set up in the Inspector: assign the icon, texts, fill image, and button.
/// </summary>
public class MetaUpgradeSlot : MonoBehaviour
{
    [Header("Upgrade Settings")]
    public string upgradeID;
    public int maxLevel = 10;
    public int baseCost = 50;
    public float costMultiplier = 1.5f;

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public Image fillBar;
    public Button buyButton;

    [Header("Feedback")]
    public float punchScale = 0.9f;
    public float punchDuration = 0.15f;

    private MainMenuManager menuManager;
    private Vector3 originalScale;

    private void Start()
    {
        menuManager = FindObjectOfType<MainMenuManager>();
        originalScale = transform.localScale;

        if (buyButton != null)
            buyButton.onClick.AddListener(AttemptPurchase);

        UpdateUI();
    }

    public void UpdateUI()
    {
        int currentLevel = SaveManager.GetUpgradeLevel(upgradeID);

        if (levelText != null)
            levelText.text = currentLevel + "/" + maxLevel;

        // Fill bar (Image.fillAmount 0..1)
        if (fillBar != null)
            fillBar.fillAmount = (float)currentLevel / maxLevel;

        if (currentLevel >= maxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (buyButton != null) buyButton.interactable = false;
        }
        else
        {
            int cost = CalculateCost(currentLevel);
            if (costText != null) costText.text = cost.ToString();

            bool canAfford = SaveManager.GetTotalCrystals() >= cost;
            if (buyButton != null) buyButton.interactable = canAfford;
        }
    }

    private int CalculateCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
    }

    private void AttemptPurchase()
    {
        int currentLevel = SaveManager.GetUpgradeLevel(upgradeID);
        if (currentLevel >= maxLevel) return;

        int cost = CalculateCost(currentLevel);

        if (SaveManager.SpendCrystals(cost))
        {
            SaveManager.SetUpgradeLevel(upgradeID, currentLevel + 1);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("buttonClick");

            if (menuManager != null) menuManager.UpdateCrystalsUI();

            // Refresh all slots
            MetaUpgradeSlot[] allSlots = FindObjectsOfType<MetaUpgradeSlot>();
            foreach (MetaUpgradeSlot slot in allSlots)
                slot.UpdateUI();

            StartCoroutine(PunchAnimation());
        }
    }

    private IEnumerator PunchAnimation()
    {
        float t = 0f;
        float halfDur = punchDuration * 0.5f;

        // Shrink
        while (t < halfDur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / halfDur;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, p);
            yield return null;
        }

        // Bounce back
        t = 0f;
        while (t < halfDur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / halfDur;
            transform.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, p);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
