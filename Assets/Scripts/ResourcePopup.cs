using UnityEngine;
using TMPro;

public class ResourcePopup : MonoBehaviour
{
    private TextMeshProUGUI popupText;
    private RectTransform rectTransform;
    private Vector2 startPos;

    [Header("Settings")]
    public float displayTime = 1.5f; // Скільки секунд висить текст
    public float floatSpeed = 40f;   // Швидкість польоту вгору

    private int accumulatedValue = 0;
    private float currentTimer = 0f;

    private void Awake()
    {
        popupText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;

        // Ховаємо текст на старті
        Color c = popupText.color;
        c.a = 0f;
        popupText.color = c;
    }

    public void ShowChange(int amount)
    {
        if (amount == 0) return;

        // Якщо текст вже зник, скидаємо значення і позицію
        if (currentTimer <= 0)
        {
            accumulatedValue = 0;
            rectTransform.anchoredPosition = startPos;
        }

        // ДОПЛЮСОВУЄМО нове значення до старого (магія накопичення!)
        accumulatedValue += amount;

        // Оновлюємо колір і знак
        if (accumulatedValue > 0)
        {
            popupText.text = $"+{accumulatedValue}";
            popupText.color = Color.green;
        }
        else if (accumulatedValue < 0)
        {
            popupText.text = $"{accumulatedValue}"; // Мінус вже є в самому числі
            popupText.color = Color.red;
        }
        else
        {
            // Якщо раптом вийшов нуль (+10 і -10)
            popupText.color = new Color(0, 0, 0, 0);
            currentTimer = 0;
            return;
        }

        // Скидаємо таймер і повертаємо текст на стартову позицію для ефекту "удару/оновлення"
        currentTimer = displayTime;
        rectTransform.anchoredPosition = startPos;

        // Робимо повністю видимим
        Color visibleColor = popupText.color;
        visibleColor.a = 1f;
        popupText.color = visibleColor;
    }

    private void Update()
    {
        if (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;

            // Плавно піднімаємо вгору
            rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

            // Плавно розчиняємо (Fade Out) в останні 0.5 секунд
            if (currentTimer < 0.5f)
            {
                Color c = popupText.color;
                c.a = currentTimer / 0.5f;
                popupText.color = c;
            }
        }
    }
}