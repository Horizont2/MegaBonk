using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MissionUIElement : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI missionText;
    public Image checkboxEmpty;
    public Image checkboxDone; // Зображення галочки

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private string baseDescription;
    private bool isCompleted = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        checkboxDone.gameObject.SetActive(false); // Ховаємо галочку на старті
    }

    public void Setup(string description, int current, int target)
    {
        baseDescription = description;
        UpdateProgress(current, target);
    }

    public void UpdateProgress(int current, int target)
    {
        if (isCompleted) return;
        missionText.text = $"{baseDescription} ({current}/{target})";
    }

    public void CompleteMission()
    {
        if (isCompleted) return;
        isCompleted = true;

        // Прибираємо цифри, залишаємо тільки текст
        missionText.text = baseDescription;

        StartCoroutine(CompleteAnimationRoutine());
    }

    private IEnumerator CompleteAnimationRoutine()
    {
        // 1. Поп-анімація галочки
        checkboxEmpty.gameObject.SetActive(false);
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

        // 2. Пауза, щоб гравець встиг прочитати
        yield return new WaitForSeconds(1.5f);

        // 3. Відліт вліво + прозорість
        t = 0;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(-600f, 0); // Відлітає за екран

        while (t < 1)
        {
            t += Time.deltaTime * 2f;
            canvasGroup.alpha = Mathf.Lerp(1f, 0.5f, t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        // 4. Знищуємо плашку
        Destroy(gameObject);
    }
}