using UnityEngine;
using System.Collections.Generic;

public enum UpgradeType
{
    Health,
    Speed,
    Damage,
    PickupRadius,
    AttackSpeed,
    Armor,       // На майбутнє
    HealthRegen  // На майбутнє
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
    private WeaponOrbit weaponOrbit; // ДОДАНО: Для управління швидкістю обертання молота

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        hammer = FindObjectOfType<HammerDamage>();
        weaponOrbit = FindObjectOfType<WeaponOrbit>();
        levelUpPanel.SetActive(false);
    }

    public void ShowMenu()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;

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

            // ВАЖЛИВО: Робимо локальну копію, щоб уникнути багу перекриття подій у кнопках (Closure Bug)
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
                if (hammer != null) hammer.damage += upgrade.amount;
                break;
            case UpgradeType.PickupRadius:
                if (player != null) player.pickupRadius += upgrade.amount;
                break;
            case UpgradeType.AttackSpeed:
                // Збільшуємо швидкість обертання молота!
                if (weaponOrbit != null) weaponOrbit.rotationSpeed += upgrade.amount;
                break;
            case UpgradeType.HealthRegen:
                if (player != null) player.healthRegenRate += upgrade.amount;
                break;
        }

        // Оновлюємо інтерфейс гравця одразу після апгрейду (щоб нове ХП одразу відмалювалося)
        if (player != null) player.UpdateHUD();

        ResumeGame();
    }

    private void ResumeGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
    }
}