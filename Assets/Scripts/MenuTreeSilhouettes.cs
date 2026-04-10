using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns a row of tree silhouette sprites along the bottom of the screen.
/// Attach to a RectTransform anchored to the bottom of the canvas.
/// Requires a tree silhouette sprite (dark, transparent background).
/// </summary>
public class MenuTreeSilhouettes : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("A tree silhouette sprite (dark pine/fir shape on transparent bg)")]
    public Sprite treeSprite;
    public int treeCount = 14;

    [Header("Size Variation")]
    public float minScale = 0.5f;
    public float maxScale = 1.3f;
    public float baseHeight = 120f;

    [Header("Appearance")]
    public Color treeColor = new Color(0.06f, 0.07f, 0.03f, 1f); // almost black-green
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.7f;

    [Header("Parallax (optional)")]
    [Tooltip("How much trees shift with mouse. 0 = no parallax")]
    public float parallaxAmount = 5f;

    private RectTransform areaRect;
    private RectTransform[] treeRects;
    private float[] treeBaseX;

    private void Start()
    {
        areaRect = GetComponent<RectTransform>();
        SpawnTrees();
    }

    private void SpawnTrees()
    {
        treeRects = new RectTransform[treeCount];
        treeBaseX = new float[treeCount];

        float totalWidth = areaRect.rect.width;

        for (int i = 0; i < treeCount; i++)
        {
            GameObject treeObj = new GameObject("Tree_" + i);
            treeObj.transform.SetParent(transform, false);

            RectTransform rect = treeObj.AddComponent<RectTransform>();
            Image img = treeObj.AddComponent<Image>();

            // Sprite
            if (treeSprite != null)
            {
                img.sprite = treeSprite;
                img.preserveAspect = true;
            }

            // Random scale
            float scale = Random.Range(minScale, maxScale);
            float treeH = baseHeight * scale;
            float treeW = treeH * 0.5f; // Approximate width based on height

            rect.sizeDelta = new Vector2(treeW, treeH);

            // Position along bottom, spread evenly with some randomness
            float spacing = totalWidth / treeCount;
            float xPos = -totalWidth * 0.5f + spacing * i + Random.Range(-spacing * 0.3f, spacing * 0.3f);
            rect.anchoredPosition = new Vector2(xPos, treeH * 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Anchor to bottom
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);

            // Color with varying alpha (far trees = more transparent)
            Color c = treeColor;
            c.a = Random.Range(minAlpha, maxAlpha);
            img.color = c;
            img.raycastTarget = false;

            treeRects[i] = rect;
            treeBaseX[i] = xPos;
        }
    }

    private void Update()
    {
        if (parallaxAmount <= 0f || treeRects == null) return;

        // Subtle parallax: trees shift slightly opposite to mouse
        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;

        for (int i = 0; i < treeRects.Length; i++)
        {
            if (treeRects[i] == null) continue;

            // Bigger trees (closer) move more
            float depth = treeRects[i].sizeDelta.y / baseHeight;
            float offsetX = -mouseX * parallaxAmount * depth;

            Vector2 pos = treeRects[i].anchoredPosition;
            pos.x = treeBaseX[i] + offsetX;
            treeRects[i].anchoredPosition = pos;
        }
    }
}
