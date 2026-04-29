using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MissionUIElement : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;       // Головний заголовок (твоє поле MissionText)
    public TextMeshProUGUI descriptionText; // Опис місії (твоє поле DescText)
    public TextMeshProUGUI progressText;    // Цифри (твоє поле ProgressText)

    public Image checkboxEmpty;
    public Image checkboxDone;
    public Image backgroundImage;

    [Header("Slider")]
    public Slider progressSlider;

    private CanvasGroup canvasGroup;
    public bool isCompleted = false;

    private int currentVisualProgress = 0;
    private int targetVisualProgress = 0;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (checkboxDone != null) checkboxDone.gameObject.SetActive(false);
    }

    // НОВЕ: Тепер ми приймаємо і Title, і Description окремо
    public void Setup(string title, string description, int current, int target)
    {
        // Скидаємо стан "виконано" для повторного використання віджета
        isCompleted = false;
        StopAllCoroutines();
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (checkboxEmpty != null) checkboxEmpty.gameObject.SetActive(true);
        if (checkboxDone != null)
        {
            checkboxDone.gameObject.SetActive(false);
            checkboxDone.transform.localScale = Vector3.one;
        }

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        if (progressSlider != null)
        {
            // Ховаємо слайдер, якщо це просте завдання на 1 дію
            progressSlider.gameObject.SetActive(target > 1);
            progressSlider.maxValue = target;
            progressSlider.value = current;
        }

        currentVisualProgress = current;
        targetVisualProgress = target;

        UpdateProgress(current, target);
    }

    public void UpdateProgress(int current, int target)
    {
        if (isCompleted) return;

        currentVisualProgress = current;
        targetVisualProgress = target;

        // Цифри окремо: "4 / 10"
        if (progressText != null)
            progressText.text = $"<color=#FFD700>{current}</color> / {target}";
    }

    private void Update()
    {
        if (progressSlider != null && !isCompleted)
        {
            progressSlider.value = Mathf.Lerp(progressSlider.value, currentVisualProgress, Time.deltaTime * 8f);
        }
    }

    public void CompleteMission()
    {
        if (isCompleted) return;
        isCompleted = true;

        // Закреслюємо заголовок і пишемо DONE замість цифр
        if (titleText != null) titleText.text = $"<s>{titleText.text}</s>";
        if (progressText != null) progressText.text = "<color=#00FF00>DONE</color>";

        if (progressSlider != null) progressSlider.value = progressSlider.maxValue;

        StartCoroutine(CompleteAnimationRoutine());
    }

    private IEnumerator CompleteAnimationRoutine()
    {
        if (checkboxEmpty != null) checkboxEmpty.gameObject.SetActive(false);
        if (checkboxDone != null)
        {
            checkboxDone.gameObject.SetActive(true);
            checkboxDone.transform.localScale = Vector3.zero;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 6f;
                float scale = Mathf.LerpUnclamped(0f, 1.2f, Mathf.Sin(t * Mathf.PI * 0.5f));
                checkboxDone.transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }
            checkboxDone.transform.localScale = Vector3.one;
        }

        if (backgroundImage != null)
        {
            Color originalColor = backgroundImage.color;
            backgroundImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);

            float flashT = 0;
            while (flashT < 1)
            {
                flashT += Time.deltaTime * 3f;
                backgroundImage.color = Color.Lerp(Color.white, originalColor, flashT);
                yield return null;
            }
        }

        float alphaT = 0;
        float startAlpha = canvasGroup.alpha;
        while (alphaT < 1)
        {
            alphaT += Time.deltaTime * 2f;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0.6f, alphaT);
            yield return null;
        }
    }
}