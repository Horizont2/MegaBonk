using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI References")]
    public CanvasGroup loadingCanvasGroup;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI hintText;
    public CanvasGroup blackFadeGroup;

    [Header("Settings")]
    public float sceneFadeSpeed = 1.5f;
    public float hintChangeInterval = 5f;

    [TextArea(2, 3)]
    public string[] gameHints;

    private bool isLoading = false;
    private Coroutine hintCoroutine;

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

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;

        // 1. Поява чорного екрану
        if (blackFadeGroup != null)
        {
            blackFadeGroup.gameObject.SetActive(true);
            while (blackFadeGroup.alpha < 1f)
            {
                blackFadeGroup.alpha += Time.unscaledDeltaTime * sceneFadeSpeed * 2f;
                yield return null;
            }
        }

        if (loadingCanvasGroup != null)
        {
            loadingSlider.value = 0f;
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 1f;
        }

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(HintRoutine());

        // Пріоритет Low, щоб UI міг анімуватися
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float visualProgress = 0f;

        // Чекаємо завантаження файлів сцени (до 90%)
        while (asyncLoad.progress < 0.9f || visualProgress < 0.9f)
        {
            visualProgress = Mathf.MoveTowards(visualProgress, asyncLoad.progress / 0.9f, Time.unscaledDeltaTime * 0.5f);
            if (loadingSlider != null) loadingSlider.value = visualProgress * 0.5f; // Перша половина прогресу
            if (loadingText != null) loadingText.text = $"LOADING ASSETS... {Mathf.FloorToInt(visualProgress * 50)}%";
            yield return null;
        }

        // 3. Активуємо сцену
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) yield return null;

        // --- ОЧІКУВАННЯ ГЕНЕРАЦІЇ СВІТУ ---
        if (loadingText != null) loadingText.text = "GENERATING WORLD...";

        float genProgress = 0f;

        // Повертаємо пріоритет, щоб генерація пройшла максимально швидко
        Application.backgroundLoadingPriority = ThreadPriority.Normal;

        // Цикл чекає, поки WorldGenerator.cs змінить IsGenerationDone на true
        while (!WorldGenerator.IsGenerationDone)
        {
            genProgress = Mathf.MoveTowards(genProgress, 1f, Time.unscaledDeltaTime * 0.2f);
            if (loadingSlider != null) loadingSlider.value = 0.5f + (genProgress * 0.45f); // Друга половина прогресу
            yield return null;
        }

        if (loadingSlider != null) loadingSlider.value = 1f;
        if (loadingText != null) loadingText.text = "READY";

        yield return new WaitForSecondsRealtime(0.5f); // Коротка пауза для візуального комфорту

        // 4. Плавне зникнення панелі завантаження
        if (loadingCanvasGroup != null)
        {
            while (loadingCanvasGroup.alpha > 0f)
            {
                loadingCanvasGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
            loadingCanvasGroup.gameObject.SetActive(false);
        }

        // 5. Плавний Fade Out чорного екрану (тут гравець вже бачить оптимізований світ)
        if (blackFadeGroup != null)
        {
            while (blackFadeGroup.alpha > 0f)
            {
                blackFadeGroup.alpha -= Time.unscaledDeltaTime * sceneFadeSpeed;
                yield return null;
            }
            blackFadeGroup.gameObject.SetActive(false);
        }

        isLoading = false;
    }

    private IEnumerator HintRoutine()
    {
        while (true)
        {
            if (gameHints.Length > 0 && hintText != null)
                hintText.text = gameHints[Random.Range(0, gameHints.Length)];
            yield return new WaitForSecondsRealtime(hintChangeInterval);
        }
    }
}