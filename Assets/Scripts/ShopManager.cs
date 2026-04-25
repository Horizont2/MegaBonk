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
    private Coroutine uiMoveCoroutine; // <-- «бер≥гаЇмо “≤Ћ№ » рух панел≥, щоб не вбивати ≥нш≥ процеси

    [Header("Inspect Feature")]
    public CameraTransitionManager camManager;
    public Camera mainCamera;                  // <-- ƒќƒјЌќ: ѕерет€гни сюди ShopCamera з≥ сцени!
    public GameObject inspectButton;
    private bool isInspectingWeapon = false;

    [Space(10)]
    public TextMeshProUGUI stat1Label, stat2Label, stat3Label;
    public Image stat1Icon, stat2Icon, stat3Icon;
    public Sprite heroStat1Sprite, heroStat2Sprite, heroStat3Sprite;
    public Sprite wepStat1Sprite, wepStat2Sprite, wepStat3Sprite;

    [Space(10)]
    public Image stat1Fill, stat2Fill, stat3Fill;
    public Color heroStat1Color = new Color(1f, 0.2f, 0.2f);
    public Color heroStat2Color = new Color(0.2f, 0.6f, 1f);
    public Color heroStat3Color = new Color(0.6f, 0.2f, 1f);
    public Color wepStat1Color = new Color(1f, 0.5f, 0f);
    public Color wepStat2Color = new Color(1f, 0.9f, 0.1f);
    public Color wepStat3Color = new Color(0.1f, 1f, 0.8f);

    [Header("Animation Settings")]
    public float swipeSpeed = 4f;
    public float rotationSpeed = 500f;

    [Header("Scene Navigation")]
    public string mainMenuSceneName = "MainMenu";

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

    [Header("UI Sliders & Stats")]
    public Slider stat1Slider, stat2Slider, stat3Slider;
    public TextMeshProUGUI stat1ValueText, stat2ValueText, stat3ValueText;
    public TextMeshProUGUI stat1PercentText, stat2PercentText, stat3PercentText;

    [Header("Events (For Cameras)")]
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
        if (backButton != null) backButton.onClick.AddListener(GoToMainMenu);
    }

    private void Update()
    {
        HandleManualRotation();
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextItem();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousItem();

        // ¬»ѕ–ј¬Ћ≈Ќј Ћќ√≤ ј «ј –»““я ќ√Ћяƒ”  Ћ≤ ќћ ѕќ ‘ќЌ” јЅќ —“≤Ќј’
        if (isInspectingWeapon && Input.GetMouseButtonDown(0))
        {
            // якщо кл≥кнули по кнопках (UI), ≥гноруЇмо
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // якщо ми влучили в €кийсь 3D об'Їкт на сцен≥ (ст≥на, ст≥л, збро€)
                if (Physics.Raycast(ray, out hit))
                {
                    // ѕерев≥р€Їмо, чи це ЌјЎј «Ѕ–ќя. якщо н≥ - закриваЇмо огл€д!
                    if (hit.collider.GetComponent<WeaponDisplayObject>() == null)
                    {
                        StopInspect();
                    }
                }
                else
                {
                    // якщо влучили взагал≥ в пустоту
                    StopInspect();
                }
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
        isInspectingWeapon = true;

        if (camManager != null) camManager.GoToInspect();
        if (currentModel != null)
        {
            WeaponDisplayObject display = currentModel.GetComponent<WeaponDisplayObject>();
            if (display != null) display.SetInspect(true);
        }

        // Ѕ≥льше не вбиваЇмо вс≥ корутини! «апускаЇмо т≥льки UI
        if (uiMoveCoroutine != null) StopCoroutine(uiMoveCoroutine);
        uiMoveCoroutine = StartCoroutine(MoveUIPanel(panelHidePositionX));

        if (inspectButton != null) inspectButton.SetActive(false);
    }

    public void StopInspect()
    {
        if (!isInspectingWeapon) return;
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
        if (isTransitioningUI) return;
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

        if (isInspectingWeapon)
        {
            StartCoroutine(StopInspectAndSwap(true));
            return;
        }
        ExecuteSwapNext();
    }

    public void PreviousItem()
    {
        if (isSwapping || isTransitioningUI) return;

        if (isInspectingWeapon)
        {
            StartCoroutine(StopInspectAndSwap(false));
            return;
        }
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

        int price = 0; int id = 0; string unlockKey = ""; string selectKey = "";

        if (currentMode == ShopMode.Heroes)
        {
            price = heroes[currentHeroIndex].price; id = heroes[currentHeroIndex].heroID;
            unlockKey = "HeroUnlocked_" + id; selectKey = SELECTED_HERO_KEY;
        }
        else
        {
            price = weapons[currentWeaponIndex].price; id = weapons[currentWeaponIndex].weaponID;
            unlockKey = "WeaponUnlocked_" + id; selectKey = SELECTED_WEP_KEY;
        }

        bool isBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;

        if (!isBought)
        {
            int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY);
            if (myDiamonds >= price)
            {
                PlayerPrefs.SetInt(DIAMONDS_KEY, myDiamonds - price);
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.SetInt(selectKey, id);
                PlayerPrefs.Save();
                UpdateUI(true);
            }
        }
        else
        {
            PlayerPrefs.SetInt(selectKey, id);
            PlayerPrefs.Save();
            UpdateUI(true);
        }
    }

    private void UpdateUI(bool animateText)
    {
        int myDiamonds = PlayerPrefs.GetInt(DIAMONDS_KEY, 0);
        if (diamondBalanceText != null) diamondBalanceText.text = myDiamonds.ToString("N0");

        int price = 0; int id = 0; string unlockKey = ""; string selectKey = ""; string itemName = "";
        float stat1FillVal = 0, stat2FillVal = 0, stat3FillVal = 0;
        string stat1Str = "", stat2Str = "", stat3Str = "";

        if (currentMode == ShopMode.Heroes)
        {
            if (heroes.Length == 0) return;
            HeroData h = heroes[currentHeroIndex];
            price = h.price; id = h.heroID; unlockKey = "HeroUnlocked_" + id; selectKey = SELECTED_HERO_KEY; itemName = h.heroName;

            stat1FillVal = h.hpBarFill; stat2FillVal = h.speedBarFill; stat3FillVal = h.radiusBarFill;
            stat1Str = (h.actualMaxHealth * stat1FillVal).ToString("F0") + "/" + h.actualMaxHealth.ToString("F0");
            stat2Str = h.actualMoveSpeed.ToString("F1") + " m/s";
            stat3Str = h.actualBombRadius.ToString("F0") + "m";
        }
        else
        {
            if (weapons.Length == 0) return;
            WeaponData w = weapons[currentWeaponIndex];
            price = w.price; id = w.weaponID; unlockKey = "WeaponUnlocked_" + id; selectKey = SELECTED_WEP_KEY; itemName = w.weaponName;

            stat1FillVal = Mathf.Clamp01(w.damageBonus / 50f);
            stat2FillVal = Mathf.Clamp01(w.attackSpeed / 3f);
            stat3FillVal = Mathf.Clamp01(w.critChance);

            stat1Str = "+" + w.damageBonus.ToString("F0");
            stat2Str = w.attackSpeed.ToString("F2");
            stat3Str = Mathf.RoundToInt(w.critChance * 100) + "%";
        }

        bool isBought = PlayerPrefs.GetInt(unlockKey, price == 0 ? 1 : 0) == 1;
        bool isSelected = PlayerPrefs.GetInt(selectKey, 0) == id;

        if (isSelected)
        {
            priceText.text = "EQUIPPED"; priceText.alignment = TextAlignmentOptions.Center; priceText.margin = new Vector4(0, 0, 0, 0);
            priceText.color = Color.white;
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);
            buyButton.interactable = false;
        }
        else if (isBought)
        {
            priceText.text = "SELECT"; priceText.alignment = TextAlignmentOptions.Center; priceText.margin = new Vector4(0, 0, 0, 0);
            priceText.color = new Color(0.6f, 1f, 0.2f);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(false);
            buyButton.interactable = true;
        }
        else
        {
            priceText.text = "BUY FOR\n" + price.ToString("N0"); priceText.alignment = TextAlignmentOptions.Left; priceText.margin = new Vector4(10, 0, 0, 0);
            if (diamondIconOnButton != null) diamondIconOnButton.SetActive(true);

            if (myDiamonds >= price) { buyButton.interactable = true; priceText.color = Color.white; }
            else { buyButton.interactable = false; priceText.color = new Color(1f, 0.2f, 0.2f); }
        }

        if (itemNameText != null) itemNameText.text = itemName;
        if (stat1ValueText != null) stat1ValueText.text = stat1Str;
        if (stat2ValueText != null) stat2ValueText.text = stat2Str;
        if (stat3ValueText != null) stat3ValueText.text = stat3Str;

        if (stat1PercentText != null) stat1PercentText.text = Mathf.RoundToInt(stat1FillVal * 100) + "%";
        if (stat2PercentText != null) stat2PercentText.text = Mathf.RoundToInt(stat2FillVal * 100) + "%";
        if (stat3PercentText != null) stat3PercentText.text = Mathf.RoundToInt(stat3FillVal * 100) + "%";

        if (stat1Slider != null) stat1Slider.value = stat1FillVal;
        if (stat2Slider != null) stat2Slider.value = stat2FillVal;
        if (stat3Slider != null) stat3Slider.value = stat3FillVal;

        if (animateText)
        {
            StartCoroutine(PopText(itemNameText)); StartCoroutine(PopText(priceText));
            StartCoroutine(PopText(stat1ValueText)); StartCoroutine(PopText(stat2ValueText)); StartCoroutine(PopText(stat3ValueText));
        }
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

    public void GoToMainMenu() { SceneManager.LoadScene(mainMenuSceneName); }
}