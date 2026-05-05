using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public int weaponID;
    public string weaponName;
    public int price;

    [Header("Models")]
    public GameObject shopPrefab;
    public GameObject inGamePrefab;

    [Header("Power System (NEW)")]
    public int basePower = 20;     // Базова сила для мапи
    public int powerPerLevel = 15; // Скільки сили дає кожен рівень прокачки

    [Header("Upgrade System (NEW)")]
    public int maxUpgradeLevel = 5;       // Поріг прокачки (треба купувати кращий меч, щоб йти далі)
    public int baseUpgradeCost = 100;     // Ціна першого покращення
    public float upgradeCostMultiplier = 1.5f; // На скільки множиться ціна кожного наступного рівня

    [Header("Base Stats")]
    public float damageBonus;
    public float attackSpeed;
    public float critChance;

    [Header("Stat Growth Per Level (NEW)")]
    public float damagePerLevel = 10f;
    public float attackSpeedPerLevel = 0.02f;
    public float critChancePerLevel = 0.02f;

    // Зручна функція для отримання ціни на конкретному рівні
    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, currentLevel));
    }
}