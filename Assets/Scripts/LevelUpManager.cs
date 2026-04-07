using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum UpgradeType
{
    Health,
    Speed,
    Damage,
    PickupRadius,
    AttackSpeed,
    Armor,       // �� ��������
    HealthRegen  // �� ��������
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

    [Header("Level-Up Flash")]
    public Image levelUpFlashImage;

    [Header("Database")]
    public List<UpgradeData> allPossibleUpgrades;

    private PlayerController player;
    private HammerDamage hammer;
    private WeaponOrbit weaponOrbit;
    private CameraFollow cameraFollow;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        hammer = FindObjectOfType<HammerDamage>();
        weaponOrbit = FindObjectOfType<WeaponOrbit>();
        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        levelUpPanel.SetActive(false);

        // Ensure flash image starts invisible
        if (levelUpFlashImage != null)
        {
            Color c = levelUpFlashImage.color;
            c.a = 0f;
            levelUpFlashImage.color = c;
        }
    }

    public void ShowMenu()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;

        if (starEffect != null) starEffect.PlayEffect();

        // Screen shake on level up
        if (cameraFollow != null) cameraFollow.StartShake();

        // White flash burst
        if (levelUpFlashImage != null) StartCoroutine(LevelUpFlash());

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

            // �������: ������ �������� ����, ��� �������� ���� ���������� ���� � ������� (Closure Bug)
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
                if (hammer != null) hammer.baseDamage += upgrade.amount;
                break;
            case UpgradeType.PickupRadius:
                if (player != null) player.pickupRadius += upgrade.amount;
                break;
            case UpgradeType.AttackSpeed:
                // �������� �������� ��������� ������!
                if (weaponOrbit != null) weaponOrbit.rotationSpeed += upgrade.amount;
                break;
            case UpgradeType.HealthRegen:
                if (player != null) player.healthRegenRate += upgrade.amount;
                break;
        }

        // ��������� ��������� ������ ������ ���� �������� (��� ���� �� ������ ������������)
        if (player != null) player.UpdateHUD();

        ResumeGame();
    }

    private void ResumeGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator LevelUpFlash()
    {
        Color c = levelUpFlashImage.color;
        c.a = 0.6f;
        levelUpFlashImage.color = c;

        // Fade out using unscaledDeltaTime since timeScale is 0
        float t = 0f;
        float dur = 0.5f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.6f, 0f, t / dur);
            levelUpFlashImage.color = c;
            yield return null;
        }
        c.a = 0f;
        levelUpFlashImage.color = c;
    }
}