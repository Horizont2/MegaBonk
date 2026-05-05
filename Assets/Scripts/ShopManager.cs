using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ShopManager : MonoBehaviour
{
    public enum ShopMode { Heroes, Weapons }

    [Header("Current Mode")]
    public ShopMode currentMode = ShopMode.Heroes;

    [Header("Databases")]
    public HeroData[] heroes;
    public WeaponData[] weapons;

    [Header("Spawn Points")]
    public Transform heroPedestalPos;
    public Transform weaponTablePos;
    public Transform weaponInspectPos;
    public Transform offscreenLeft;
    public Transform offscreenRight;

    [Header("UI Dynamic Swapping")]
    public RectTransform mainUIPanel;
    public float panelHidePositionX = -1200f;
    private float panelStartX;
    private Coroutine uiMoveCoroutine;

    [Header("Inspect Feature")]
    public CameraTransitionManager camManager;
    public Camera mainCamera;
    public GameObject inspectButton;
    private bool isInspectingWeapon = false;

    [Header("UI Labels & Icons")]
    public TextMeshProUGUI stat1Label; public TextMeshProUGUI stat2Label; public TextMeshProUGUI stat3Label;
    public Image stat1Icon; public Image stat2Icon; public Image stat3Icon;
    public Sprite heroStat1Sprite; public Sprite heroStat2Sprite; public Sprite heroStat3Sprite;
    public Sprite wepStat1Sprite; public Sprite wepStat2Sprite; public Sprite wepStat3Sprite;

    [Header("UI Fills")]
    public Image stat1Fill; public Image stat2Fill; public Image stat3Fill;
    public Color heroStat1Color = new Color(1f, 0.2f, 0.2f);
    public Color heroStat2Color = new Color(0.2f, 0.6f, 1f);
    public Color heroStat3Color = new Color(0.6f, 0.2f, 1f);
    public Color wepStat1Color = new Color(1f, 0.5f, 0f);
    public Color wepStat2Color = new Color(1f, 0.9f, 0.1f);
    public Color wepStat3Color = new Color(0.1f, 1f, 0.8f);

    [Header("UI Power Stat (NEW 4th Stat)")]
    public TextMeshProUGUI powerLabel;
    public Slider powerSlider;
    public TextMeshProUGUI powerValueText;
    public TextMeshProUGUI powerPercentText;
    public Image powerFill;
    public Color powerColor = new Color(1f, 0.8f, 0.2f); // «ÓÎÓÚËÈ

    [Header("Animation Settings")]
    public float swipeSpeed = 4f;
    public float rotationSpeed = 500f;
    public float modeSwitchCooldown = 1.5f;
    private float nextModeSwitchTime = 0f;

    [Header("Scene Navigation")]
    public string campSceneName = "CampScene";

    [Header("UI Main Elements")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public GameObject diamondIconOnButton;
    public TextMeshProUGUI diamondBalanceText;

    [Header("UI Control Buttons")]
    public Button buyButton;
    public Button backButton;
    public Button leftArrow;
    public Button rightArrow;

    [Header("UI Upgrade Button")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradePriceText;
    public GameObject upgradeDiamondIcon;

    [Header("UI Sliders & Stats (First 3)")]
    public Slider stat1Slider; public Slider stat2Slider; public Slider stat3Slider;
    public TextMeshProUGUI stat1ValueText; public TextMeshProUGUI stat2ValueText; public TextMeshProUGUI stat3ValueText;
    public TextMeshProUGUI stat1PercentText; public TextMeshProUGUI stat2PercentText; public TextMeshProUGUI stat3PercentText;

    [Header("Events")]
    public UnityEvent OnSwitchToHeroes;
    public UnityEvent OnSwitchToWeapons;

    private int currentHeroIndex = 0;
    private int currentWeaponIndex = 0;
    private GameObject currentModel;
    private bool isSwapping = false;
    private bool isTransitioningUI = false;

    private const string DIAMONDS_KEY = "PlayerDiamonds";
    private const string SELECTED_HERO_KEY = "SelectedHeroID";
    private const string SELECTED_WEP_KEY = "SelectedWeaponID";

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (mainCamera == null) mainCamera = Camera.main;

        panelStartX = mainUIPanel.anchoredPosition.x;

        if (!PlayerPrefs.HasKey(DIAMONDS_KEY)) PlayerPrefs.SetInt(DIAMONDS_KEY, 0);
        if (!PlayerPrefs.HasKey(SELECTED_HERO_KEY)) { PlayerPrefs.SetInt(SELECTED_HERO_KEY, 0); PlayerPrefs.SetInt("HeroUnlocked_0", 1); }
        if (!PlayerPrefs.HasKey(SELECTED_WEP_KEY)) { PlayerPrefs.SetInt(SELECTED_WEP_KEY, 0); PlayerPrefs.SetInt("WeaponUnlocked_0", 1); PlayerPrefs.Save(); }

        int savedHeroID = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0);
        for (int i = 0; i < heroes.Length; i++) { if (heroes[i].heroID == savedHeroID) { currentHeroIndex = i; break; } }

        int savedWepID = PlayerPrefs.GetInt(SELECTED_WEP_KEY, 0);
        for (int i = 0; i < weapons.Length; i++) { if (weapons[i].weaponID == savedWepID) { currentWeaponIndex = i; break; } }

        currentMode = ShopMode.Heroes;
        ApplyUITheme();
        if (heroes.Length > 0) SpawnModel(currentHeroIndex, heroPedestalPos.position);
        UpdateUI(false);
        OnSwitchToHeroes.Invoke();

        if (inspectButton != null) inspectButton.SetActive(false);

        if (leftArrow != null) leftArrow.onClick.AddListener(PreviousItem);
        if (rightArrow != null) rightArrow.onClick.AddListener(NextItem);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyOrSelectPressed);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradePressed);
        if (backButton != null) backButton.onClick.AddListener(GoToCampScene);
    }

    private void Update()
    {
        HandleManualRotation();
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextItem();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousItem();

        if (isInspectingWeapon && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.GetComponentInParent<WeaponDisplayObject>() == null) StopInspect();
                }
                else StopInspect();
            }
        }
    }

    private void HandleManualRotation()
    {
        if (currentModel != null && !isSwapping && currentMode == ShopMode.Heroes)
        {
            if (Input.GetMouseButton(0))
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                currentModel.transform.Rotate(Vector3.up, -rotX, Space.World);
            }
        }
    }

    public void StartInspect()
    {
        if (currentMode != ShopMode.Weapons || isSwapping || isTransitioningUI) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        isInspectingWeapon = true;
        if (camManager != null) camManager.GoToInspect();
        if (currentModel != null)
        {
            WeaponDisplayObject display = currentModel.GetComponent<WeaponDisplayObject>();
            if (display != null) display.SetInspect(true);
        }

        if (uiMoveCoroutine != null) StopCoroutine(uiMoveCoroutine);
        uiMoveCoroutine = StartCoroutine(MoveUIPanel(panelHidePositionX));
        if (inspectButton != null) inspectButton.SetActive(false);
    }

    public void StopInspect()
    {
        if (!isInspectingWeapon) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        isInspectingWeapon = false;
        if (camManager != null) camManager.GoToArsenal();
        if (currentModel != null)
        {
            WeaponDisplayObject display = currentModel.GetComponent<WeaponDisplayObject>();
            if (display != null) display.SetInspect(false);
        }

        if (uiMoveCoroutine != null) StopCoroutine(uiMoveCoroutine);
        uiMoveCoroutine = StartCoroutine(MoveUIPanel(panelStartX));
        if (inspectButton != null) inspectButton.SetActive(true);
    }

    private IEnumerator MoveUIPanel(float targetX)
    {
        float t = 0;
        Vector2 pos = mainUIPanel.anchoredPosition;
        float startX = pos.x;

        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            pos.x = Mathf.Lerp(startX, targetX, Mathf.SmoothStep(0, 1, t));
            mainUIPanel.anchoredPosition = pos;
            yield return null;
        }
    }

    public void ToggleShopMode()
    {
        if (isTransitioningUI || Time.time < nextModeSwitchTime) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        nextModeSwitchTime = Time.time + modeSwitchCooldown;
        ShopMode targetMode = (currentMode == ShopMode.Heroes) ? ShopMode.Weapons : ShopMode.Heroes;
        if (isInspectingWeapon) StopInspect();
        StartCoroutine(TransitionShopMode(targetMode));
    }

    private IEnumerator TransitionShopMode(ShopMode newMode)
    {
        isTransitioningUI = true;
        currentMode = newMode;

        if (currentMode == ShopMode.Heroes) OnSwitchToHeroes.Invoke();
        else OnSwitchToWeapons.Invoke();

        yield return StartCoroutine(MoveUIPanel(panelHidePositionX));

        if (currentModel != null) Destroy(currentModel);
        ApplyUITheme();

        if (currentMode == ShopMode.Heroes) SpawnModel(currentHeroIndex, heroPedestalPos.position);
        else SpawnModel(currentWeaponIndex, weaponTablePos.position);

        UpdateUI(false);
        if (inspectButton != null) inspectButton.SetActive(currentMode == ShopMode.Weapons);

        yield return StartCoroutine(MoveUIPanel(panelStartX));
        isTransitioningUI = false;
    }

    private void ApplyUITheme()
    {
        if (powerLabel != null) powerLabel.text = "POWER";
        if (powerFill != null) powerFill.color = powerColor;

        if (currentMode == ShopMode.Heroes)
        {
            if (stat1Label != null) stat1Label.text = "HP";
            if (stat2Label != null) stat2Label.text = "SPEED";
            if (stat3Label != null) stat3Label.text = "RADIUS";
            if (stat1Icon != null) stat1Icon.sprite = heroStat1Sprite;
            if (stat2Icon != null) stat2Icon.sprite = heroStat2Sprite;
            if (stat3Icon != null) stat3Icon.sprite = heroStat3Sprite;
            if (stat1Fill != null) stat1Fill.color = heroStat1Color;
            if (stat2Fill != null) stat2Fill.color = heroStat2Color;
            if (stat3Fill != null) stat3Fill.color = heroStat3Color;
        }
        else
        {
            if (stat1Label != null) stat1Label.text = "DAMAGE";
            if (stat2Label != null) stat2Label.text = "ATTACK SPEED";
            if (stat3Label != null) stat3Label.text = "CRIT CHANCE";
            if (stat1Icon != null) stat1Icon.sprite = wepStat1Sprite;
            if (stat2Icon != null) stat2Icon.sprite = wepStat2Sprite;
            if (stat3Icon != null) stat3Icon.sprite = wepStat3Sprite;
            if (stat1Fill != null) stat1Fill.color = wepStat1Color;
            if (stat2Fill != null) stat2Fill.color = wepStat2Color;
            if (stat3Fill != null) stat3Fill.color = wepStat3Color;
        }
    }

    private void SpawnModel(int index, Vector3 position)
    {
        if (currentMode == ShopMode.Heroes && heroes.Length > 0 && heroes[index].shopModelPrefab != null)
        {
            currentModel = Instantiate(heroes[index].shopModelPrefab, position, heroPedestalPos.rotation);
            Animator anim = currentModel.GetComponent<Animator>();
            if (anim != null) { anim.SetBool("IsGrounded", true); anim.SetFloat("Speed", 0f); }
        }
        else if (currentMode == ShopMode.Weapons && weapons.Length > 0 && weapons[index].shopPrefab != null)
        {
            currentModel = Instantiate(weapons[index].shopPrefab, position, weaponTablePos.rotation);
            WeaponDisplayObject display = currentModel.GetComponent<WeaponDisplayObject>();
            if (display != null) display.Setup(weaponTablePos, weaponInspectPos);
        }
    }

    public void NextItem()
    {
        if (isSwapping || isTransitioningUI) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        if (isInspectingWeapon) { StartCoroutine(StopInspectAndSwap(true)); return; }
        ExecuteSwapNext();
    }

    public void PreviousItem()
    {
        if (isSwapping || isTransitioningUI) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        if (isInspectingWeapon) { StartCoroutine(StopInspectAndSwap(false)); return; }
        ExecuteSwapPrev();
    }

    private IEnumerator StopInspectAndSwap(bool goNext)
    {
        StopInspect();
        yield return new WaitForSeconds(0.4f);
        if (goNext) ExecuteSwapNext();
        else ExecuteSwapPrev();
    }

    private void ExecuteSwapNext()
    {
        int maxLen = (currentMode == ShopMode.Heroes) ? heroes.Length : weapons.Length;
        if (maxLen <= 1) return;
        int oldIndex = (currentMode == ShopMode.Heroes) ? currentHeroIndex : currentWeaponIndex;
        int newIndex = (oldIndex + 1) % maxLen;
        if (currentMode == ShopMode.Heroes) currentHeroIndex = newIndex; else currentWeaponIndex = newIndex;
        StartCoroutine(SwapAnimation(oldIndex, newIndex, false));
    }

    private void ExecuteSwapPrev()
    {
        int maxLen = (currentMode == ShopMode.Heroes) ? heroes.Length : weapons.Length;
        if (maxLen <= 1) return;
        int oldIndex = (currentMode == ShopMode.Heroes) ? currentHeroIndex : currentWeaponIndex;
        int newIndex = (oldIndex - 1 + maxLen) % maxLen;
        if (currentMode == ShopMode.Heroes) currentHeroIndex = newIndex; else currentWeaponIndex = newIndex;
        StartCoroutine(SwapAnimation(oldIndex, newIndex, true));
    }

    private IEnumerator SwapAnimation(int oldIndex, int newIndex, bool swipeRight)
    {
        isSwapping = true;
        GameObject oldModel = currentModel;
        Vector3 spawnPos = (currentMode == ShopMode.Heroes) ? heroPedestalPos.position : weaponTablePos.position;

        Vector3 oldTargetPos = swipeRight ? offscreenRight.position : offscreenLeft.position;
        Vector3 newStartPos = swipeRight ? offscreenLeft.position : offscreenRight.position;

        oldTargetPos.y = spawnPos.y;
        newStartPos.y = spawnPos.y;

        SpawnModel(newIndex, newStartPos);
        UpdateUI(true);

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * swipeSpeed;
            float smoothStep = Mathf.SmoothStep(0f, 1f, progress);
            if (oldModel != null) oldModel.transform.position = Vector3.Lerp(spawnPos, oldTargetPos, smoothStep);
            if (currentModel != null) currentModel.transform.position = Vector3.Lerp(newStartPos, spawnPos, smoothStep);
            yield return null;
        }

        if (oldModel != null) Destroy(oldModel);
        isSwapping = false;
    }

    public void OnBuyOrSelectPressed()
    {
        if (isSwapping || isTransitioningUI) return;

        int id = 0; string unlockKey = ""; string selectKey = ""; int price = 0;

        if (currentMode == ShopMode.Heroes)
        {
            id = heroes[currentHeroIndex].heroID; price = heroes[currentHeroIndex].price;
            unlockKey = "HeroUnlocked_" + id; selectKey = SELECTED_HERO_KEY;
        }
        else
        {
            id = weapons[currentWeaponIndex].weaponID; price = weapons[currentWeaponIndex].price;
            unlockKey = "WeaponUnlocked_" + id; selectKey = SELECTED_WEP_KEY;
        }

        bool isBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;
        int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY, 0);

        if (!isBought)
        {
            if (myDiamonds >= price)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
                PlayerPrefs.SetInt(DIAMONDS_KEY, myDiamonds - price);
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.SetInt(selectKey, id);
                PlayerPrefs.Save();
                UpdateUI(true);
            }
            else if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Error);
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
            PlayerPrefs.SetInt(selectKey, id);
            PlayerPrefs.Save();
            UpdateUI(true);
        }
    }

    public void OnUpgradePressed()
    {
        if (isSwapping || isTransitioningUI || currentMode != ShopMode.Weapons) return;

        int id = weapons[currentWeaponIndex].weaponID;
        int level = PlayerPrefs.GetInt("WeaponLevel_" + id, 0);
        WeaponData w = weapons[currentWeaponIndex];

        if (level < w.maxUpgradeLevel)
        {
            int upgCost = w.GetUpgradeCost(level);
            int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY, 0);

            if (myDiamonds >= upgCost)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_LevelUp);
                PlayerPrefs.SetInt(DIAMONDS_KEY, myDiamonds - upgCost);
                PlayerPrefs.SetInt("WeaponLevel_" + id, level + 1);
                PlayerPrefs.Save();
                UpdateUI(true);
            }
            else if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Error);
        }
    }

    private void UpdateUI(bool animateText)
    {
        int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY, 0);
        if (diamondBalanceText != null) diamondBalanceText.text = myDiamonds.ToString("N0");

        int price = 0; int id = 0; string unlockKey = ""; string selectKey = ""; string itemName = "";

        float stat1FillVal = 0, stat2FillVal = 0, stat3FillVal = 0, powerFillVal = 0;
        string stat1Str = "", stat2Str = "", stat3Str = "", powerStr = "";

        if (currentMode == ShopMode.Heroes)
        {
            if (heroes.Length == 0) return;
            HeroData h = heroes[currentHeroIndex];
            price = h.price; id = h.heroID; unlockKey = "HeroUnlocked_" + id; selectKey = SELECTED_HERO_KEY; itemName = h.heroName;

            float maxGameHP = 600f; float maxGameSpeed = 10f; float maxGameRadius = 12f; float maxGamePower = 300f;

            stat1FillVal = Mathf.Clamp01(h.actualMaxHealth / maxGameHP);
            stat2FillVal = Mathf.Clamp01(h.actualMoveSpeed / maxGameSpeed);
            stat3FillVal = Mathf.Clamp01(h.actualBombRadius / maxGameRadius);
            powerFillVal = Mathf.Clamp01(h.basePower / maxGamePower);

            stat1Str = h.actualMaxHealth.ToString("F0");
            stat2Str = h.actualMoveSpeed.ToString("F1") + " m/s";
            stat3Str = h.actualBombRadius.ToString("F0") + "m";
            powerStr = h.basePower.ToString();
        }
        else
        {
            if (weapons.Length == 0) return;
            WeaponData w = weapons[currentWeaponIndex];
            id = w.weaponID; price = w.price; unlockKey = "WeaponUnlocked_" + id; selectKey = SELECTED_WEP_KEY;

            int level = PlayerPrefs.GetInt("WeaponLevel_" + id, 0);
            itemName = w.weaponName + (level > 0 ? $" <size=70%><color=#AAAAAA>(Lv. {level}/{w.maxUpgradeLevel})</color></size>" : "");

            float currDmg = w.damageBonus + (level * w.damagePerLevel);
            float currSpd = w.attackSpeed + (level * w.attackSpeedPerLevel);
            float currCrit = w.critChance + (level * w.critChancePerLevel);
            int currPower = w.basePower + (level * w.powerPerLevel);

            float maxGameDmg = 400f; float maxGameAtkSpd = 3.0f; float maxGameCrit = 1.0f; float maxGamePower = 1000f;

            stat1FillVal = Mathf.Clamp01(currDmg / maxGameDmg);
            stat2FillVal = Mathf.Clamp01(currSpd / maxGameAtkSpd);
            stat3FillVal = Mathf.Clamp01(currCrit / maxGameCrit);
            powerFillVal = Mathf.Clamp01(currPower / maxGamePower);

            stat1Str = currDmg.ToString("F0");
            stat2Str = currSpd.ToString("F2");
            stat3Str = Mathf.RoundToInt(currCrit * 100) + "%";
            powerStr = currPower.ToString();
        }

        bool isBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;
        bool isSelected = PlayerPrefs.GetInt(selectKey, 0) == id;

        // 1. ÀŒ√≤ ¿ √ŒÀŒ¬ÕŒØ  ÕŒœ »
        if (!isBought)
        {
            priceText.text = "BUY FOR\n" + price.ToString("N0");
            priceText.alignment = TextAlignmentOptions.Left; priceText.margin = new Vector4(10, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(true);

            buyButton.interactable = (myDiamonds >= price);
            priceText.color = (myDiamonds >= price) ? Color.white : new Color(1f, 0.2f, 0.2f);
        }
        else if (!isSelected)
        {
            priceText.text = "EQUIP";
            priceText.alignment = TextAlignmentOptions.Center; priceText.margin = new Vector4(0, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);

            buyButton.interactable = true;
            priceText.color = new Color(0.6f, 1f, 0.2f);
        }
        else
        {
            priceText.text = "EQUIPPED";
            priceText.alignment = TextAlignmentOptions.Center; priceText.margin = new Vector4(0, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);

            buyButton.interactable = false;
            priceText.color = Color.white;
        }

        // 2. ÀŒ√≤ ¿ ƒ–”√ŒØ  ÕŒœ » (œ–Œ ¿◊ ¿)
        if (currentMode == ShopMode.Weapons && isBought)
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);

            int level = PlayerPrefs.GetInt("WeaponLevel_" + id, 0);
            if (level < weapons[currentWeaponIndex].maxUpgradeLevel)
            {
                int upgCost = weapons[currentWeaponIndex].GetUpgradeCost(level);
                if (upgradePriceText != null)
                {
                    upgradePriceText.text = "UPGRADE\n" + upgCost.ToString("N0");
                    upgradePriceText.alignment = TextAlignmentOptions.Left; upgradePriceText.margin = new Vector4(10, 0, 0, 0);
                    upgradePriceText.color = (myDiamonds >= upgCost) ? Color.white : new Color(1f, 0.2f, 0.2f);
                }
                if (upgradeDiamondIcon != null) upgradeDiamondIcon.SetActive(true);
                if (upgradeButton != null) upgradeButton.interactable = (myDiamonds >= upgCost);
            }
            else
            {
                if (upgradePriceText != null)
                {
                    upgradePriceText.text = "MAX LEVEL";
                    upgradePriceText.alignment = TextAlignmentOptions.Center; upgradePriceText.margin = new Vector4(0, 0, 0, 0);
                    upgradePriceText.color = new Color(1f, 0.8f, 0.2f);
                }
                if (upgradeDiamondIcon != null) upgradeDiamondIcon.SetActive(false);
                if (upgradeButton != null) upgradeButton.interactable = false;
            }
        }
        else
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
        }

        if (itemNameText != null) itemNameText.text = itemName;

        // ŒÌÓ‚ÎÂÌÌˇ ·‡ÁÓ‚Ëı 3 ÒÚ‡Ú≥‚
        if (stat1ValueText != null) stat1ValueText.text = stat1Str;
        if (stat2ValueText != null) stat2ValueText.text = stat2Str;
        if (stat3ValueText != null) stat3ValueText.text = stat3Str;
        if (stat1PercentText != null) stat1PercentText.text = Mathf.RoundToInt(stat1FillVal * 100) + "%";
        if (stat2PercentText != null) stat2PercentText.text = Mathf.RoundToInt(stat2FillVal * 100) + "%";
        if (stat3PercentText != null) stat3PercentText.text = Mathf.RoundToInt(stat3FillVal * 100) + "%";
        if (stat1Slider != null) stat1Slider.value = stat1FillVal;
        if (stat2Slider != null) stat2Slider.value = stat2FillVal;
        if (stat3Slider != null) stat3Slider.value = stat3FillVal;

        // ŒÌÓ‚ÎÂÌÌˇ ÌÓ‚Ó„Ó 4-„Ó ÒÚ‡Ú‡ (Power)
        if (powerValueText != null) powerValueText.text = powerStr;
        if (powerPercentText != null) powerPercentText.text = Mathf.RoundToInt(powerFillVal * 100) + "%";
        if (powerSlider != null) powerSlider.value = powerFillVal;

        if (animateText)
        {
            StartCoroutine(PopText(itemNameText)); StartCoroutine(PopText(priceText));
            StartCoroutine(PopText(stat1ValueText)); StartCoroutine(PopText(stat2ValueText)); StartCoroutine(PopText(stat3ValueText));
            if (powerValueText != null) StartCoroutine(PopText(powerValueText));
            if (upgradeButton != null && upgradeButton.gameObject.activeSelf) StartCoroutine(PopText(upgradePriceText));
        }

        CalculateAndSaveTotalPower();
    }

    private void CalculateAndSaveTotalPower()
    {
        int baseTotal = 0;
        int selHeroID = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0);
        int selWepID = PlayerPrefs.GetInt(SELECTED_WEP_KEY, 0);

        if (heroes != null) foreach (var h in heroes) if (h.heroID == selHeroID) baseTotal += h.basePower;
        if (weapons != null) foreach (var w in weapons)
            {
                if (w.weaponID == selWepID)
                {
                    int lvl = PlayerPrefs.GetInt("WeaponLevel_" + w.weaponID, 0);
                    baseTotal += w.basePower + (lvl * w.powerPerLevel);
                    PlayerPrefs.SetFloat("EquippedWeaponDamage", w.damageBonus + (lvl * w.damagePerLevel));
                }
            }

        // «‡ÒÚÓÒÓ‚Û∫ÏÓ ÏÌÓÊÌËÍ ÍÛÁÌ≥ ‰Ó Á‡„‡Î¸ÌÓø ÒËÎË
        int forgeLevel = PlayerPrefs.GetInt("SaveBld_Forge", 0);
        float forgeMultiplier = 1.00f;
        switch (forgeLevel)
        {
            case 1: forgeMultiplier = 1.02f; break;
            case 2: forgeMultiplier = 1.05f; break;
            case 3: forgeMultiplier = 1.08f; break;
            case 4: forgeMultiplier = 1.11f; break;
            case 5: forgeMultiplier = 1.15f; break;
        }

        int finalTotalPower = Mathf.RoundToInt((baseTotal == 0 ? 50 : baseTotal) * forgeMultiplier);
        PlayerPrefs.SetInt("PlayerTotalPower", finalTotalPower);
    }

    private IEnumerator PopText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) yield break;
        textComponent.transform.localScale = Vector3.one;
        Vector3 popScale = new Vector3(1.2f, 1.2f, 1.2f);
        float t = 0;
        while (t < 1) { t += Time.deltaTime / 0.08f; textComponent.transform.localScale = Vector3.Lerp(Vector3.one, popScale, t); yield return null; }
        t = 0;
        while (t < 1) { t += Time.deltaTime / 0.15f; textComponent.transform.localScale = Vector3.Lerp(popScale, Vector3.one, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
        textComponent.transform.localScale = Vector3.one;
    }

    public void GoToCampScene()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        PlayerPrefs.SetInt("ReturningFromShop", 1);
        PlayerPrefs.Save();
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;

        if (mainUIPanel != null) mainUIPanel.gameObject.SetActive(false);
        if (inspectButton != null) inspectButton.SetActive(false);

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(campSceneName);
        else SceneManager.LoadScene(campSceneName);
    }
}