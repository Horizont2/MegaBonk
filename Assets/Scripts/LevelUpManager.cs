using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum UpgradeType
{
    Health, Speed, Damage, PickupRadius, AttackSpeed, Armor, HealthRegen
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
    public Button randomButton; // НОВА КНОПКА RANDOM

    [Header("Database")]
    public List<UpgradeData> allPossibleUpgrades;

    private PlayerController player;
    private HammerDamage hammer;
    private WeaponOrbit weaponOrbit;

    // Зберігаємо поточні 3 варіанти, щоб Random знав, з чого вибирати
    private UpgradeData[] currentOptions = new UpgradeData[3];

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        hammer = FindFirstObjectByType<HammerDamage>();
        weaponOrbit = FindFirstObjectByType<WeaponOrbit>();
        levelUpPanel.SetActive(false);

        if (randomButton != null)
        {
            randomButton.onClick.AddListener(OnRandomClicked);
        }
    }

    public void ShowMenu()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f; // Час зупиняється

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (starEffect != null) starEffect.PlayEffect();

        if (randomButton != null) randomButton.interactable = true;

        GenerateRandomChoices();
    }

    private void GenerateRandomChoices()
    {
        List<UpgradeData> availablePool = new List<UpgradeData>(allPossibleUpgrades);

        for (int i = 0; i < uiButtons.Length; i++)
        {
            uiButtons[i].ResetVisuals();
            uiButtons[i].ResetTextColors();
            uiButtons[i].buttonComponent.interactable = true;

            if (availablePool.Count == 0) break;

            int randomIndex = Random.Range(0, availablePool.Count);
            UpgradeData chosenUpgrade = availablePool[randomIndex];
            currentOptions[i] = chosenUpgrade; // Зберігаємо для рулетки

            uiButtons[i].titleText.text = chosenUpgrade.upgradeName;

            // --- ФІКС: Об'єднуємо Опис + Стат з нового рядка і фарбуємо стат у золотий колір ---
            string finalDescription = chosenUpgrade.description;
            if (!string.IsNullOrEmpty(chosenUpgrade.statDisplay))
            {
                // \n робить абзац, а теги <color> змінюють колір тексту всередині TextMeshPro
                finalDescription += "\n<color=#FFD700><b>" + chosenUpgrade.statDisplay + "</b></color>";
            }
            uiButtons[i].descriptionText.text = finalDescription;
            // -----------------------------------------------------------------------------------

            if (chosenUpgrade.icon != null) uiButtons[i].iconImage.sprite = chosenUpgrade.icon;

            // Ховаємо окремий текстовий об'єкт для статів, якщо він ще залишився на префабі
            if (uiButtons[i].statText != null) uiButtons[i].statText.gameObject.SetActive(false);

            // Локальна копія змінної для лямбда-виразу
            UpgradeData upgradeToApply = chosenUpgrade;
            int buttonIndex = i;

            uiButtons[i].buttonComponent.onClick.RemoveAllListeners();
            uiButtons[i].buttonComponent.onClick.AddListener(() => OnStandardUpgradeClicked(upgradeToApply, buttonIndex));

            availablePool.RemoveAt(randomIndex);
        }
    }

    // Звичайний клік по картці
    private void OnStandardUpgradeClicked(UpgradeData upgrade, int buttonIndex)
    {
        BlockAllButtons();
        uiButtons[buttonIndex].HighlightAsSelected();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        StartCoroutine(ApplyWithDelayRoutine(upgrade));
    }

    // Клік по кнопці Random
    private void OnRandomClicked()
    {
        BlockAllButtons();
        StartCoroutine(RouletteRoutine());
    }

    private void BlockAllButtons()
    {
        if (randomButton != null) randomButton.interactable = false;
        foreach (var btn in uiButtons) btn.buttonComponent.interactable = false;
    }

    // Анімація рулетки!
    private IEnumerator RouletteRoutine()
    {
        // Кількість "стрибків" рулетки
        int jumps = Random.Range(10, 16);
        int currentIndex = 0;
        float delay = 0.05f; // Швидкий старт

        for (int i = 0; i < jumps; i++)
        {
            foreach (var btn in uiButtons) btn.ResetVisuals();

            currentIndex = i % uiButtons.Length;

            // Тимчасова підсвітка (імітація наведення)
            uiButtons[currentIndex].bgImage.color = uiButtons[currentIndex].hoverColor;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Hover); // Звук "тік"

            yield return new WaitForSecondsRealtime(delay); // Realtime, бо TimeScale = 0!
            delay += 0.015f; // Поступово сповільнюємо рулетку
        }

        // Рулетка зупинилася!
        UpgradeData chosenUpgrade = currentOptions[currentIndex];

        foreach (var btn in uiButtons) btn.ResetVisuals();

        // Яскраво підсвічуємо переможця
        uiButtons[currentIndex].HighlightAsSelected();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_LevelUp); // Епічний звук

        yield return new WaitForSecondsRealtime(0.8f); // Даємо гравцю секунду порадіти

        ApplyUpgrade(chosenUpgrade);
    }

    private IEnumerator ApplyWithDelayRoutine(UpgradeData upgrade)
    {
        yield return new WaitForSecondsRealtime(0.4f); // Невелика затримка після звичайного кліку
        ApplyUpgrade(upgrade);
    }

    public void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.Health:
                player.maxHealth += upgrade.amount;
                player.currentHealth += upgrade.amount; // Одразу лікуємо на отриману суму!
                break;
            case UpgradeType.Speed:
                player.moveSpeed += upgrade.amount;
                break;
            case UpgradeType.Damage:
                // Збільшуємо глобальний множник урону, щоб він впливав і на ближній бій, і на орбітальний
                if (player != null) player.globalDamageMultiplier += (upgrade.amount / 100f);
                if (hammer != null) hammer.baseDamage += upgrade.amount;
                break;
            case UpgradeType.PickupRadius:
                if (player != null) player.pickupRadius += upgrade.amount;
                break;
            case UpgradeType.AttackSpeed:
                if (weaponOrbit != null) weaponOrbit.baseRotationSpeed += upgrade.amount; // Змінили змінну під новий скрипт
                break;
            case UpgradeType.Armor:
                if (player != null) player.damageReduction += upgrade.amount; // Тепер броня працює!
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