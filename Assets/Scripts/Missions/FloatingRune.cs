using UnityEngine;

public class FloatingRune : MonoBehaviour
{
    [Header("Bobbing Settings (Погойдування)")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    [Header("Pulsating Settings (Пульсація)")]
    public float pulseSpeed = 3f;
    public float minScale = 0.8f;
    public float maxScale = 1.1f;

    private Vector3 startPos;
    private Camera mainCamera;
    private Vector3 initialScale;

    void Start()
    {
        // Запам'ятовуємо початкові координати та розмір
        startPos = transform.localPosition;
        initialScale = transform.localScale;
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. Логіка погойдування (Bobbing)
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

        // 2. Логіка пульсації розміру (Pulsating)
        float scaleModifier = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        transform.localScale = initialScale * scaleModifier;

        // 3. Логіка Білборда (завжди дивиться на камеру)
        if (mainCamera != null)
        {
            // Обертаємо об'єкт так, щоб він дивився в ту саму сторону, що й камера
            transform.forward = mainCamera.transform.forward;
        }
    }
}