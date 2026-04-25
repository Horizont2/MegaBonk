using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Налаштування паралаксу")]
    public float moveAmount = 15f;    // Наскільки сильно зміщується UI
    public float smoothSpeed = 5f;    // Наскільки плавно він за нею тягнеться

    private Vector3 startPosition;

    void Start()
    {
        // Запам'ятовуємо початкову позицію панелі
        startPosition = transform.localPosition;
    }

    void Update()
    {
        // Отримуємо позицію миші на екрані (від 0 до ширини/висоти)
        Vector2 mousePos = Input.mousePosition;

        // Нормалізуємо позицію миші від -1 до 1 (щоб центр екрана був 0,0)
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;

        // Рахуємо нову позицію (зміщуємо в протилежний бік від миші)
        // Якщо хочеш, щоб меню тягнулося ЗА мишею, прибери мінус перед moveAmount
        Vector3 targetPos = startPosition + new Vector3(-normalizedX * moveAmount, -normalizedY * moveAmount, 0);

        // Плавно рухаємо панель до цілі
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
    }
}