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
    public float sceneFadeSpeed = 1.5f;
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
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameObject mapCanvas = GameObject.Find("MapCanvas");
            if (mapCanvas != null && mapCanvas.activeInHierarchy) return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "GameScene" || sceneName == "CampScene") TogglePause();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(SyncCameraAndVolumeRoutine());

        bool showGameplayUI = (scene.name != "Menu");
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
        if (isPaused) TogglePause();
    }

    private IEnumerator SyncCameraAndVolumeRoutine()
    {
        yield return null;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = defaultRenderMode;
            canvas.sortingOrder = 0;

            if (defaultRenderMode == RenderMode.ScreenSpaceCamera)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = FindFirstObjectByType<Camera>();
                canvas.worldCamera = cam;
            }
        }

        Volume volume = FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet(out dofEffect)) dofEffect.active = false;
        }
        else dofEffect = null;
    }

    public void FadeAndLoadScene(string sceneName)
    {
        if (isPaused) TogglePause();
        if (loadingPanelGroup != null) StartCoroutine(LoadSceneAsyncRoutine(sceneName));
        else SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneToLoad)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
        }

        loadingPanelGroup.gameObject.SetActive(true);
        loadingPanelGroup.transform.SetAsLastSibling();

        if (hintCycleCoroutine != null) StopCoroutine(hintCycleCoroutine);
        hintCycleCoroutine = StartCoroutine(CycleHintsRoutine());

        while (loadingPanelGroup.alpha < 1f)
        {
            loadingPanelGroup.alpha += Time.unscaledDeltaTime * sceneFadeSpeed;
            yield return null;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;
        float visualProgress = 0f;

        while (!asyncLoad.isDone)
        {
            float targetProgress = asyncLoad.progress / 0.9f;
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.unscaledDeltaTime * 2.5f);

            if (loadingSlider != null) loadingSlider.value = visualProgress;
            if (loadingText != null) loadingText.text = $"LOADING... {Mathf.FloorToInt(visualProgress * 100)}%";

            if (asyncLoad.progress >= 0.9f && visualProgress >= 0.99f)
            {
                if (loadingSlider != null) loadingSlider.value = 1f;
                if (loadingText != null) loadingText.text = "LOADING... 100%";

                yield return new WaitForSecondsRealtime(0.15f);
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private IEnumerator FadeOutLoadingScreen()
    {
        loadingPanelGroup.alpha = 1f;
        while (loadingPanelGroup.alpha > 0f)
        {
            loadingPanelGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
            yield return null;
        }
        loadingPanelGroup.gameObject.SetActive(false);
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

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        if (dofEffect != null) dofEffect.active = isPaused;

        if (isPaused)
        {
            ResetGiveUpState();
            StartCoroutine(ShowMenuRoutine());
        }
        else StartCoroutine(HideMenuRoutine());

        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
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

        if (!isConfirmingGiveUp)
        {
            isConfirmingGiveUp = true;
            if (giveUpText != null) giveUpText.text = "You sure?\nAll journey progress will be lost";

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
            if (ResourceManager.Instance != null) ResourceManager.Instance.ClearRunInventory();
            FadeAndLoadScene("CampScene");
        }
    }

    private void ResetGiveUpState()
    {
        isConfirmingGiveUp = false;
        if (giveUpText != null) giveUpText.text = "Give Up";

        foreach (var btn in pauseButtonGroups)
        {
            if (btn != null)
            {
                btn.alpha = 1f;
                btn.interactable = true;
            }
        }
    }

    public void SetLevelObjective(string message)
    {
        if (objectiveText != null) objectiveText.text = message;
        if (objectivePanelGroup != null) objectivePanelGroup.alpha = 1f;
    }

    public void HideLevelObjective()
    {
        if (objectivePanelGroup != null) objectivePanelGroup.alpha = 0f;
    }
}