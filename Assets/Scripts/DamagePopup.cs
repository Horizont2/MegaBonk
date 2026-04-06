using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    public TextMeshPro textMesh;
    public float moveSpeed = 3f;
    public float fadeSpeed = 1.5f;
    public float lifetime = 1f;

    private Color textColor;
    private Transform camTransform;
    private float timer;

    public void Setup(float damageAmount)
    {
        textMesh.text = Mathf.CeilToInt(damageAmount).ToString();
        textColor = textMesh.color;
        textColor.a = 1f;
        textMesh.color = textColor;
        timer = 0f;

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

        // Move upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

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
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.ReturnToPool(gameObject);
            else
                Destroy(gameObject);
        }
    }
}