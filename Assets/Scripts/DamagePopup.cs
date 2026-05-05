using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    public TextMeshPro textMesh;
    public float moveSpeed = 3f;
    public float fadeSpeed = 1.5f;
    public float lifetime = 1f;

    [Header("Visual Styles (NEW)")]
    public Color normalColor = Color.white;
    public Color critColor = new Color(1f, 0.8f, 0f); // Золото-оранжевий колір для криту
    public float normalSize = 5f; // Базовий розмір (підлаштуй під свій проект)
    public float critSize = 8f;   // Збільшений розмір для криту

    private Color textColor;
    private Transform camTransform;

    // ОНОВЛЕНО: Тепер метод приймає параметр isCrit
    public void Setup(float damageAmount, bool isCrit = false)
    {
        textMesh.text = Mathf.CeilToInt(damageAmount).ToString();

        if (isCrit)
        {
            textMesh.text += "!"; // Додаємо соковитий знак оклику
            textMesh.color = critColor;
            textMesh.fontSize = critSize;

            // Крит-попап висить трохи довше і летить трохи швидше вгору
            lifetime += 0.5f;
            moveSpeed *= 1.2f;
        }
        else
        {
            textMesh.color = normalColor;
            textMesh.fontSize = normalSize;
        }

        textColor = textMesh.color;

        // Випадковий зсув, щоб цифри не накладались одна на одну
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        transform.position += new Vector3(randomX, 1f, randomZ);
    }

    private void Start()
    {
        if (Camera.main != null) camTransform = Camera.main.transform;

        // Автоматичне знищення
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Рух вгору
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Завжди дивимося в камеру
        if (camTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
        }

        // Плавне згасання
        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}