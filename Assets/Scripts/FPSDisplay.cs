using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public static FPSDisplay Instance;
    private TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    private void Awake()
    {
        Instance = this;
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Перевіряємо стан при завантаженні сцени
        UpdateVisibility();
    }

    private void Update()
    {
        // Розрахунок FPS через незмінений час (працює на паузі)
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        // Фарбуємо текст залежно від продуктивності
        string color = "white";
        if (fps < 30) color = "#FF4444"; // Червоний (погано)
        else if (fps < 55) color = "#FFD700"; // Жовтий (середньо)
        else color = "#00FF00"; // Зелений (ідеально)

        fpsText.text = $"<color={color}>{Mathf.CeilToInt(fps)} FPS</color>";
    }

    public void UpdateVisibility()
    {
        // Вмикаємо або вимикаємо об'єкт залежно від налаштувань
        bool isEnabled = PlayerPrefs.GetInt("Settings_ShowFPS", 0) == 1;

        // Ми не можемо просто зробити SetActive(false), бо скрипт перестане працювати.
        // Тому просто вимикаємо рендеринг тексту.
        if (fpsText != null) fpsText.enabled = isEnabled;
    }
}