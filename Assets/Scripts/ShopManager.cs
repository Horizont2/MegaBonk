using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    public Transform offscreenLeft;
    public Transform offscreenRight;

    [Header("Scene Navigation")]
    public string campSceneName = "CampScene";

    [Header("UI Tabs (Category BG)")]
    public Button heroesTabButton;
    public CanvasGroup heroesTabGroup;
    public Button arsenalTabButton;
    public CanvasGroup arsenalTabGroup;
    public float activeTabAlpha = 0.3f;
    public float inactiveTabAlpha = 1f;

    [Header("UI Canvas Groups (For AAA Fades)")]
    public CanvasGroup heroArrowsGroup;
    public CanvasGroup arsenalGridGroup;
    public CanvasGroup arsenalContentGroup;
    public CanvasGroup arsenalDescriptionGroup; // ФІКС: Окрема група для опису зброї

    [Header("Stats Panel Dynamic Positioning")]
    public RectTransform statsPanelRect;
    public CanvasGroup statsPanelGroup;
    public Vector2 statsPosHero = new Vector2(300, 0);
    public Vector2 statsPosArsenal = new Vector2(1500, 0);

    [Header("Arsenal Dynamic List")]
    public Transform itemListContent;
    public GameObject itemButtonPrefab;
    public Button backToGridButton;

    [Header("Arsenal Category Buttons")]
    public Button btnCategorySwords;
    public Button btnCategoryAxes;
    public Button btnCategoryBows;
    public Button btnCategoryHelmets;
    public Button btnCategoryArmor;
    public Button btnCategoryGloves;

    [Header("UI Labels & Theme Colors")]
    public TextMeshProUGUI stat1Label; public TextMeshProUGUI stat2Label; public TextMeshProUGUI stat3Label; public TextMeshProUGUI powerLabel;

    public Color heroStat1Color = new Color(1f, 0.2f, 0.2f);
    public Color heroStat2Color = new Color(0.2f, 0.6f, 1f);
    public Color heroStat3Color = new Color(0.6f, 0.2f, 1f);

    public Color wepStat1Color = new Color(1f, 0.5f, 0f);
    public Color wepStat2Color = new Color(1f, 0.9f, 0.1f);
    public Color wepStat3Color = new Color(0.1f, 1f, 0.8f);

    public Color powerColor = new Color(0.6f, 0.8f, 0.2f);

    private Color textNormalColor;
    private Color textErrorColor;
    private Color textSuccessColor;

    [Header("UI Fills (Images)")]
    public Image stat1Fill; public Image stat2Fill; public Image stat3Fill; public Image powerFill;

    [Header("Animation Settings")]
    public float swipeSpeed = 4f;
    public float rotationSpeed = 500f;
    public float fadeSpeed = 7f;

    [Header("UI Main Elements")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public Image descriptionItemIcon;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI diamondBalanceText;

    [Header("UI Control Buttons")]
    public Button buyButton;
    public Button backToCampButton;
    public Button leftArrow;
    public Button rightArrow;

    [Header("UI Upgrade Button")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradePriceText;

    [Header("Buy Button Dynamic Style")]
    public RectTransform buyButtonRect;
    public Image buyButtonImage;
    public Vector2 heroBuyButtonSize = new Vector2(250f, 60f);
    public Color heroBuyButtonColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    public Vector2 arsenalBuyButtonSize = new Vector2(350f, 80f);
    public Color arsenalBuyButtonColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private Coroutine buyButtonAnimCoroutine;

    [Header("UI Stats Values")]
    public TextMeshProUGUI stat1PercentText; public TextMeshProUGUI stat2PercentText; public TextMeshProUGUI stat3PercentText; public TextMeshProUGUI powerPercentText;

    private int currentHeroIndex = 0;
    private int currentWeaponIndex = 0;
    private GameObject currentHeroModel;
    private GameObject currentWeaponModel;

    private bool isSwapping = false;
    private bool isFading = false;
    private Coroutine statsMoveCoroutine;
    private bool isViewingWeaponCategory = false;

    private List<WeaponData> currentCategoryList = new List<WeaponData>();
    private WeaponData selectedWeaponData;

    private const string DIAMONDS_KEY = "PlayerDiamonds";
    private const string SELECTED_HERO_KEY = "SelectedHeroID";
    private const string SELECTED_WEP_KEY = "SelectedWeaponID";

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#EAE5D9", out textNormalColor);
        ColorUtility.TryParseHtmlString("#8B2E2E", out textErrorColor);
        ColorUtility.TryParseHtmlString("#758B2E", out textSuccessColor);

        SetGroupAlpha(statsPanelGroup, 0);
        SetGroupAlpha(heroArrowsGroup, 0);
        SetGroupAlpha(arsenalGridGroup, 0);
        SetGroupAlpha(arsenalContentGroup, 0);
        SetGroupAlpha(arsenalDescriptionGroup, 0);
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!PlayerPrefs.HasKey(DIAMONDS_KEY)) PlayerPrefs.SetInt(DIAMONDS_KEY, 0);
        if (!PlayerPrefs.HasKey(SELECTED_HERO_KEY)) { PlayerPrefs.SetInt(SELECTED_HERO_KEY, 0); PlayerPrefs.SetInt("HeroUnlocked_0", 1); }
        if (!PlayerPrefs.HasKey(SELECTED_WEP_KEY)) { PlayerPrefs.SetInt(SELECTED_WEP_KEY, 0); PlayerPrefs.SetInt("WeaponUnlocked_0", 1); PlayerPrefs.Save(); }

        int savedHeroID = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0);
        for (int i = 0; i < heroes.Length; i++) { if (heroes[i].heroID == savedHeroID) { currentHeroIndex = i; break; } }

        int savedWepID = PlayerPrefs.GetInt(SELECTED_WEP_KEY, 0);
        for (int i = 0; i < weapons.Length; i++) { if (weapons[i].weaponID == savedWepID) { selectedWeaponData = weapons[i]; currentWeaponIndex = i; break; } }

        if (leftArrow) leftArrow.onClick.AddListener(() => { PlayButtonAnim(leftArrow.transform); PreviousHero(); });
        if (rightArrow) rightArrow.onClick.AddListener(() => { PlayButtonAnim(rightArrow.transform); NextHero(); });
        if (buyButton) buyButton.onClick.AddListener(() => { PlayButtonAnim(buyButton.transform); OnBuyOrSelectPressed(); });
        if (upgradeButton) upgradeButton.onClick.AddListener(() => { PlayButtonAnim(upgradeButton.transform); OnUpgradePressed(); });
        if (backToCampButton) backToCampButton.onClick.AddListener(() => { PlayButtonAnim(backToCampButton.transform); GoToCampScene(); });
        if (backToGridButton) backToGridButton.onClick.AddListener(() => { PlayButtonAnim(backToGridButton.transform); ReturnToArsenalGrid(); });

        if (heroesTabButton) heroesTabButton.onClick.AddListener(() => { PlayButtonAnim(heroesTabButton.transform); SwitchTab(ShopMode.Heroes); });
        if (arsenalTabButton) arsenalTabButton.onClick.AddListener(() => { PlayButtonAnim(arsenalTabButton.transform); SwitchTab(ShopMode.Weapons); });

        if (btnCategorySwords) btnCategorySwords.onClick.AddListener(() => { PlayButtonAnim(btnCategorySwords.transform); OpenArsenalCategory(ItemCategory.Sword); });
        if (btnCategoryAxes) btnCategoryAxes.onClick.AddListener(() => { PlayButtonAnim(btnCategoryAxes.transform); OpenArsenalCategory(ItemCategory.Axe); });
        if (btnCategoryBows) btnCategoryBows.onClick.AddListener(() => { PlayButtonAnim(btnCategoryBows.transform); OpenArsenalCategory(ItemCategory.Bow); });
        if (btnCategoryHelmets) btnCategoryHelmets.onClick.AddListener(() => { PlayButtonAnim(btnCategoryHelmets.transform); OpenArsenalCategory(ItemCategory.Helmet); });
        if (btnCategoryArmor) btnCategoryArmor.onClick.AddListener(() => { PlayButtonAnim(btnCategoryArmor.transform); OpenArsenalCategory(ItemCategory.Armor); });
        if (btnCategoryGloves) btnCategoryGloves.onClick.AddListener(() => { PlayButtonAnim(btnCategoryGloves.transform); OpenArsenalCategory(ItemCategory.Gloves); });

        // --- ФІКС: Віднімаємо 40 від поточної позиції, яку ти виставив в Інспекторі ---
        if (buyButtonRect != null)
        {
            float currentX = buyButtonRect.anchoredPosition.x;
            buyButtonRect.anchoredPosition = new Vector2(currentX - 40, buyButtonRect.anchoredPosition.y);
        }

        currentMode = ShopMode.Heroes;
        ApplyUITheme();
        UpdateTabVisuals();

        if (buyButtonRect != null) buyButtonRect.sizeDelta = heroBuyButtonSize;
        if (buyButtonImage != null) buyButtonImage.color = heroBuyButtonColor;

        SpawnHero(currentHeroIndex, heroPedestalPos.position);

        if (statsPanelRect != null) statsPanelRect.anchoredPosition = statsPosHero;
        UpdateUI(false);

        StartCoroutine(FadeAndScaleGroup(statsPanelGroup, 1f));
        StartCoroutine(FadeAndScaleGroup(heroArrowsGroup, 1f));
    }

    private void Update()
    {
        if (currentHeroModel != null && !isSwapping && Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentHeroModel.transform.Rotate(Vector3.up, -rotX, Space.World);
        }
    }

    // --- AAA АНІМАЦІЯ КЛІКУ КНОПОК ---
    private void PlayButtonAnim(Transform btnTransform)
    {
        StartCoroutine(ButtonScaleRoutine(btnTransform));
    }

    private IEnumerator ButtonScaleRoutine(Transform btnTransform)
    {
        Vector3 originalScale = Vector3.one;
        btnTransform.localScale = originalScale * 0.9f; // Стискаємо кнопку
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 12f;
            btnTransform.localScale = Vector3.Lerp(originalScale * 0.9f, originalScale, t);
            yield return null;
        }
        btnTransform.localScale = originalScale;
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = (alpha > 0.5f);
        group.blocksRaycasts = (alpha > 0.5f);
    }

    // --- AAA АНІМАЦІЯ ПОЯВИ ПАНЕЛЕЙ (Фейд + Легке збільшення) ---
    private IEnumerator FadeAndScaleGroup(CanvasGroup group, float targetAlpha)
    {
        if (group == null) yield break;
        isFading = true;

        group.blocksRaycasts = (targetAlpha > 0.5f);
        group.interactable = (targetAlpha > 0.5f);

        float startAlpha = group.alpha;
        Vector3 startScale = (targetAlpha > 0.5f) ? new Vector3(0.95f, 0.95f, 0.95f) : Vector3.one;
        Vector3 endScale = (targetAlpha > 0.5f) ? Vector3.one : new Vector3(0.95f, 0.95f, 0.95f);
        Transform tForm = group.transform;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            float curve = Mathf.SmoothStep(0, 1, t);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, curve);
            tForm.localScale = Vector3.Lerp(startScale, endScale, curve);
            yield return null;
        }
        group.alpha = targetAlpha;
        tForm.localScale = endScale;
        isFading = false;
    }

    private IEnumerator TeleportAndFadeStatsPanel(Vector2 targetPos, float targetAlpha)
    {
        if (statsPanelGroup == null || statsPanelRect == null) yield break;
        isFading = true;
        statsPanelGroup.blocksRaycasts = false;

        while (statsPanelGroup.alpha > 0.01f)
        {
            statsPanelGroup.alpha = Mathf.MoveTowards(statsPanelGroup.alpha, 0f, Time.deltaTime * fadeSpeed * 1.5f);
            yield return null;
        }
        statsPanelGroup.alpha = 0f;

        statsPanelRect.anchoredPosition = targetPos;

        if (targetAlpha > 0f)
        {
            ApplyUITheme();
            UpdateUI(false);

            while (statsPanelGroup.alpha < targetAlpha - 0.01f)
            {
                statsPanelGroup.alpha = Mathf.MoveTowards(statsPanelGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed * 1.5f);
                yield return null;
            }
        }

        statsPanelGroup.alpha = targetAlpha;
        statsPanelGroup.blocksRaycasts = (targetAlpha > 0.5f);
        isFading = false;
    }

    // --- НОВИЙ МЕТОД: Повертає зброю в руці до тієї, що реально екіпірована ---
    private void RevertToEquippedWeapon()
    {
        int equippedWepID = PlayerPrefs.GetInt(SELECTED_WEP_KEY, 0);
        int equippedIndex = 0;
        for (int i = 0; i < weapons.Length; i++) { if (weapons[i].weaponID == equippedWepID) { equippedIndex = i; break; } }

        if (currentWeaponIndex != equippedIndex)
        {
            currentWeaponIndex = equippedIndex;
            selectedWeaponData = weapons[currentWeaponIndex];
            EquipWeaponToHero(currentWeaponIndex);
        }
    }
    public void SwitchTab(ShopMode newMode)
    {
        if (currentMode == newMode || isSwapping || isFading) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        currentMode = newMode;
        UpdateTabVisuals();

        if (buyButtonAnimCoroutine != null) StopCoroutine(buyButtonAnimCoroutine);
        buyButtonAnimCoroutine = StartCoroutine(AnimateBuyButtonRoutine(currentMode));

        if (statsMoveCoroutine != null) StopCoroutine(statsMoveCoroutine);

        if (currentMode == ShopMode.Heroes)
        {
            isViewingWeaponCategory = false; // Скидаємо стан
            RevertToEquippedWeapon(); // ФІКС: Повертаємо зброю в руці до екіпірованої

            StartCoroutine(FadeAndScaleGroup(arsenalGridGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(arsenalContentGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(arsenalDescriptionGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(heroArrowsGroup, 1f));

            statsMoveCoroutine = StartCoroutine(TeleportAndFadeStatsPanel(statsPosHero, 1f));
        }
        else
        {
            isViewingWeaponCategory = false; // Ми на сітці
            StartCoroutine(FadeAndScaleGroup(heroArrowsGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(arsenalContentGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(arsenalDescriptionGroup, 0f));
            StartCoroutine(FadeAndScaleGroup(arsenalGridGroup, 1f));

            statsMoveCoroutine = StartCoroutine(TeleportAndFadeStatsPanel(statsPosArsenal, 0f));

            if (itemNameText) itemNameText.text = "";
            if (itemDescriptionText) itemDescriptionText.text = "Select an item from a category.";
            if (upgradeButton) upgradeButton.gameObject.SetActive(false);

            // --- ФІКС: Повертаємо екіпірованого героя з анімацією ---
            int equippedHeroID = PlayerPrefs.GetInt(SELECTED_HERO_KEY, 0);
            int equippedIndex = 0;
            for (int i = 0; i < heroes.Length; i++) { if (heroes[i].heroID == equippedHeroID) { equippedIndex = i; break; } }

            if (currentHeroIndex != equippedIndex)
            {
                int oldIndex = currentHeroIndex;
                currentHeroIndex = equippedIndex;
                StartCoroutine(SwapHeroAnimation(oldIndex, currentHeroIndex, currentHeroIndex < oldIndex));
            }
        }
        UpdateUI(false);
    }

    private IEnumerator AnimateBuyButtonRoutine(ShopMode targetMode)
    {
        if (buyButtonRect == null || buyButtonImage == null) yield break;

        Vector2 targetSize = (targetMode == ShopMode.Heroes) ? heroBuyButtonSize : arsenalBuyButtonSize;
        Color targetColor = (targetMode == ShopMode.Heroes) ? heroBuyButtonColor : arsenalBuyButtonColor;

        Vector2 startSize = buyButtonRect.sizeDelta;
        Color startColor = buyButtonImage.color;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            float curve = Mathf.SmoothStep(0f, 1f, t);

            buyButtonRect.sizeDelta = Vector2.Lerp(startSize, targetSize, curve);
            buyButtonImage.color = Color.Lerp(startColor, targetColor, curve);

            yield return null;
        }
    }

    private void UpdateTabVisuals()
    {
        // ВАЖЛИВО: Перевір в Інспекторі, щоб Inactive Tab Alpha було 1.0!
        if (heroesTabGroup) heroesTabGroup.alpha = (currentMode == ShopMode.Heroes) ? activeTabAlpha : inactiveTabAlpha;
        if (arsenalTabGroup) arsenalTabGroup.alpha = (currentMode == ShopMode.Weapons) ? activeTabAlpha : inactiveTabAlpha;
    }

    public void OpenArsenalCategory(ItemCategory cat)
    {
        if (isFading) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        isViewingWeaponCategory = true; // Ми відкрили список

        StartCoroutine(FadeAndScaleGroup(arsenalGridGroup, 0f));

        foreach (Transform child in itemListContent) Destroy(child.gameObject);
        currentCategoryList.Clear();

        foreach (var w in weapons)
        {
            if (w.category == cat)
            {
                currentCategoryList.Add(w);

                GameObject btnObj = Instantiate(itemButtonPrefab, itemListContent);
                ShopItemButton itemSlot = btnObj.GetComponent<ShopItemButton>();

                if (itemSlot != null)
                {
                    if (itemSlot.nameText != null) itemSlot.nameText.text = w.weaponName;
                    if (itemSlot.iconImage != null) itemSlot.iconImage.sprite = w.icon;

                    WeaponData capturedData = w;
                    if (itemSlot.buttonComponent != null)
                    {
                        itemSlot.buttonComponent.onClick.AddListener(() =>
                        {
                            PlayButtonAnim(itemSlot.transform);
                            SelectWeaponFromList(capturedData);
                        });
                    }
                }
            }
        }

        if (currentCategoryList.Count > 0)
        {
            SelectWeaponFromList(currentCategoryList[0]);
        }
        else
        {
            if (itemNameText) itemNameText.text = "Empty Category";
            if (itemDescriptionText) itemDescriptionText.text = "There are no items in this category yet.";
            UpdateUI(false);
        }

        StartCoroutine(FadeAndScaleGroup(arsenalContentGroup, 1f));
        StartCoroutine(FadeAndScaleGroup(arsenalDescriptionGroup, 1f));
        StartCoroutine(FadeAndScaleGroup(statsPanelGroup, 1f));
    }

    public void ReturnToArsenalGrid()
    {
        if (isFading) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        isViewingWeaponCategory = false; // Повернулися на сітку
        RevertToEquippedWeapon(); // ФІКС: Повертаємо зброю в руці до екіпірованої

        StartCoroutine(FadeAndScaleGroup(arsenalContentGroup, 0f));
        StartCoroutine(FadeAndScaleGroup(arsenalDescriptionGroup, 0f));
        StartCoroutine(FadeAndScaleGroup(statsPanelGroup, 0f));
        StartCoroutine(FadeAndScaleGroup(arsenalGridGroup, 1f));

        if (itemNameText) itemNameText.text = "";
        if (itemDescriptionText) itemDescriptionText.text = "Select a category.";

        UpdateUI(false);
    }

    private void SelectWeaponFromList(WeaponData wData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Hover);
        selectedWeaponData = wData;

        for (int i = 0; i < weapons.Length; i++) { if (weapons[i] == wData) { currentWeaponIndex = i; break; } }

        EquipWeaponToHero(currentWeaponIndex);
        UpdateUI(true);
    }

    private void SpawnHero(int index, Vector3 position)
    {
        if (currentHeroModel != null) Destroy(currentHeroModel);

        if (heroes.Length > 0 && heroes[index].shopModelPrefab != null)
        {
            currentHeroModel = Instantiate(heroes[index].shopModelPrefab, position, heroPedestalPos.rotation);
            Animator anim = currentHeroModel.GetComponent<Animator>();
            if (anim != null) { anim.SetBool("IsGrounded", true); anim.SetFloat("Speed", 0f); }

            EquipWeaponToHero(currentWeaponIndex);
        }
    }

    private void EquipWeaponToHero(int wepIndex)
    {
        if (currentWeaponModel != null) Destroy(currentWeaponModel);
        if (currentHeroModel == null || weapons.Length <= wepIndex || weapons[wepIndex].shopPrefab == null) return;

        Transform socket = FindDeepChild(currentHeroModel.transform, "handslot.r");
        if (socket != null)
        {
            currentWeaponModel = Instantiate(weapons[wepIndex].shopPrefab, socket.position, socket.rotation, socket);

            MonoBehaviour[] scripts = currentWeaponModel.GetComponents<MonoBehaviour>();
            foreach (var script in scripts) { if (script != null) script.enabled = false; }

            StartCoroutine(PopWeaponScale(currentWeaponModel.transform));
        }
    }

    private IEnumerator PopWeaponScale(Transform wepTransform)
    {
        Vector3 finalScale = wepTransform.localScale;
        wepTransform.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 6f;
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
            wepTransform.localScale = Vector3.Lerp(Vector3.zero, finalScale * 1.2f, curve);
            yield return null;
        }
        wepTransform.localScale = finalScale;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent) { if (child.name == name) return child; Transform result = FindDeepChild(child, name); if (result != null) return result; }
        return null;
    }

    private void ApplyUITheme()
    {
        if (powerLabel) powerLabel.text = "POWER";
        if (powerFill) powerFill.color = powerColor;

        if (currentMode == ShopMode.Heroes)
        {
            if (stat1Label) stat1Label.text = "HP";
            if (stat2Label) stat2Label.text = "SPEED";
            if (stat3Label) stat3Label.text = "RADIUS";

            if (stat1Fill) stat1Fill.color = heroStat1Color;
            if (stat2Fill) stat2Fill.color = heroStat2Color;
            if (stat3Fill) stat3Fill.color = heroStat3Color;
        }
        else
        {
            if (stat1Label) stat1Label.text = "DAMAGE";
            if (stat2Label) stat2Label.text = "ATK SPEED";
            if (stat3Label) stat3Label.text = "CRIT";

            if (stat1Fill) stat1Fill.color = wepStat1Color;
            if (stat2Fill) stat2Fill.color = wepStat2Color;
            if (stat3Fill) stat3Fill.color = wepStat3Color;
        }
    }

    public void NextHero()
    {
        if (isSwapping || currentMode != ShopMode.Heroes) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        if (heroes.Length <= 1) return;

        int oldIndex = currentHeroIndex;
        currentHeroIndex = (currentHeroIndex + 1) % heroes.Length;
        StartCoroutine(SwapHeroAnimation(oldIndex, currentHeroIndex, false));
    }

    public void PreviousHero()
    {
        if (isSwapping || currentMode != ShopMode.Heroes) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        if (heroes.Length <= 1) return;

        int oldIndex = currentHeroIndex;
        currentHeroIndex = (currentHeroIndex - 1 + heroes.Length) % heroes.Length;
        StartCoroutine(SwapHeroAnimation(oldIndex, currentHeroIndex, true));
    }

    private IEnumerator SwapHeroAnimation(int oldIndex, int newIndex, bool swipeRight)
    {
        isSwapping = true;
        GameObject oldModel = currentHeroModel;

        Vector3 spawnPos = heroPedestalPos.position;
        Vector3 oldTargetPos = swipeRight ? offscreenRight.position : offscreenLeft.position;
        Vector3 newStartPos = swipeRight ? offscreenLeft.position : offscreenRight.position;
        oldTargetPos.y = spawnPos.y; newStartPos.y = spawnPos.y;

        SpawnHero(newIndex, newStartPos);
        UpdateUI(true);

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * swipeSpeed;
            float t = Mathf.Clamp01(progress);

            // --- НОВЕ: AAA Криві Безьє (Відскок та зменшення) ---
            float c1 = 1.70158f;
            float c3 = c1 + 1f;

            // Від'їжджає з прискоренням (Ease In Back)
            float easeInBack = c3 * t * t * t - c1 * t * t;
            // Приїжджає з відскоком (Ease Out Back)
            float easeOutBack = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            if (oldModel != null)
            {
                oldModel.transform.position = Vector3.LerpUnclamped(spawnPos, oldTargetPos, easeInBack);
                oldModel.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.7f, t); // Герой трохи "стискається" коли тікає
            }

            if (currentHeroModel != null)
            {
                currentHeroModel.transform.position = Vector3.LerpUnclamped(newStartPos, spawnPos, easeOutBack);
            }

            yield return null;
        }

        if (currentHeroModel != null) currentHeroModel.transform.position = spawnPos;
        if (oldModel != null) Destroy(oldModel);
        isSwapping = false;
    }

    public void OnBuyOrSelectPressed()
    {
        if (isSwapping || isFading) return;

        int id = 0; string unlockKey = ""; string selectKey = ""; int price = 0;

        if (currentMode == ShopMode.Heroes)
        {
            id = heroes[currentHeroIndex].heroID; price = heroes[currentHeroIndex].price;
            unlockKey = "HeroUnlocked_" + id; selectKey = SELECTED_HERO_KEY;
        }
        else
        {
            if (selectedWeaponData == null) return;
            id = selectedWeaponData.weaponID; price = selectedWeaponData.price;
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
        if (isSwapping || isFading || currentMode != ShopMode.Weapons || selectedWeaponData == null) return;

        int id = selectedWeaponData.weaponID;
        int level = PlayerPrefs.GetInt("WeaponLevel_" + id, 0);

        if (level < selectedWeaponData.maxUpgradeLevel)
        {
            int upgCost = selectedWeaponData.GetUpgradeCost(level);
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
        if (diamondBalanceText != null) diamondBalanceText.text = "Diamonds: " + myDiamonds.ToString("N0");

        int price = 0; int id = 0; string unlockKey = ""; string selectKey = ""; string itemName = "";
        float stat1FillVal = 0, stat2FillVal = 0, stat3FillVal = 0, powerFillVal = 0;

        int displayPower = 0; // <--- ДОДАНО: Змінна для зберігання чистого числа Power

        // --- ФІКС: Ховаємо кнопку Equip/Buy на екрані сітки категорій ---
        if (currentMode == ShopMode.Weapons && !isViewingWeaponCategory)
        {
            if (buyButton) buyButton.gameObject.SetActive(false);
            if (upgradeButton) upgradeButton.gameObject.SetActive(false);
        }
        else
        {
            if (buyButton) buyButton.gameObject.SetActive(true);
        }

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
            displayPower = h.basePower; // <--- ДОДАНО: Зберігаємо число для героя

            if (upgradeButton) upgradeButton.gameObject.SetActive(false);
            if (descriptionItemIcon) descriptionItemIcon.gameObject.SetActive(false);
        }
        else
        {
            if (selectedWeaponData == null) return;
            WeaponData w = selectedWeaponData;
            id = w.weaponID; price = w.price; unlockKey = "WeaponUnlocked_" + id; selectKey = SELECTED_WEP_KEY;

            int level = PlayerPrefs.GetInt("WeaponLevel_" + id, 0);
            itemName = w.weaponName + (level > 0 ? $" <size=70%><color=#AAAAAA>(Lv. {level}/{w.maxUpgradeLevel})</color></size>" : "");

            if (itemDescriptionText) itemDescriptionText.text = w.description;

            if (descriptionItemIcon)
            {
                descriptionItemIcon.gameObject.SetActive(true);
                descriptionItemIcon.sprite = w.icon;
            }

            float currDmg = w.damageBonus + (level * w.damagePerLevel);
            float currSpd = w.attackSpeed + (level * w.attackSpeedPerLevel);
            float currCrit = w.critChance + (level * w.critChancePerLevel);
            int currPower = w.basePower + (level * w.powerPerLevel);

            float maxGameDmg = 400f; float maxGameAtkSpd = 3.0f; float maxGameCrit = 1.0f; float maxGamePower = 1000f;

            stat1FillVal = Mathf.Clamp01(currDmg / maxGameDmg);
            stat2FillVal = Mathf.Clamp01(currSpd / maxGameAtkSpd);
            stat3FillVal = Mathf.Clamp01(currCrit / maxGameCrit);

            powerFillVal = Mathf.Clamp01(currPower / maxGamePower);
            displayPower = currPower; // <--- ДОДАНО: Зберігаємо число для зброї з урахуванням рівня

            bool wIsBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;
            if (wIsBought)
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);

                if (level < w.maxUpgradeLevel)
                {
                    int upgCost = w.GetUpgradeCost(level);
                    if (upgradePriceText != null)
                    {
                        upgradePriceText.text = "Upgrade for " + upgCost.ToString("N0");
                        upgradePriceText.color = (myDiamonds >= upgCost) ? textNormalColor : textErrorColor;
                    }
                    if (upgradeButton != null) upgradeButton.interactable = (myDiamonds >= upgCost);
                }
                else
                {
                    if (upgradePriceText != null)
                    {
                        upgradePriceText.text = "MAX";
                        upgradePriceText.color = new Color(1f, 0.8f, 0.2f);
                    }
                    if (upgradeButton != null) upgradeButton.interactable = false;
                }
            }
            else
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            }
        }

        bool isBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;
        bool isSelected = PlayerPrefs.GetInt(selectKey, 0) == id;

        if (!isBought)
        {
            priceText.text = price.ToString("N0");
            if (buyButton) buyButton.interactable = (myDiamonds >= price);
            priceText.color = (myDiamonds >= price) ? textNormalColor : textErrorColor;
        }
        else if (!isSelected)
        {
            priceText.text = "EQUIP";
            if (buyButton) buyButton.interactable = true;
            priceText.color = textNormalColor;
        }
        else
        {
            priceText.text = "EQUIPPED";
            if (buyButton) buyButton.interactable = false;
            priceText.color = textSuccessColor;
        }

        if (itemNameText) itemNameText.text = itemName;

        if (stat1PercentText) stat1PercentText.text = Mathf.RoundToInt(stat1FillVal * 100) + "%";
        if (stat2PercentText) stat2PercentText.text = Mathf.RoundToInt(stat2FillVal * 100) + "%";
        if (stat3PercentText) stat3PercentText.text = Mathf.RoundToInt(stat3FillVal * 100) + "%";

        // --- ФІКС: Виводимо чисте число замість відсотків ---
        if (powerPercentText) powerPercentText.text = displayPower.ToString();

        if (stat1Fill) stat1Fill.fillAmount = stat1FillVal;
        if (stat2Fill) stat2Fill.fillAmount = stat2FillVal;
        if (stat3Fill) stat3Fill.fillAmount = stat3FillVal;
        if (powerFill) powerFill.fillAmount = powerFillVal;

        if (animateText)
        {
            StartCoroutine(PopText(itemNameText)); StartCoroutine(PopText(priceText));
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

        int finalTotalPower = Mathf.RoundToInt(baseTotal == 0 ? 50 : baseTotal);
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

        SetGroupAlpha(statsPanelGroup, 0);
        SetGroupAlpha(arsenalGridGroup, 0);
        SetGroupAlpha(arsenalContentGroup, 0);
        SetGroupAlpha(arsenalDescriptionGroup, 0);

        if (LoadingManager.Instance != null) LoadingManager.Instance.LoadScene(campSceneName);
        else if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(campSceneName);
        else SceneManager.LoadScene(campSceneName);
    }
}