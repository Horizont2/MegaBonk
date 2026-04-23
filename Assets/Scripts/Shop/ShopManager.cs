using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main shop controller. Manages camera movement between pedestals,
/// purchasing, equipping, and navigation. Place on an empty GO in the shop scene.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Pedestals")]
    [Tooltip("All pedestals in the scene, in order (left to right)")]
    public ShopPedestal[] pedestals;

    [Header("Camera")]
    [Tooltip("The main shop camera")]
    public Transform shopCamera;
    [Tooltip("How far back the camera sits from pedestal")]
    public float cameraDistance = 4f;
    [Tooltip("How high the camera sits above pedestal")]
    public float cameraHeight = 1.5f;
    public float cameraLerpSpeed = 4f;
    [Tooltip("Optional: camera look-at offset (Y) so it looks slightly above the model base")]
    public float lookAtOffsetY = 1f;

    [Header("UI Manager")]
    public ShopUIManager uiManager;

    [Header("Effects")]
    public ShopEffects effects;

    [Header("Navigation")]
    [Tooltip("Keyboard keys for navigation")]
    public KeyCode prevKey = KeyCode.LeftArrow;
    public KeyCode nextKey = KeyCode.RightArrow;
    public KeyCode buyKey = KeyCode.Return;
    public KeyCode backKey = KeyCode.Escape;

    [Header("Scene Navigation")]
    public string mainMenuSceneName = "MainMenu";

    private int currentIndex = 0;
    private Vector3 cameraTargetPos;
    private Vector3 cameraTargetLookAt;
    private bool isTransitioning = false;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (pedestals.Length > 0)
        {
            // Find currently equipped character to start on that pedestal
            string equippedID = PlayerPrefs.GetString("EquippedCharacter", "");
            for (int i = 0; i < pedestals.Length; i++)
            {
                if (pedestals[i].itemData != null && pedestals[i].itemData.itemID == equippedID)
                {
                    currentIndex = i;
                    break;
                }
            }

            FocusPedestal(currentIndex, instant: true);
        }

        UpdateUI();
    }

    private void Update()
    {
        // Navigation input
        if (Input.GetKeyDown(nextKey))
            Navigate(1);
        else if (Input.GetKeyDown(prevKey))
            Navigate(-1);

        if (Input.GetKeyDown(buyKey))
            TryBuyOrEquip();

        if (Input.GetKeyDown(backKey))
            GoBack();

        // Smooth camera movement
        if (shopCamera != null)
        {
            shopCamera.position = Vector3.Lerp(shopCamera.position, cameraTargetPos, cameraLerpSpeed * Time.deltaTime);
            Vector3 lookDir = cameraTargetLookAt - shopCamera.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                shopCamera.rotation = Quaternion.Slerp(shopCamera.rotation, targetRot, cameraLerpSpeed * Time.deltaTime);
            }
        }
    }

    // ─── NAVIGATION ───

    public void Navigate(int direction)
    {
        if (pedestals.Length == 0) return;

        int newIndex = currentIndex + direction;
        if (newIndex < 0) newIndex = pedestals.Length - 1;
        if (newIndex >= pedestals.Length) newIndex = 0;

        if (newIndex != currentIndex)
        {
            // Unfocus old
            pedestals[currentIndex].SetFocused(false);
            currentIndex = newIndex;
            FocusPedestal(currentIndex, instant: false);
            UpdateUI();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("buttonClick");
        }
    }

    public void NavigateNext() => Navigate(1);
    public void NavigatePrev() => Navigate(-1);

    private void FocusPedestal(int index, bool instant)
    {
        ShopPedestal ped = pedestals[index];
        ped.SetFocused(true);

        // Calculate camera position: behind and above the pedestal
        Transform target = ped.GetCameraTarget();
        Vector3 pedestalForward = target.forward;

        // Camera sits in front of the pedestal looking at it
        cameraTargetPos = target.position - pedestalForward * cameraDistance + Vector3.up * cameraHeight;
        cameraTargetLookAt = target.position + Vector3.up * lookAtOffsetY;

        if (instant && shopCamera != null)
        {
            shopCamera.position = cameraTargetPos;
            shopCamera.LookAt(cameraTargetLookAt);
        }
    }

    // ─── BUY / EQUIP ───

    public void TryBuyOrEquip()
    {
        ShopItemData item = GetCurrentItem();
        if (item == null) return;

        ItemPurchaseState state = GetItemState(item);

        switch (state)
        {
            case ItemPurchaseState.Locked:
                TryPurchase(item);
                break;
            case ItemPurchaseState.Owned:
                EquipItem(item);
                break;
            case ItemPurchaseState.Equipped:
                // Already equipped, do nothing
                break;
        }
    }

    private void TryPurchase(ShopItemData item)
    {
        if (SaveManager.GetTotalCrystals() >= item.price)
        {
            // Spend crystals
            SaveManager.SpendCrystals(item.price);

            // Mark as purchased
            PlayerPrefs.SetInt("ShopOwned_" + item.itemID, 1);
            PlayerPrefs.Save();

            // Auto-equip on purchase
            EquipItem(item);

            // Effects
            pedestals[currentIndex].PlayPurchaseAnimation();

            if (effects != null)
                effects.PlayPurchaseEffect(pedestals[currentIndex].transform.position);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("levelUp");

            UpdateUI();
        }
        else
        {
            // Not enough crystals
            if (effects != null)
                effects.PlayErrorEffect();

            if (uiManager != null)
                uiManager.ShakeBuyButton();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("error");
        }
    }

    private void EquipItem(ShopItemData item)
    {
        switch (item.itemType)
        {
            case ShopItemType.Character:
                PlayerPrefs.SetString("EquippedCharacter", item.itemID);
                break;
            case ShopItemType.Weapon:
                PlayerPrefs.SetString("EquippedWeapon", item.itemID);
                break;
            case ShopItemType.Grenade:
                PlayerPrefs.SetString("EquippedGrenade", item.itemID);
                break;
            case ShopItemType.Ability:
                PlayerPrefs.SetString("EquippedAbility", item.itemID);
                break;
        }
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("buttonClick");

        UpdateUI();
    }

    public void GoBack()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ─── STATE HELPERS ───

    public ShopItemData GetCurrentItem()
    {
        if (pedestals.Length == 0 || currentIndex >= pedestals.Length) return null;
        return pedestals[currentIndex].itemData;
    }

    public ItemPurchaseState GetItemState(ShopItemData item)
    {
        if (item == null) return ItemPurchaseState.Locked;

        bool owned = item.isDefaultUnlocked || PlayerPrefs.GetInt("ShopOwned_" + item.itemID, 0) == 1;

        if (!owned) return ItemPurchaseState.Locked;

        // Check if equipped
        string equippedKey = item.itemType switch
        {
            ShopItemType.Character => "EquippedCharacter",
            ShopItemType.Weapon => "EquippedWeapon",
            ShopItemType.Grenade => "EquippedGrenade",
            ShopItemType.Ability => "EquippedAbility",
            _ => ""
        };

        if (PlayerPrefs.GetString(equippedKey, "") == item.itemID)
            return ItemPurchaseState.Equipped;

        return ItemPurchaseState.Owned;
    }

    public int GetCurrentIndex() => currentIndex;
    public int GetTotalCount() => pedestals.Length;

    private void UpdateUI()
    {
        if (uiManager != null)
            uiManager.UpdateDisplay(this);
    }
}

public enum ItemPurchaseState
{
    Locked,
    Owned,
    Equipped
}
