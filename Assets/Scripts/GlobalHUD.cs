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

    [Header("Interaction Prompt")]
    public CanvasGroup promptCanvasGroup;
    public TextMeshProUGUI promptText;
    public float promptFadeSpeed = 5f;
    public float typingSpeed = 0.03f;

    [Header("Scene Transition")]
    public Image fadeImage;
    public float sceneFadeSpeed = 1.5f;

    [Header("Pause Menu Settings")]
    public CanvasGroup pausePanelGroup;
    public GameObject[] pauseButtons; // Для анімації масштабу
    public CanvasGroup[] pauseButtonGroups; // НОВЕ: Для затемнення кнопок
    public CanvasGroup giveUpButtonGroup;   // НОВЕ: Конкретно кнопка Give Up
    public TextMeshProUGUI giveUpText;      // НОВЕ: Текст на кнопці Give Up
    public float buttonDelay = 0.05f;

    private bool isPaused = false;
    private bool isConfirmingGiveUp = false; // Стан підтвердження
    private DepthOfField dofEffect;

    private Coroutine promptFadeCoroutine;
    private Coroutine typingCoroutine;

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string sceneName = SceneManager.GetActiveScene().name;
            // ПАУЗА ДОСТУПНА ТІЛЬКИ В ЦИХ ДВОХ СЦЕНАХ
            if (sceneName == "GameScene" || sceneName == "CampScene")
            {
                TogglePause();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(SyncCameraAndVolumeRoutine());

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(SceneFadeRoutine(0f, ""));
        }

        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;

        if (isPaused) TogglePause();
    }

    private IEnumerator SyncCameraAndVolumeRoutine()
    {
        yield return null;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
        }

        Volume volume = FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet(out dofEffect))
            {
                dofEffect.active = false;
            }
        }
        else
        {
            dofEffect = null;
        }
    }

    public void FadeAndLoadScene(string sceneName)
    {
        if (isPaused) TogglePause();

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(SceneFadeRoutine(1f, sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator SceneFadeRoutine(float targetAlpha, string sceneToLoad)
    {
        Color c = fadeImage.color;
        if (targetAlpha == 0f && c.a < 0.1f) c.a = 1f;

        while (Mathf.Abs(c.a - targetAlpha) > 0.01f)
        {
            c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime * sceneFadeSpeed);
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;

        if (!string.IsNullOrEmpty(sceneToLoad)) SceneManager.LoadScene(sceneToLoad);
        else if (targetAlpha == 0f) fadeImage.gameObject.SetActive(false);
    }

    public void ShowPrompt(string message)
    {
        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        promptFadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
        typingCoroutine = StartCoroutine(TypeTextRoutine(message));
    }

    public void HidePrompt()
    {
        if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
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

    private IEnumerator TypeTextRoutine(string message)
    {
        if (promptText == null) yield break;
        promptText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            promptText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // --- ПАУЗА ТА МЕНЮ ---
    public void TogglePause()
    {
        if (pausePanelGroup == null) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (dofEffect != null) dofEffect.active = isPaused;

        if (isPaused)
        {
            ResetGiveUpState(); // Скидаємо стан підтвердження при відкритті
            StartCoroutine(ShowMenuRoutine());
        }
        else
        {
            StartCoroutine(HideMenuRoutine());
        }

        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private IEnumerator ShowMenuRoutine()
    {
        pausePanelGroup.gameObject.SetActive(true);
        pausePanelGroup.blocksRaycasts = true;
        pausePanelGroup.interactable = true;

        // Вмикаємо/вимикаємо кнопку Give Up залежно від сцени
        bool inGame = SceneManager.GetActiveScene().name == "GameScene";
        if (giveUpButtonGroup != null) giveUpButtonGroup.gameObject.SetActive(inGame);

        foreach (var btn in pauseButtons)
        {
            if (btn != null) btn.GetComponent<RectTransform>().localScale = Vector3.zero;
        }

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
            if (btn != null && btn.activeSelf) // Не анімуємо вимкнену кнопку
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

    // --- ЛОГІКА КНОПКИ GIVE UP ---
    public void OnGiveUpClicked()
    {
        if (!isConfirmingGiveUp)
        {
            // ПЕРШИЙ КЛІК: Режим підтвердження
            isConfirmingGiveUp = true;
            if (giveUpText != null) giveUpText.text = "You sure?\nAll journey progress will be lost";

            // Затемнюємо всі інші кнопки
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
            // ДРУГИЙ КЛІК: Підтверджено. Очищаємо зібране і йдемо в табір.
            TogglePause();
            if (ResourceManager.Instance != null) ResourceManager.Instance.ClearRunInventory();
            FadeAndLoadScene("CampScene");
        }
    }

    private void ResetGiveUpState()
    {
        isConfirmingGiveUp = false;
        if (giveUpText != null) giveUpText.text = "Give Up";

        // Повертаємо всім кнопкам яскравість і клікабельність
        foreach (var btn in pauseButtonGroups)
        {
            if (btn != null)
            {
                btn.alpha = 1f;
                btn.interactable = true;
            }
        }
    }
}