using UnityEngine;

public enum ShopItemType
{
    Character,
    Weapon,
    Grenade,
    Ability
}

[CreateAssetMenu(fileName = "New Shop Item", menuName = "MegaBonk/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public ShopItemType itemType;
    public Sprite icon;

    [Header("Price")]
    public int price = 100;
    public bool isDefaultUnlocked = false;

    [Header("Prefabs")]
    [Tooltip("The model prefab to display on the shop pedestal (visual only)")]
    public GameObject displayPrefab;
    [Tooltip("The actual gameplay prefab to spawn in-game")]
    public GameObject gameplayPrefab;

    [Header("Character Stats (only for Character type)")]
    public CharacterStats characterStats;

    [Header("Weapon Stats (only for Weapon type)")]
    public WeaponStats weaponStats;

    [Header("Grenade Stats (only for Grenade type)")]
    public GrenadeStats grenadeStats;

    [Header("Passive Ability")]
    [TextArea(1, 2)]
    public string passiveDescription;
}

[System.Serializable]
public class CharacterStats
{
    public float maxHP = 100f;
    public float moveSpeed = 8f;
    public float pickupRadius = 4f;
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;
    [Range(0.5f, 2f)]
    public float damageMultiplier = 1f;
}

[System.Serializable]
public class WeaponStats
{
    public float damage = 25f;
    public float attackSpeed = 1f;
    public float range = 3f;
    public float knockback = 2f;
}

[System.Serializable]
public class GrenadeStats
{
    public float explosionRadius = 6f;
    public float damage = 200f;
    public float cooldown = 5f;
    public float delay = 2f;
}
