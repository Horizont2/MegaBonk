using UnityEngine;

[RequireComponent(typeof(Light))]
public class GlimmerSweep : MonoBehaviour
{
    [Header("Sweep Points")]
    public Transform startPoint; // Звідки починає
    public Transform endPoint;   // Куди летить

    [Header("Settings")]
    public float speed = 0.5f;   // Швидкість прольоту
    public float maxIntensity = 50f; // Максимальна яскравість по центру

    private Light glimmerLight;
    private float progress = 0f;

    private void Awake()
    {
        glimmerLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        // Скидаємо прогрес щоразу, коли світло вмикається
        progress = 0f;
    }

    private void Update()
    {
        if (startPoint == null || endPoint == null) return;

        // Збільшуємо прогрес від 0 до 1
        progress += Time.deltaTime * speed;
        if (progress > 1f) progress = 0f; // Зациклюємо

        // 1. Рухаємо світло між точками
        transform.position = Vector3.Lerp(startPoint.position, endPoint.position, progress);

        // 2. Магія математики: плавно вмикаємо і вимикаємо яскравість!
        // Sin(progress * PI) дає дугу: 0 на старті, 1 по центру, 0 в кінці.
        glimmerLight.intensity = Mathf.Sin(progress * Mathf.PI) * maxIntensity;
    }
}