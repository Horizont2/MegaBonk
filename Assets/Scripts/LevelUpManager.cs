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
    public string statDisplay; // ДОДАНО: Текст типу "+20" або "+15%"
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

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        hammer = FindObjectOfType<HammerDamage>();
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
        // Робимо тимчасову копію нашого списку, щоб вибирати унікальні варіанти
        List<UpgradeData> availablePool = new List<UpgradeData>(allPossibleUpgrades);

        for (int i = 0; i < uiButtons.Length; i++)
        {
            // Якщо покращення в базі закінчилися (наприклад, їх всього 2), виходимо
            if (availablePool.Count == 0) break;

            // Вибираємо випадковий індекс
            int randomIndex = Random.Range(0, availablePool.Count);
            UpgradeData chosenUpgrade = availablePool[randomIndex];

            // Налаштовуємо UI кнопки
            uiButtons[i].titleText.text = chosenUpgrade.upgradeName;
            uiButtons[i].descriptionText.text = chosenUpgrade.description;
            uiButtons[i].statText.text = chosenUpgrade.statDisplay;
            if (chosenUpgrade.icon != null) uiButtons[i].iconImage.sprite = chosenUpgrade.icon;

            // Очищаємо старі команди кнопки і додаємо нову
            uiButtons[i].buttonComponent.onClick.RemoveAllListeners();
            uiButtons[i].buttonComponent.onClick.AddListener(() => ApplyUpgrade(chosenUpgrade));

            // Видаляємо вибране покращення з пулу, щоб воно не випало двічі за один раз
            availablePool.RemoveAt(randomIndex);
        }
    }

    // Цей метод викликається, коли гравець натискає на кнопку
    public void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.Health:
                player.maxHealth += upgrade.amount;
                player.currentHealth = player.maxHealth; // Лікуємо
                break;
            case UpgradeType.Speed:
                player.moveSpeed += upgrade.amount;
                break;
            case UpgradeType.Damage:
                if (hammer != null) hammer.damage += upgrade.amount;
                break;
            case UpgradeType.PickupRadius:
                // Ми додамо цей функціонал кристалам пізніше!
                Debug.Log("Pickup Radius upgraded!");
                break;
        }

        ResumeGame();
    }

    private void ResumeGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
    }
}