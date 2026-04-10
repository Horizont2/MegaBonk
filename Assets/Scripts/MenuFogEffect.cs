using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Spawns slow-drifting fog blobs that move horizontally across the screen.
/// Attach to a full-screen RectTransform inside MenuCanvas.
/// Requires a soft circle sprite (blurred edges) as fogPrefab.
/// </summary>
public class MenuFogEffect : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("A UI Image prefab with a soft/blurred circle sprite")]
    public GameObject fogPrefab;
    public int maxParticles = 5;
    public float spawnInterval = 5f;

    [Header("Movement")]
    public float minSpeed = 20f;
    public float maxSpeed = 50f;

    [Header("Appearance")]
    public Color fogColor = new Color(0.31f, 0.47f, 0.16f, 0.06f); // green tint, very low alpha
    public float minSize = 250f;
    public float maxSize = 500f;
    public float lifetime = 30f;

    private RectTransform areaRect;

    private void Start()
    {
        areaRect = GetComponent<RectTransform>();
        if (fogPrefab != null)
            StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (transform.childCount < maxParticles)
                StartCoroutine(AnimateFogBlob());

            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private IEnumerator AnimateFogBlob()
    {
        GameObject fog = Instantiate(fogPrefab, transform);
        RectTransform rect = fog.GetComponent<RectTransform>();
        Image img = fog.GetComponent<Image>();

        // Random size
        float size = Random.Range(minSize, maxSize);
        rect.sizeDelta = new Vector2(size, size);

        // Start from left side, random Y between 40%-80% of screen height
        float startX = -areaRect.rect.width * 0.5f - size * 0.5f;
        float yPos = Random.Range(areaRect.rect.height * -0.1f, areaRect.rect.height * 0.3f);
        rect.anchoredPosition = new Vector2(startX, yPos);

        // Color with slight variation
        if (img != null)
        {
            Color c = fogColor;
            c.g += Random.Range(-0.05f, 0.05f);
            c.a = fogColor.a;
            img.color = c;
            img.raycastTarget = false;
        }

        float speed = Random.Range(minSpeed, maxSpeed);
        float t = 0f;
        float endX = areaRect.rect.width * 0.5f + size * 0.5f;

        while (t < lifetime)
        {
            if (fog == null) yield break;

            t += Time.unscaledDeltaTime;
            float p = t / lifetime;

            // Move right
            Vector2 pos = rect.anchoredPosition;
            pos.x += speed * Time.unscaledDeltaTime;
            rect.anchoredPosition = pos;

            // Fade in then out
            if (img != null)
            {
                float alpha;
                if (p < 0.2f)
                    alpha = Mathf.Lerp(0f, fogColor.a, p / 0.2f);
                else if (p > 0.8f)
                    alpha = Mathf.Lerp(fogColor.a, 0f, (p - 0.8f) / 0.2f);
                else
                    alpha = fogColor.a;

                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }

            // Stop if passed right edge
            if (rect.anchoredPosition.x > endX) break;

            yield return null;
        }

        if (fog != null) Destroy(fog);
    }
}
