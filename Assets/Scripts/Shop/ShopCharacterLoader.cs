using UnityEngine;

/// <summary>
/// Reads the player's equipped character/weapon from PlayerPrefs
/// and applies stats to the PlayerController at game start.
/// Attach to the same GameObject as PlayerController, or to a GameManager.
/// </summary>
public class ShopCharacterLoader : MonoBehaviour
{
    [Header("All Available Items (drag all ShopItemData assets here)")]
    public ShopItemData[] allItems;

    [Header("References")]
    public PlayerController playerController;
    [Tooltip("Spawn point for the character model (if swapping visuals)")]
    public Transform modelParent;

    private void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        ApplyEquippedCharacter();
        ApplyEquippedWeapon();
    }

    private void ApplyEquippedCharacter()
    {
        string equippedID = PlayerPrefs.GetString("EquippedCharacter", "");
        if (string.IsNullOrEmpty(equippedID)) return;

        ShopItemData charData = FindItem(equippedID);
        if (charData == null || charData.itemType != ShopItemType.Character) return;

        // Apply character stats
        CharacterStats stats = charData.characterStats;
        if (playerController != null)
        {
            playerController.maxHealth = stats.maxHP;
            playerController.currentHealth = stats.maxHP;
            playerController.moveSpeed = stats.moveSpeed;
            playerController.pickupRadius = stats.pickupRadius;
            playerController.UpdateHUD();
        }

        // Swap visual model if gameplay prefab is provided
        if (charData.gameplayPrefab != null && modelParent != null)
        {
            // Clear existing model children
            foreach (Transform child in modelParent)
                Destroy(child.gameObject);

            Instantiate(charData.gameplayPrefab, modelParent.position, modelParent.rotation, modelParent);
        }
    }

    private void ApplyEquippedWeapon()
    {
        string equippedID = PlayerPrefs.GetString("EquippedWeapon", "");
        if (string.IsNullOrEmpty(equippedID)) return;

        ShopItemData weaponData = FindItem(equippedID);
        if (weaponData == null || weaponData.itemType != ShopItemType.Weapon) return;

        // Apply weapon stats to WeaponOrbit or HammerDamage
        WeaponOrbit weapon = FindObjectOfType<WeaponOrbit>();
        HammerDamage hammer = FindObjectOfType<HammerDamage>();

        if (hammer != null)
        {
            hammer.damage = weaponData.weaponStats.damage;
            hammer.knockbackForce = weaponData.weaponStats.knockback;
        }

        // Swap weapon model if provided
        if (weaponData.gameplayPrefab != null && weapon != null)
        {
            Transform weaponModel = weapon.transform.GetChild(0);
            if (weaponModel != null) Destroy(weaponModel.gameObject);
            Instantiate(weaponData.gameplayPrefab, weapon.transform);
        }
    }

    private ShopItemData FindItem(string itemID)
    {
        foreach (ShopItemData item in allItems)
        {
            if (item != null && item.itemID == itemID)
                return item;
        }
        return null;
    }
}
