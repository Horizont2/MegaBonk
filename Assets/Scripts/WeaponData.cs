using UnityEngine;

public enum ItemCategory { Sword, Axe, Bow, Helmet, Armor, Gloves } // НОВЕ: Категорії

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public int weaponID;
    public string weaponName;
    public ItemCategory category; // НОВЕ: Вибір категорії в Інспекторі

    public Sprite icon;

    [TextArea(3, 5)]
    public string description;
    public int price;

    [Header("Models")]
    public GameObject shopPrefab;
    public GameObject inGamePrefab;

    [Header("Power System")]
    public int basePower = 20;
    public int powerPerLevel = 15;

    [Header("Upgrade System")]
    public int maxUpgradeLevel = 5;
    public int baseUpgradeCost = 100;
    public float upgradeCostMultiplier = 1.5f;

    [Header("Base Stats")]
    public float damageBonus;
    public float attackSpeed;
    public float critChance;

    [Header("Stat Growth Per Level")]
    public float damagePerLevel = 10f;
    public float attackSpeedPerLevel = 0.02f;
    public float critChancePerLevel = 0.02f;

    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, currentLevel));
    }
}