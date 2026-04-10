using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Spawns floating ember/spark UI particles that rise and fade.
/// Attach to an empty RectTransform that covers the area where embers should appear.
/// Requires a small sprite prefab assigned in Inspector.
/// </summary>
public class MenuEmberParticle : MonoBehaviour
{
    [Header("Ember Settings")]
    [Tooltip("A small UI Image prefab (e.g. 4x4 white circle sprite)")]
    public GameObject emberPrefab;
    public int maxEmbers = 20;
    public float spawnInterval = 0.3f;

    [Header("Movement")]
    public float minRiseSpeed = 30f;
    public float maxRiseSpeed = 80f;
    public float horizontalDrift = 15f;
    public float lifetime = 4f;

    [Header("Appearance")]
    public Color[] emberColors = {
        new Color(1f, 0.7f, 0.2f, 0.7f),
        new Color(1f, 0.55f, 0.12f, 0.6f),
        new Color(1f, 0.86f, 0.4f, 0.5f)
    };
    public float minSize = 2f;
    public float maxSize = 5f;

    private RectTransform areaRect;

    private void Start()
    {
        areaRect = GetComponent<RectTransform>();
        if (emberPrefab != null)
            StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (transform.childCount < maxEmbers)
                StartCoroutine(AnimateEmber());

            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private IEnumerator AnimateEmber()
    {
        GameObject ember = Instantiate(emberPrefab, transform);
        RectTransform rect = ember.GetComponent<RectTransform>();
        Image img = ember.GetComponent<Image>();

        // Random spawn position along bottom of area
        float spawnX = Random.Range(-areaRect.rect.width * 0.5f, areaRect.rect.width * 0.5f);
        float spawnY = Random.Range(-areaRect.rect.height * 0.3f, -areaRect.rect.height * 0.1f);
        rect.anchoredPosition = new Vector2(spawnX, spawnY);

        // Random size
        float size = Random.Range(minSize, maxSize);
        rect.sizeDelta = new Vector2(size, size);

        // Random color
        if (img != null && emberColors.Length > 0)
            img.color = emberColors[Random.Range(0, emberColors.Length)];

        float riseSpeed = Random.Range(minRiseSpeed, maxRiseSpeed);
        float driftDir = Random.Range(-1f, 1f);
        float t = 0f;

        while (t < lifetime)
        {
            if (ember == null) yield break;

            t += Time.unscaledDeltaTime;
            float p = t / lifetime;

            // Rise + drift
            Vector2 pos = rect.anchoredPosition;
            pos.y += riseSpeed * Time.unscaledDeltaTime;
            pos.x += driftDir * horizontalDrift * Time.unscaledDeltaTime;
            rect.anchoredPosition = pos;

            // Fade: appear quickly, fade out slowly
            if (img != null)
            {
                Color c = img.color;
                if (p < 0.2f)
                    c.a = Mathf.Lerp(0f, 0.8f, p / 0.2f);
                else
                    c.a = Mathf.Lerp(0.8f, 0f, (p - 0.2f) / 0.8f);
                img.color = c;
            }

            // Shrink near end
            float scale = p > 0.7f ? Mathf.Lerp(1f, 0.3f, (p - 0.7f) / 0.3f) : 1f;
            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        if (ember != null) Destroy(ember);
    }
}
