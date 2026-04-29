using UnityEngine;
using System.Collections.Generic;

public enum UpgradeType
{
    Health,
    Speed,
    Damage,
    PickupRadius,
    AttackSpeed,
    Armor,
    HealthRegen
}

[System.Serializable]
public class UpgradeData
{
    public string upgradeName;
    [TextArea(2, 3)] public string description;
    public string statDisplay;
    public Sprite icon;
    public UpgradeType type;
    public float amount;
}

public class LevelUpManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelUpPanel;
    public UIStarEffect starEffect;
    public UpgradeButtonUI[] uiButtons;

    [Header("Database")]
    public List<UpgradeData> allPossibleUpgrades;

    private PlayerController player;
    private HammerDamage hammer;
    private WeaponOrbit weaponOrbit;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        hammer = FindFirstObjectByType<HammerDamage>();
        weaponOrbit = FindFirstObjectByType<WeaponOrbit>();
        levelUpPanel.SetActive(false);
    }

    public void ShowMenu()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (starEffect != null) starEffect.PlayEffect();

        GenerateRandomChoices();
    }

    private void GenerateRandomChoices()
    {
        List<UpgradeData> availablePool = new List<UpgradeData>(allPossibleUpgrades);

        for (int i = 0; i < uiButtons.Length; i++)
        {
            if (availablePool.Count == 0) break;

            int randomIndex = Random.Range(0, availablePool.Count);
            UpgradeData chosenUpgrade = availablePool[randomIndex];

            UpgradeData upgradeToApply = chosenUpgrade;

            uiButtons[i].titleText.text = upgradeToApply.upgradeName;
            uiButtons[i].descriptionText.text = upgradeToApply.description;
            uiButtons[i].statText.text = upgradeToApply.statDisplay;
            if (upgradeToApply.icon != null) uiButtons[i].iconImage.sprite = upgradeToApply.icon;

            uiButtons[i].buttonComponent.onClick.RemoveAllListeners();
            uiButtons[i].buttonComponent.onClick.AddListener(() => ApplyUpgrade(upgradeToApply));

            availablePool.RemoveAt(randomIndex);
        }
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        // ÇÂÓÊ: Âèá³ð àïãðåéäó
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        switch (upgrade.type)
        {
            case UpgradeType.Health:
                player.maxHealth += upgrade.amount;
                player.currentHealth = player.maxHealth;
                break;
            case UpgradeType.Speed:
                player.moveSpeed += upgrade.amount;
                break;
            case UpgradeType.Damage:
                if (hammer != null) hammer.baseDamage += upgrade.amount;
                break;
            case UpgradeType.PickupRadius:
                if (player != null) player.pickupRadius += upgrade.amount;
                break;
            case UpgradeType.AttackSpeed:
                if (weaponOrbit != null) weaponOrbit.rotationSpeed += upgrade.amount;
                break;
            case UpgradeType.HealthRegen:
                if (player != null) player.healthRegenRate += upgrade.amount;
                break;
        }

        if (player != null) player.UpdateHUD();

        ResumeGame();
    }

    private void ResumeGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}