using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ActiveBuildWidget : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timerText;
    public Slider progressSlider;
    public Image buildingIcon;
    public RectTransform hammerIcon; // Перетягни сюди іконку молотка для анімації

    private DateTime endTime;
    private float totalDuration;
    private bool isActive = false;

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

        if (remainingSeconds <= 0)
        {
            isActive = false;
            Destroy(gameObject); // Знищуємо віджет, коли час вийшов
            return;
        }

        // Оновлюємо таймер у форматі Хвилини:Секунди
        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}", remaining.Minutes, remaining.Seconds);

        // Оновлюємо слайдер
        if (progressSlider != null) progressSlider.value = 1f - (remainingSeconds / totalDuration);

        // Анімація молотка (гойдається туди-сюди)
        if (hammerIcon != null)
        {
            float angle = Mathf.Sin(Time.time * 15f) * 20f;
            hammerIcon.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}