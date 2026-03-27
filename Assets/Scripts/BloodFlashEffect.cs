using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BloodFlashEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("How fast the blood vanishes. Lower is slower.")]
    public float fadeSpeed = 3f;
    
    [Tooltip("Maximum transparency of the blood (0 to 1).")]
    public float maxAlpha = 0.5f;

    // Component references
    private Image bloodImage;

    private void Awake()
    {
        bloodImage = GetComponent<Image>();
        
        // Start completely transparent
        Color c = bloodImage.color;
        c.a = 0f;
        bloodImage.color = c;
    }

    private void Update()
    {
        if (bloodImage.color.a > 0f)
        {
            // Smoothly decrease the Alpha over time
            Color c = bloodImage.color;
            c.a -= fadeSpeed * Time.deltaTime;
            
            // Prevent Alpha from going below 0
            c.a = Mathf.Max(c.a, 0f); 
            bloodImage.color = c;
        }
    }

    public void Flash()
    {
        // Set Alpha to max to show the effect immediately
        Color c = bloodImage.color;
        c.a = maxAlpha;
        bloodImage.color = c;
    }
}