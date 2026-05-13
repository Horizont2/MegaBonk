using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    public TextMeshPro textMesh;
    public float moveSpeed = 3f;
    public float fadeSpeed = 2f;
    public float lifetime = 1f;

    [Header("Visual Styles")]
    public Color normalColor = Color.white;
    public Color critColor = new Color(1f, 0.8f, 0f);
    public float normalSize = 5f;
    public float critSize = 8f;

    private Color textColor;
    private Transform camTransform;
    private float currentLifeTime;

    public void Setup(float damageAmount, bool isCrit = false)
    {
        // Додаємо знак мінуса перед цифрою
        textMesh.text = "-" + Mathf.CeilToInt(damageAmount).ToString();

        if (isCrit)
        {
            textMesh.text += "!";
            textMesh.color = critColor;
            textMesh.fontSize = critSize;
            lifetime += 0.5f;
            moveSpeed *= 1.2f;
        }
        else
        {
            textMesh.color = normalColor;
            textMesh.fontSize = normalSize;
        }

        textColor = textMesh.color;
        currentLifeTime = lifetime;

        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        transform.position += new Vector3(randomX, 1f, randomZ);

        // Починаємо з нульового масштабу для ефекту появи (Pop)
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        if (Camera.main != null) camTransform = Camera.main.transform;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        currentLifeTime -= Time.deltaTime;
        float lifeRatio = 1f - (currentLifeTime / lifetime); // Значення від 0 до 1

        // 1. Анімація масштабу (Juicy Pop Effect)
        if (lifeRatio < 0.15f)
        {
            // Різко збільшуємо від 0 до 1.3
            float scale = Mathf.Lerp(0f, 1.3f, lifeRatio / 0.15f);
            transform.localScale = new Vector3(scale, scale, scale);
        }
        else if (lifeRatio < 0.3f)
        {
            // Пружинимо назад від 1.3 до 1.0
            float scale = Mathf.Lerp(1.3f, 1f, (lifeRatio - 0.15f) / 0.15f);
            transform.localScale = new Vector3(scale, scale, scale);
        }

        // 2. Рух вгору (уповільнюється під кінець)
        transform.position += Vector3.up * moveSpeed * Time.deltaTime * (currentLifeTime / lifetime);

        // 3. Завжди дивимося прямо в камеру
        if (camTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
        }

        // 4. Плавне згасання тексту в другій половині життя
        if (currentLifeTime < lifetime * 0.5f)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
        }
    }
}