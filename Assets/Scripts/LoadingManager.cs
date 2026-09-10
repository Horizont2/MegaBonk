using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup loadingCanvasGroup;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI hintText;
    public CanvasGroup blackFadeGroup;
    public RectTransform loadingSpinner;

    [Header("Dynamic Backgrounds")]
    public Image loadingArt;
    public Sprite[] loadingSprites;

    [Header("Settings")]
    public float sceneFadeSpeed = 1.5f;
    public float hintChangeInterval = 5f;
    public float spinnerRotationSpeed = 150f;

    [TextArea(2, 3)]
    public string[] gameHints;

    public bool isLoading { get; private set; } = false;
    private Coroutine hintCoroutine;

    // What is being loaded, and since when. Needed to tell "a load is genuinely
    // still working" from "the latch was left up by a load that died" — see
    // LoadScene. Nothing else may write these.
    private string loadingSceneName;
    private float loadStartedAt;
    private AsyncOperation pendingLoad;

    // A scene load that has not finished in this long is not slow, it is dead.
    // Generous on purpose: a region load includes full world generation.
    private const float StaleLoadSeconds = 90f;

    // Ceiling on the world-generation wait inside LoadRoutine. Past it the
    // loading screen comes down anyway: a half-built world the player can see
    // and quit from beats a loading screen that never ends.
    private const float GenerationTimeoutSeconds = 120f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadingCanvasGroup != null) { loadingCanvasGroup.alpha = 0f; loadingCanvasGroup.gameObject.SetActive(false); }
            if (blackFadeGroup != null) { blackFadeGroup.alpha = 0f; blackFadeGroup.gameObject.SetActive(false); }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isLoading && loadingSpinner != null)
        {
            loadingSpinner.Rotate(0, 0, -spinnerRotationSpeed * Time.unscaledDeltaTime);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            // Do NOT swallow the request in silence.
            //
            // isLoading used to be cleared only on the very last line of
            // LoadRoutine, so anything that stopped that coroutine short — a
            // null AsyncOperation because the scene was missing from Build
            // Settings, a world generation that never reported done, the
            // coroutine being stopped — left this latched true for the rest of
            // the session. Every later transition then returned right here
            // without so much as a log line. That is how a won region could play
            // its whole victory cinematic, show the title card, and then simply
            // never hand the player back to camp: both the normal
            // FadeAndLoadScene AND the region's own stranded-player watchdog
            // called straight into this early return.
            bool sameTarget = loadingSceneName == sceneName;
            float running = Time.unscaledTime - loadStartedAt;
            if (sameTarget && running < StaleLoadSeconds) return;   // genuinely still working

            Debug.LogWarning($"[LoadingManager] Asked for '{sceneName}' while a load of " +
                             $"'{loadingSceneName}' has been running {running:F1}s. Treating that one as " +
                             "dead and taking over.");

            // Release the old async op before starting another one. It is parked
            // on allowSceneActivation = false, and leaving it parked while a
            // second load starts is what turns a recoverable stall into a hang.
            if (pendingLoad != null) { pendingLoad.allowSceneActivation = true; pendingLoad = null; }
            StopAllCoroutines();
            hintCoroutine = null;
            isLoading = false;
        }

        if (loadingSprites != null && loadingSprites.Length > 0 && loadingArt != null)
        {
            loadingArt.sprite = loadingSprites[Random.Range(0, loadingSprites.Length)];
        }

        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;
        loadingSceneName = sceneName;
        loadStartedAt = Time.unscaledTime;

        // Note: we do NOT flip PlayerController.isControlBlocked here.
        // PlayerController's Update reads LoadingManager.Instance.isLoading
        // directly, so freezing input during a load doesn't collide with
        // whatever the next scene's tutorial/cinematic wants to set.

        // 1. Fade Out (затемнення екрану)
        if (blackFadeGroup != null)
        {
            blackFadeGroup.gameObject.SetActive(true);
            while (blackFadeGroup.alpha < 1f)
            {
                blackFadeGroup.alpha += Time.unscaledDeltaTime * sceneFadeSpeed * 2f;
                yield return null;
            }
        }

        // 2. Показуємо UI завантаження
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 1f;
        }

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        if (gameHints.Length > 0) hintCoroutine = StartCoroutine(HintRoutine());

        Application.backgroundLoadingPriority = ThreadPriority.Low;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            // Almost always a scene missing from Build Settings. Without this
            // check the next line threw, the coroutine died, and isLoading
            // stayed up forever — every transition for the rest of the session
            // silently did nothing, far away from the real cause.
            Debug.LogError($"[LoadingManager] LoadSceneAsync returned null for '{sceneName}'. Is it in Build Settings?");
            if (loadingCanvasGroup != null) { loadingCanvasGroup.alpha = 0f; loadingCanvasGroup.gameObject.SetActive(false); }
            if (blackFadeGroup != null) { blackFadeGroup.alpha = 0f; blackFadeGroup.gameObject.SetActive(false); }
            if (hintCoroutine != null) { StopCoroutine(hintCoroutine); hintCoroutine = null; }
            isLoading = false;
            yield break;
        }
        pendingLoad = asyncLoad;
        asyncLoad.allowSceneActivation = false;

        // Чекаємо готовності сцени на 90%
        while (asyncLoad.progress < 0.9f)
        {
            float rawProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            int displayPercent = Mathf.FloorToInt(rawProgress * 50f);
            if (loadingText != null) loadingText.text = LocalizationManager.Tr("LOADING ASSETS... {0}%", displayPercent);
            yield return null;
        }

        Application.backgroundLoadingPriority = ThreadPriority.High;

        // Активуємо сцену
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;
        pendingLoad = null;

        Application.backgroundLoadingPriority = ThreadPriority.Normal;

        // 3. Чекаємо поки WorldGenerator згенерує світ
        WorldGenerator worldGen = FindFirstObjectByType<WorldGenerator>();
        if (worldGen != null)
        {
            int highestDisplayPercent = 50; // ФІКС 2: Додаємо змінну-пам'ять для відсотків

            // Bounded. An unbounded wait here is not just a long loading screen:
            // isLoading never comes down, so once generation stalls the game can
            // never change scene again for the rest of the session.
            float genDeadline = Time.unscaledTime + GenerationTimeoutSeconds;
            while (!WorldGenerator.IsGenerationDone)
            {
                if (Time.unscaledTime > genDeadline)
                {
                    Debug.LogError($"[LoadingManager] World generation did not finish within " +
                                   $"{GenerationTimeoutSeconds}s in '{sceneName}'. Dropping the loading screen " +
                                   "anyway so the session is not stuck on it.");
                    break;
                }

                int currentRealPercent = Mathf.FloorToInt(50f + (Mathf.Clamp01(WorldGenerator.CurrentProgress) * 50f));

                // Прогрес на екрані може ТІЛЬКИ зростати
                if (currentRealPercent > highestDisplayPercent)
                {
                    highestDisplayPercent = currentRealPercent;
                }

                if (loadingText != null)
                    loadingText.text = LocalizationManager.Tr("GENERATING WORLD... {0}%", highestDisplayPercent);

                yield return null;
            }
        }

        // isControlBlocked intentionally NOT touched — see the note in
        // the "start of load" block above. When isLoading flips false at
        // the end of this coroutine, PlayerController's OR-check
        // naturally re-enables input unless another system (tutorial /
        // cinematic / pause) is holding its own block.

        if (loadingText != null) loadingText.text = LocalizationManager.Tr("READY");
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. Fade In (плавне повернення до гри)
        if (loadingCanvasGroup != null)
        {
            while (loadingCanvasGroup.alpha > 0f)
            {
                loadingCanvasGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
            loadingCanvasGroup.gameObject.SetActive(false);
        }

        if (blackFadeGroup != null)
        {
            while (blackFadeGroup.alpha > 0f)
            {
                blackFadeGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
            blackFadeGroup.gameObject.SetActive(false);
        }

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        isLoading = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLevelTimer();
        }
    }

    private IEnumerator HintRoutine()
    {
        while (true)
        {
            if (hintText != null && gameHints != null && gameHints.Length > 0)
                // Wrap through Tr — hint strings are authored in English
                // in the Inspector; the localisation table can override
                // each one via self-keyed entries.
                hintText.text = LocalizationManager.Tr(gameHints[Random.Range(0, gameHints.Length)]);
            yield return new WaitForSecondsRealtime(hintChangeInterval);
        }
    }
}