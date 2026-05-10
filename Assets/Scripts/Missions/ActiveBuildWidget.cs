using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ActiveBuildWidget : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timerText;
    public Slider progressSlider;
    public Image buildingIcon;
    public RectTransform hammerIcon;

    private DateTime endTime;
    private float totalDuration;
    private bool isActive = false;
    private bool isAnimatingExit = false; // Блокування подвійного виклику

    public void Setup(string bName, Sprite bIcon, DateTime targetTime, float duration)
    {
        if (titleText != null) titleText.text = bName;
        if (buildingIcon != null && bIcon != null) buildingIcon.sprite = bIcon;

        endTime = targetTime;
        totalDuration = duration;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return;

        TimeSpan remaining = endTime - DateTime.UtcNow;
        float remainingSeconds = (float)remaining.TotalSeconds;

        if (remainingSeconds <= 0 && !isAnimatingExit)
        {
            isActive = false;
            StartCoroutine(ExitAnimationRoutine()); // Запускаємо анімацію замість Destroy
            return;
        }

        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}", remaining.Minutes, remaining.Seconds);
        if (progressSlider != null) progressSlider.value = 1f - (remainingSeconds / totalDuration);

        if (hammerIcon != null)
        {
            float angle = Mathf.Sin(Time.time * 15f) * 20f;
            hammerIcon.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private IEnumerator ExitAnimationRoutine()
    {
        isAnimatingExit = true;
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;

        // 1. Відтяжка (Anticipation) - трохи вліво до центру
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
            rect.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(-40f, 0), curve);
            yield return null;
        }

        // 2. Стрімкий виліт вправо за екран
        t = 0;
        Vector2 midPos = rect.anchoredPosition;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            float curve = t * t * t; // Експоненційне прискорення
            rect.anchoredPosition = Vector2.Lerp(midPos, midPos + new Vector2(600f, 0), curve);

            // Плавно робимо прозорим
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f - t;

            yield return null;
        }

        Destroy(gameObject);
    }
}