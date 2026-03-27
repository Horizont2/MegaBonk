using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class UIIconGlimmer : MonoBehaviour
{
    [Header("Glimmer Settings")]
    public GameObject sparklePrefab;
    [Tooltip("Min and Max time between glimmer sparkles")]
    public float minGlimmerInterval = 0.5f;
    public float maxGlimmerInterval = 2f;
    [Tooltip("Lifetime of a single sparkle")]
    public float sparkleLifetime = 1.2f;

    [Header("Appearance Settings")]
    public Color sparkleColor = Color.white;
    public Vector3 maxSparkleScale = new Vector3(1.5f, 1.5f, 1f);

    private RectTransform myRect;
    private Coroutine glimmerLoopCoroutine;

    private void Awake()
    {
        // Get the RectTransform of the Icon we are attached to
        myRect = GetComponent<RectTransform>();
    }

    // Call this to start the continuous glimmer effect
    public void StartEffect()
    {
        if (glimmerLoopCoroutine != null) StopCoroutine(glimmerLoopCoroutine);
        glimmerLoopCoroutine = StartCoroutine(GlimmerLoop());
    }

    // Automatically stop when the icon object is disabled
    private void OnDisable()
    {
        if (glimmerLoopCoroutine != null) StopCoroutine(glimmerLoopCoroutine);

        // Destroy any leftover sparkles
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator GlimmerLoop()
    {
        // Infinite loop while the object is active
        while (true)
        {
            StartCoroutine(AnimateSingleSparkle());

            // Wait for a random interval (using Realtime for unscaled menus)
            float waitTime = Random.Range(minGlimmerInterval, maxGlimmerInterval);
            yield return new WaitForSecondsRealtime(waitTime);
        }
    }

    private IEnumerator AnimateSingleSparkle()
    {
        // 1. CREATE SPARKLE as a child of the icon
        GameObject sparkle = Instantiate(sparklePrefab, transform);
        RectTransform rect = sparkle.GetComponent<RectTransform>();
        Image img = sparkle.GetComponent<Image>();

        // 2. CHOOSE POSITION randomly within the icon boundaries
        // We calculate random coordinates inside the RectTransform rectangle
        float randomX = Random.Range(-myRect.rect.width / 2f, myRect.rect.width / 2f);
        float randomY = Random.Range(-myRect.rect.height / 2f, myRect.rect.height / 2f);
        rect.anchoredPosition = new Vector2(randomX, randomY);

        // 3. SET INITIAL STATE
        if (img != null) img.color = sparkleColor;
        rect.localScale = Vector3.zero; // Start invisibly small

        float timer = 0f;
        // Basic glimmer rotation
        float randomRotationSpeed = Random.Range(30f, 90f);

        // 4. ANIMATION LOOP (Uses unscaledDeltaTime for menus)
        while (timer < sparkleLifetime)
        {
            if (sparkle == null) yield break;

            timer += Time.unscaledDeltaTime;
            float progress = timer / sparkleLifetime;

            // Curve logic: grows to maxScale at 50% lifetime, then shrinks to zero
            // and fades alpha to zero in the second half
            float scaleMultiplier = 1f;
            if (progress <= 0.5f)
            {
                // Grow phase (0% -> 50%)
                scaleMultiplier = progress * 2f; // maps 0-0.5 to 0-1
            }
            else
            {
                // Fade out/shrink phase (50% -> 100%)
                scaleMultiplier = 1f - ((progress - 0.5f) * 2f); // maps 0.5-1 to 1-0

                // Fade alpha
                if (img != null)
                {
                    Color c = img.color;
                    c.a = scaleMultiplier; // Use the shrinking multiplier for alpha fade
                    img.color = c;
                }
            }

            // Apply Scale and Rotation
            rect.localScale = Vector3.Scale(maxSparkleScale, new Vector3(scaleMultiplier, scaleMultiplier, 1f));
            rect.Rotate(0, 0, randomRotationSpeed * Time.unscaledDeltaTime);

            yield return null;
        }

        // 5. CLEAN UP
        Destroy(sparkle);
    }
}