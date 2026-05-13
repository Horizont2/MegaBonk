using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalHUD : MonoBehaviour
{
    public static GlobalHUD Instance;

    [Header("Visibility Control")]
    public GameObject[] gameplayPanels;

    [Header("Interaction Prompt")]
    public CanvasGroup promptCanvasGroup;
    public TextMeshProUGUI promptText;
    public float promptFadeSpeed = 5f;
    public float typingSpeed = 0.03f;

    [Header("Level Objective UI")]
    public CanvasGroup objectivePanelGroup;
    public TextMeshProUGUI objectiveText;

    [Header("Scene Transition & Loading")]
    private bool isLoading = false;
    public float sceneFadeSpeed = 1.5f;
    public CanvasGroup blackFadeGroup;
    public CanvasGroup loadingPanelGroup;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI hintText;
    public float hintChangeInterval = 10f;

    [TextArea(2, 3)]
    public string[] gameHints = new string[]
    {
        "Upgrade your Storage Vaults in the Camp to increase your maximum Stash capacity.",
        "Use your melee attacks to gather Wood, Stone, and Food from trees and rocks during a run.",
        "Keep an eye on your Stack. Too many enemies nearby will start draining your health!",
        "Excess resources are converted into Diamonds at the end of a run. Use them for meta-upgrades.",
        "The Extraction Point (the Horse) is your only way to safely bring loot back to the Camp.",
        "Giving up during a journey will result in the loss of all resources gathered during that run.",
        "You can hold 'E' near Camp Buildings to build or upgrade them if you have enough Stash resources.",
        "Check the Notice Board in the Camp frequently for new missions and extra rewards.",
        "Hold the Right Mouse Button to aim and charge your grenade throw for massive area damage.",
        "Permanent upgrades bought with Diamonds persist even if you fail your journey.",
        "Use Dash (Shift) to escape dangerous situations when your Stack is getting too high.",
        "Higher Stack counts increase your damage multiplier but risk critical overheating.",
        "Struggling to survive? Invest in Health and Armor meta-upgrades in the Shop.",
        "Some structures in the Camp produce resources automatically over time. Upgrade them for more yield!",
        "Experiment with different heroes and weapons in the Main Menu to find your favorite playstyle."
    };

    [Header("Pause Menu Settings")]
    public CanvasGroup pausePanelGroup;
    public GameObject[] pauseButtons;
    public CanvasGroup[] pauseButtonGroups;
    public CanvasGroup giveUpButtonGroup;
    public TextMeshProUGUI giveUpText;
    public float buttonDelay = 0.05f;

    private bool isPaused = false;
    private bool isConfirmingGiveUp = false;
    private DepthOfField dofEffect;

    private Coroutine promptFadeCoroutine;
    private Coroutine promptTypingCoroutine;
    private Coroutine hintTypingCoroutine;
    private Coroutine hintCycleCoroutine;

    private RenderMode defaultRenderMode;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) defaultRenderMode = canvas.renderMode;

        if (loadingPanelGroup != null)
        {
            loadingPanelGroup.gameObject.SetActive(false);
            loadingPanelGroup.alpha = 0f;
        }

        // НОВЕ: Ховаємо чорний екран на старті
        if (blackFadeGroup != null)
        {
            blackFadeGroup.gameObject.SetActive(false);
            blackFadeGroup.alpha = 0f;
        }
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 0. ОГЛЯД ЗБРОЇ
            ShopManager shop = FindFirstObjectByType<ShopManager>();
            if (shop != null && shop.IsInspecting())
            {
                shop.StopInspect();
                return;
            }

            // 1. НАЛАШТУВАННЯ
            if (SettingsUI.Instance != null && SettingsUI.Instance.settingsPanel.activeInHierarchy)
            {
                SettingsUI.Instance.CloseSettings();
                return;
            }

            // 1.5 ДОШКА МІСІЙ
            // Ми закриваємо її тут, і метод CloseBoard() сам сховає курсор
            NoticeBoardManager noticeBoard = FindFirstObjectByType<NoticeBoardManager>();
            if (noticeBoard != null && noticeBoard.isBoardOpen)
            {
                noticeBoard.CloseBoard();
                return; // Виходимо, щоб не відкрилася пауза
            }

            // 2. МАПА
            if (MapTableInteract.IsMapActive) return;

            MapPanelUI mapPanel = FindFirstObjectByType<MapPanelUI>();
            if (mapPanel != null && mapPanel.IsPanelOpen()) return;

            // 3. ПАУЗА (Додав Lvl_1 у список)
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "GameScene" || sceneName == "CampScene" || sceneName == "ShopScene" || sceneName == "Lvl_1")
            {
                TogglePause();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(SyncCameraAndVolumeRoutine());

        bool showGameplayUI = (scene.name != "Menu" && scene.name != "ShopScene");
        if (gameplayPanels != null)
        {
            foreach (GameObject panel in gameplayPanels)
            {
                if (panel != null) panel.SetActive(showGameplayUI);
            }
        }

        if (loadingPanelGroup != null && loadingPanelGroup.gameObject.activeSelf)
        {
            if (hintCycleCoroutine != null) StopCoroutine(hintCycleCoroutine);
            StartCoroutine(FadeOutLoadingScreen());
        }

        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;

        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanelGroup != null)
            {
                pausePanelGroup.alpha = 0f;
                pausePanelGroup.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator SyncCameraAndVolumeRoutine()
    {
        yield return null;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = defaultRenderMode;
            canvas.sortingOrder = 50;

            if (defaultRenderMode == RenderMode.ScreenSpaceCamera)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = FindFirstObjectByType<Camera>();
                canvas.worldCamera = cam;
            }
        }

        // --- ФІКС БЛЮРУ: Тепер він не зникатиме в магазині ---
        Volume[] allVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (Volume v in allVolumes)
        {
            if (v.isGlobal && v.profile != null)
            {
                if (v.profile.TryGet(out dofEffect))
                {
                    bool isShop = SceneManager.GetActiveScene().name == "ShopScene";
                    dofEffect.active = isShop || isPaused;
                    break;
                }
            }
        }
    }  

    public void FadeAndLoadScene(string sceneName)
    {
        // ФІКС: Блокуємо повторне натискання, щоб завантаження не "перезапускалося"
        if (isLoading) return;

        if (isPaused) TogglePause();
        StartCoroutine(LoadSceneAsyncRoutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneToLoad)
    {
        isLoading = true; // Починаємо завантаження

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        // 1. Плавне згасання в ТЕМРЯВУ
        if (blackFadeGroup != null)
        {
            blackFadeGroup.gameObject.SetActive(true);
            blackFadeGroup.transform.SetAsLastSibling(); // ФІКС: Робимо чорний екран поверх усієї гри
            blackFadeGroup.alpha = 0f;
            while (blackFadeGroup.alpha < 1f)
            {
                blackFadeGroup.alpha += Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
        }

        // 2. Плавне з'явлення інтерфейсу завантаження
        if (loadingPanelGroup != null)
        {
            loadingSlider.value = 0f;
            loadingPanelGroup.gameObject.SetActive(true);
            loadingPanelGroup.transform.SetAsLastSibling(); // ФІКС: Тепер панель завантаження стає ПОВЕРХ чорного екрану!
            loadingPanelGroup.alpha = 0f;

            if (hintCycleCoroutine != null) StopCoroutine(hintCycleCoroutine);
            if (hintTypingCoroutine != null) StopCoroutine(hintTypingCoroutine);
            hintText.text = "";

            while (loadingPanelGroup.alpha < 1f)
            {
                loadingPanelGroup.alpha += Time.unscaledDeltaTime * sceneFadeSpeed * 2f;
                yield return null;
            }
        }

        hintCycleCoroutine = StartCoroutine(CycleHintsRoutine());

        // 3. Асинхронне завантаження
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        float visualProgress = 0f;

        while (!asyncLoad.isDone)
        {
            float targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.unscaledDeltaTime * 0.5f);

            if (loadingSlider != null) loadingSlider.value = visualProgress;
            if (loadingText != null) loadingText.text = $"LOADING... {Mathf.FloorToInt(visualProgress * 100)}%";

            if (asyncLoad.progress >= 0.9f && visualProgress >= 0.99f)
            {
                visualProgress = 1f;
                if (loadingSlider != null) loadingSlider.value = 1f;
                if (loadingText != null) loadingText.text = "READY";

                yield return new WaitForSecondsRealtime(0.5f);
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private IEnumerator FadeOutLoadingScreen()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        // Ховаємо інтерфейс завантаження
        if (loadingPanelGroup != null)
        {
            while (loadingPanelGroup.alpha > 0f)
            {
                loadingPanelGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed * 2f;
                yield return null;
            }
            loadingPanelGroup.gameObject.SetActive(false);
        }

        // Розтухає чорний екран, відкриваючи нову сцену
        if (blackFadeGroup != null)
        {
            blackFadeGroup.transform.SetAsLastSibling(); // На всякий випадок закріплюємо сортування
            while (blackFadeGroup.alpha > 0f)
            {
                blackFadeGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
            blackFadeGroup.gameObject.SetActive(false);
        }

        isLoading = false; // Дозволяємо нові завантаження
    }

    private IEnumerator CycleHintsRoutine()
    {
        while (true)
        {
            if (gameHints.Length > 0 && hintText != null)
            {
                string randomHint = gameHints[Random.Range(0, gameHints.Length)];
                if (hintTypingCoroutine != null) StopCoroutine(hintTypingCoroutine);
                hintTypingCoroutine = StartCoroutine(TypeTextRoutine(randomHint, hintText));
            }
            yield return new WaitForSecondsRealtime(hintChangeInterval);
        }
    }

    private IEnumerator TypeTextRoutine(string message, TextMeshProUGUI textTarget)
    {
        if (textTarget == null) yield break;

        textTarget.text = message;
        textTarget.ForceMeshUpdate();
        int totalChars = textTarget.textInfo.characterCount;
        textTarget.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalChars; i++)
        {
            textTarget.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        textTarget.maxVisibleCharacters = 99999;
    }

    public void ShowPrompt(string message)
    {
        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
        if (promptTypingCoroutine != null) StopCoroutine(promptTypingCoroutine);

        promptFadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
        promptTypingCoroutine = StartCoroutine(TypeTextRoutine(message, promptText));
    }

    public void HidePrompt()
    {
        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
        if (promptTypingCoroutine != null) StopCoroutine(promptTypingCoroutine);

        promptFadeCoroutine = StartCoroutine(FadeCanvasGroup(0f));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        if (promptCanvasGroup == null) yield break;
        while (Mathf.Abs(promptCanvasGroup.alpha - targetAlpha) > 0.01f)
        {
            promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * promptFadeSpeed);
            yield return null;
        }
        promptCanvasGroup.alpha = targetAlpha;
    }

    public void SetGameplayPanelsActive(bool active)
    {
        if (gameplayPanels != null)
        {
            foreach (GameObject panel in gameplayPanels)
            {
                if (panel != null) panel.SetActive(active);
            }
        }
    }

    public void TogglePause()
    {
        if (pausePanelGroup == null) return;

        // --- ФІКС: Якщо ми знімаємо з паузи (Continue), але налаштування відкриті - закриваємо їх ---
        if (isPaused && SettingsUI.Instance != null && SettingsUI.Instance.settingsPanel.activeInHierarchy)
        {
            SettingsUI.Instance.CloseSettings();
        }

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        if (dofEffect != null)
        {
            bool isShop = SceneManager.GetActiveScene().name == "ShopScene";
            dofEffect.active = isShop || isPaused;
        }

        if (isPaused)
        {
            ResetGiveUpState();
            StartCoroutine(ShowMenuRoutine());
        }
        else StartCoroutine(HideMenuRoutine());

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "ShopScene" || currentScene == "Menu")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private IEnumerator ShowMenuRoutine()
    {
        pausePanelGroup.gameObject.SetActive(true);
        pausePanelGroup.blocksRaycasts = true;
        pausePanelGroup.interactable = true;

        bool inGame = SceneManager.GetActiveScene().name == "GameScene";
        if (giveUpButtonGroup != null) giveUpButtonGroup.gameObject.SetActive(inGame);

        foreach (var btn in pauseButtons) if (btn != null) btn.GetComponent<RectTransform>().localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 6f;
            pausePanelGroup.alpha = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        pausePanelGroup.alpha = 1f;

        foreach (var btn in pauseButtons)
        {
            if (btn != null && btn.activeSelf)
            {
                StartCoroutine(AnimateButtonIn(btn.GetComponent<RectTransform>()));
                yield return new WaitForSecondsRealtime(buttonDelay);
            }
        }
    }

    private IEnumerator AnimateButtonIn(RectTransform btn)
    {
        Vector3 targetScale = Vector3.one;
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 5f;
            float s = Mathf.Sin(t * Mathf.PI * 0.5f + 0.2f) * 1.15f;
            btn.localScale = new Vector3(s, s, s);
            yield return null;
        }
        btn.localScale = targetScale;
    }

    private IEnumerator HideMenuRoutine()
    {
        pausePanelGroup.interactable = false;
        pausePanelGroup.blocksRaycasts = false;

        float t = pausePanelGroup.alpha;
        while (t > 0)
        {
            t -= Time.unscaledDeltaTime * 10f;
            pausePanelGroup.alpha = t;
            yield return null;
        }
        pausePanelGroup.alpha = 0f;
        pausePanelGroup.gameObject.SetActive(false);
    }

    public void OnGiveUpClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        string currentScene = SceneManager.GetActiveScene().name;

        if (!isConfirmingGiveUp)
        {
            isConfirmingGiveUp = true;
            if (giveUpText != null) giveUpText.text = (currentScene == "Lvl_1") ? "Skip Tutorial?" : "You sure?\nAll journey progress will be lost";

            foreach (var btn in pauseButtonGroups)
            {
                if (btn != null && btn != giveUpButtonGroup)
                {
                    btn.alpha = 0.3f;
                    btn.interactable = false;
                }
            }
        }
        else
        {
            TogglePause();
            if (currentScene == "Lvl_1")
            {
                FadeAndLoadScene("Menu"); // Повертаємо в меню
            }
            else
            {
                if (ResourceManager.Instance != null) ResourceManager.Instance.ClearRunInventory();
                FadeAndLoadScene("CampScene");
            }
        }
    }

    private void ResetGiveUpState()
    {
        isConfirmingGiveUp = false;
        string currentScene = SceneManager.GetActiveScene().name;
        if (giveUpText != null) giveUpText.text = (currentScene == "Lvl_1") ? "Back to Menu" : "Give Up";

        foreach (var btn in pauseButtonGroups)
        {
            if (btn != null) { btn.alpha = 1f; btn.interactable = true; }
        }
    }

    public void SetLevelObjective(string message)
    {
        if (objectiveText != null) objectiveText.text = message;
        if (objectivePanelGroup != null) objectivePanelGroup.alpha = 1f;
    }

    public void HideLevelObjective()
    {
        if (objectivePanelGroup != null && objectivePanelGroup.alpha > 0)
        {
            StartCoroutine(HideObjectiveRoutine());
        }
    }

    private IEnumerator HideObjectiveRoutine()
    {
        RectTransform rect = objectivePanelGroup.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;

        // Відтяжка вправо (бо панель зліва)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            rect.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(30f, 0), Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }

        // Виліт вліво за екран
        t = 0;
        Vector2 midPos = rect.anchoredPosition;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            rect.anchoredPosition = Vector2.Lerp(midPos, midPos + new Vector2(-600f, 0), t * t * t);
            objectivePanelGroup.alpha = 1f - t;
            yield return null;
        }

        objectivePanelGroup.alpha = 0f;
        rect.anchoredPosition = startPos; // Повертаємо на початкове місце для майбутніх місій
    }
}