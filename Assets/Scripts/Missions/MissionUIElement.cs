using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MissionUIElement : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI progressText;    // Для цифр (0/50)
    public TextMeshProUGUI descriptionText; // Для тексту "Kill 50 skeletons"
    public Image checkboxEmpty;
    public Image checkboxDone;

    [Header("Slider (НОВЕ)")]
    public Slider progressSlider;           // Посилання на твій Slider

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    public bool isCompleted = false;

    private int currentVisualProgress = 0;
    private int targetVisualProgress = 0;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        if (checkboxDone != null) checkboxDone.gameObject.SetActive(false);
    }

    public void Setup(string description, int current, int target)
    {
        if (descriptionText != null) descriptionText.text = description;

        if (progressSlider != null)
        {
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

        if (progressText != null) progressText.text = $"Progress: {current} / {target}";
    }

    private void Update()
    {
        // Плавна анімація слайдера кожного кадру
        if (progressSlider != null && !isCompleted)
        {
            progressSlider.value = Mathf.Lerp(progressSlider.value, currentVisualProgress, Time.deltaTime * 5f);
        }
    }

    public void CompleteMission()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (progressText != null) progressText.text = "<color=#00FF00>COMPLETED</color>";
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
                t += Time.deltaTime * 5f;
                checkboxDone.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f), t);
                yield return null;
            }
            checkboxDone.transform.localScale = Vector3.one;
        }
    }
}