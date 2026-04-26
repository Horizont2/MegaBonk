using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MetaUpgradeSlot : MonoBehaviour
{
    [Header("Upgrade Settings")]
    public string upgradeID;      // Unique ID (e.g., "MetaHealth", "MetaDamage")
    public int maxLevel = 10;     // Maximum allowed level
    public int baseCost = 50;     // Cost for level 1
    public float costMultiplier = 1.5f; // Each level costs 50% more

    [Header("UI References")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public Slider progressBar;
    public Button buyButton;

    private MainMenuManager menuManager;

    private void Start()
    {
        menuManager = FindFirstObjectByType<MainMenuManager>();

        // Assign the click event to the button automatically
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(AttemptPurchase);
        }

        UpdateUI(); // Refresh visuals on start
    }

    public void UpdateUI()
    {
        int currentLevel = SaveManager.GetUpgradeLevel(upgradeID);

        // 1. Update text and progress bar
        if (levelText != null) levelText.text = $"Lvl {currentLevel}/{maxLevel}";
        if (progressBar != null)
        {
            progressBar.maxValue = maxLevel;
            progressBar.value = currentLevel;
        }

        // 2. Update cost and button state
        if (currentLevel >= maxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (buyButton != null) buyButton.interactable = false; // Disable button
        }
        else
        {
            int currentCost = CalculateCost(currentLevel);
            if (costText != null) costText.text = currentCost.ToString();

            // Check if player has enough crystals to buy this
            bool canAfford = SaveManager.GetTotalCrystals() >= currentCost;
            if (buyButton != null) buyButton.interactable = canAfford;
        }
    }

    // Calculates exponential cost 
    private int CalculateCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
    }

    private void AttemptPurchase()
    {
        int currentLevel = SaveManager.GetUpgradeLevel(upgradeID);
        if (currentLevel >= maxLevel) return;

        int cost = CalculateCost(currentLevel);

        // Try to spend crystals via SaveManager
        if (SaveManager.SpendCrystals(cost))
        {
            // Level up!
            SaveManager.SetUpgradeLevel(upgradeID, currentLevel + 1);

            // Update the global crystals text on screen
            if (menuManager != null) menuManager.UpdateCrystalsUI();

            // Refresh ALL upgrade slots on screen (in case player can no longer afford others)
            MetaUpgradeSlot[] allSlots = FindObjectsByType<MetaUpgradeSlot>(FindObjectsSortMode.None);
            foreach (MetaUpgradeSlot slot in allSlots)
            {
                slot.UpdateUI();
            }
        }
    }
}