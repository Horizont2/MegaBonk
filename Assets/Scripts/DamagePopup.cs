using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    public TextMeshPro textMesh;
    public float moveSpeed = 3f;
    public float fadeSpeed = 1.5f;
    public float lifetime = 1f;

    [Header("Scale Animation")]
    public float startScale = 0.6f;
    public float normalScale = 0.35f;
    public float scaleDownSpeed = 8f;

    [Header("Damage Colors")]
    public Color normalColor = Color.white;
    public Color mediumColor = new Color(1f, 0.85f, 0.3f); // Gold
    public Color highColor = new Color(1f, 0.3f, 0.2f);    // Red-orange
    public float mediumThreshold = 30f;
    public float highThreshold = 80f;

    private Color textColor;
    private Transform camTransform;
    private float timer;
    private float currentScale;
    private bool isCrit;

    public void Setup(float damageAmount)
    {
        textMesh.text = Mathf.CeilToInt(damageAmount).ToString();
        timer = 0f;

        // Color based on damage amount
        if (damageAmount >= highThreshold)
        {
            textColor = highColor;
            isCrit = true;
            currentScale = startScale * 1.3f; // Extra big for heavy hits
            textMesh.text = Mathf.CeilToInt(damageAmount).ToString() + "!";
        }
        else if (damageAmount >= mediumThreshold)
        {
            textColor = mediumColor;
            isCrit = false;
            currentScale = startScale * 1.1f;
        }
        else
        {
            textColor = normalColor;
            isCrit = false;
            currentScale = startScale;
        }

        textColor.a = 1f;
        textMesh.color = textColor;
        transform.localScale = Vector3.one * currentScale;

        // Random offset so multiple hits don't overlap
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        transform.position += new Vector3(randomX, 1f, randomZ);
    }

    private void OnEnable()
    {
        if (Camera.main != null) camTransform = Camera.main.transform;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Move upwards (heavy hits float a bit faster)
        float speed = isCrit ? moveSpeed * 1.3f : moveSpeed;
        transform.position += Vector3.up * speed * Time.deltaTime;

        // Scale pop: quickly shrink from big to normal size
        if (currentScale > normalScale)
        {
            currentScale = Mathf.Lerp(currentScale, normalScale, scaleDownSpeed * Time.deltaTime);
            transform.localScale = Vector3.one * currentScale;
        }

        // Always face camera
        if (camTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
        }

        // Fade out
        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;

        // Return to pool when lifetime expires
        if (timer >= lifetime)
        {
            transform.localScale = Vector3.one;
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.ReturnToPool(gameObject);
            else
                Destroy(gameObject);
        }
    }
}