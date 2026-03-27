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

    // Call this method right after instantiating the popup
    public void Setup(float damageAmount)
    {
        // Round the damage to a whole number
        textMesh.text = Mathf.CeilToInt(damageAmount).ToString();
        textColor = textMesh.color;

        // Add a random offset so multiple hits don't overlap perfectly
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        transform.position += new Vector3(randomX, 1f, randomZ);
    }

    private void Start()
    {
        if (Camera.main != null) camTransform = Camera.main.transform;

        // Automatically destroy the popup after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move the text upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Force the text to always face the camera
        if (camTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
        }

        // Smoothly fade out the text alpha
        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}