using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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

    private Coroutine promptFadeCoroutine;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        // РОБИМО СКРИПТ "ВІЧНИМ"
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Об'єкт не видаляється при зміні сцени
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Підписуємось на подію завантаження сцени
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Відписуємось при видаленні
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Цей метод викликається АВТОМАТИЧНО щоразу, коли завантажилась будь-яка сцена
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StopAllCoroutines(); // Зупиняємо старі переходи
            StartCoroutine(SceneFadeRoutine(0f, "")); // Плавне розтемнення (Fade In)
        }

        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;

        // Знаходимо камеру в новій сцені, якщо потрібно для UI
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    // --- ЛОГІКА ЗАТЕМНЕННЯ ТА ПЕРЕХОДУ ---
    public void FadeAndLoadScene(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(SceneFadeRoutine(1f, sceneName)); // Плавне затемнення (Fade Out)
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator SceneFadeRoutine(float targetAlpha, string sceneToLoad)
    {
        Color c = fadeImage.color;

        // Якщо ми розтемнюємо екран, спочатку переконуємось, що він був чорним
        if (targetAlpha == 0f && c.a < 0.1f) c.a = 1f;

        while (Mathf.Abs(c.a - targetAlpha) > 0.01f)
        {
            c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.deltaTime * sceneFadeSpeed);
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (targetAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    // --- АНІМАЦІЯ ПІДКАЗКИ ---
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
}