using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime minimap system. Creates its own camera and UI.
/// Attach to any GameObject in the game scene.
/// Shows player (white), enemies (red dots), bosses (large red), crystals (cyan).
/// </summary>
public class MinimapSystem : MonoBehaviour
{
    [Header("Minimap Settings")]
    public float mapSize = 80f;
    public float mapHeight = 200f;
    public int textureResolution = 256;
    public float uiSize = 200f;
    public float borderWidth = 3f;

    [Header("Marker Settings")]
    public float enemyScanRadius = 60f;
    public float markerRefreshRate = 0.15f;
    public int maxEnemyMarkers = 40;

    [Header("Colors")]
    public Color borderColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Color playerColor = Color.white;
    public Color enemyColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    public Color bossColor = new Color(1f, 0f, 0f, 1f);
    public Color crystalColor = new Color(0.3f, 1f, 1f, 0.5f);

    private Camera minimapCamera;
    private RenderTexture renderTexture;
    private RawImage minimapImage;
    private Image borderImage;
    private Image playerMarker;
    private Canvas canvas;

    // Marker pools
    private Image[] enemyMarkers;
    private RectTransform minimapRect;
    private Transform playerTransform;
    private float markerTimer;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        CreateMinimapCamera();
        CreateMinimapUI();
        CreateMarkers();
    }

    private void CreateMinimapCamera()
    {
        GameObject camObj = new GameObject("MinimapCamera");
        camObj.transform.SetParent(transform);
        minimapCamera = camObj.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = mapSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.12f, 0.15f, 0.12f, 1f);
        minimapCamera.cullingMask = 1; // Only Default layer (terrain)
        minimapCamera.depth = -10;

        renderTexture = new RenderTexture(textureResolution, textureResolution, 16);
        renderTexture.filterMode = FilterMode.Bilinear;
        minimapCamera.targetTexture = renderTexture;
    }

    private void CreateMinimapUI()
    {
        // Find existing canvas or create one
        canvas = FindExistingGameCanvas();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MinimapCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Border (circle-ish frame)
        GameObject borderObj = new GameObject("MinimapBorder");
        borderObj.transform.SetParent(canvas.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(1, 1);
        borderRect.anchorMax = new Vector2(1, 1);
        borderRect.pivot = new Vector2(1, 1);
        borderRect.anchoredPosition = new Vector2(-15, -15);
        borderRect.sizeDelta = new Vector2(uiSize + borderWidth * 2, uiSize + borderWidth * 2);
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;

        // Map image
        GameObject mapObj = new GameObject("MinimapImage");
        mapObj.transform.SetParent(borderObj.transform, false);
        minimapRect = mapObj.AddComponent<RectTransform>();
        minimapRect.anchorMin = Vector2.zero;
        minimapRect.anchorMax = Vector2.one;
        minimapRect.offsetMin = new Vector2(borderWidth, borderWidth);
        minimapRect.offsetMax = new Vector2(-borderWidth, -borderWidth);
        minimapImage = mapObj.AddComponent<RawImage>();
        minimapImage.texture = renderTexture;

        // Mask for circular shape
        Mask mask = borderObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Player marker (center dot)
        GameObject playerDot = new GameObject("PlayerDot");
        playerDot.transform.SetParent(mapObj.transform, false);
        RectTransform dotRect = playerDot.AddComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(8, 8);
        playerMarker = playerDot.AddComponent<Image>();
        playerMarker.color = playerColor;

        // Player direction indicator (small triangle above dot)
        GameObject dirObj = new GameObject("DirIndicator");
        dirObj.transform.SetParent(playerDot.transform, false);
        RectTransform dirRect = dirObj.AddComponent<RectTransform>();
        dirRect.anchoredPosition = new Vector2(0, 7);
        dirRect.sizeDelta = new Vector2(4, 6);
        Image dirImage = dirObj.AddComponent<Image>();
        dirImage.color = playerColor;
    }

    private void CreateMarkers()
    {
        enemyMarkers = new Image[maxEnemyMarkers];
        for (int i = 0; i < maxEnemyMarkers; i++)
        {
            GameObject marker = new GameObject("EnemyMarker_" + i);
            marker.transform.SetParent(minimapRect.transform, false);
            RectTransform rect = marker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(5, 5);
            Image img = marker.AddComponent<Image>();
            img.color = enemyColor;
            marker.SetActive(false);
            enemyMarkers[i] = img;
        }
    }

    private Canvas FindExistingGameCanvas()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder < 50)
                return c;
        }
        return null;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // Move minimap camera above player, looking straight down
        minimapCamera.transform.position = new Vector3(
            playerTransform.position.x,
            mapHeight,
            playerTransform.position.z
        );
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Rotate player direction indicator
        if (playerMarker != null)
        {
            float yRotation = playerTransform.eulerAngles.y;
            playerMarker.rectTransform.localRotation = Quaternion.Euler(0, 0, -yRotation);
        }

        // Update enemy markers periodically
        markerTimer += Time.deltaTime;
        if (markerTimer >= markerRefreshRate)
        {
            markerTimer = 0f;
            UpdateEnemyMarkers();
        }
    }

    private void UpdateEnemyMarkers()
    {
        // Hide all markers first
        for (int i = 0; i < maxEnemyMarkers; i++)
            enemyMarkers[i].gameObject.SetActive(false);

        // Find all active enemies
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        int markerIndex = 0;
        float halfMapSize = mapSize;
        float halfUISize = (uiSize - borderWidth * 2) * 0.5f;

        foreach (EnemyAI enemy in enemies)
        {
            if (markerIndex >= maxEnemyMarkers) break;
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            Vector3 offset = enemy.transform.position - playerTransform.position;
            float dist = new Vector2(offset.x, offset.z).magnitude;
            if (dist > enemyScanRadius) continue;

            // Convert world offset to minimap UI position
            float normalizedX = offset.x / halfMapSize;
            float normalizedZ = offset.z / halfMapSize;

            // Clamp to minimap bounds
            normalizedX = Mathf.Clamp(normalizedX, -0.95f, 0.95f);
            normalizedZ = Mathf.Clamp(normalizedZ, -0.95f, 0.95f);

            Image marker = enemyMarkers[markerIndex];
            marker.gameObject.SetActive(true);

            RectTransform rect = marker.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(normalizedX * halfUISize, normalizedZ * halfUISize);

            // Boss enemies get bigger, redder markers
            BossEnemy boss = enemy.GetComponent<BossEnemy>();
            if (boss != null)
            {
                rect.sizeDelta = new Vector2(10, 10);
                marker.color = bossColor;
            }
            else
            {
                rect.sizeDelta = new Vector2(5, 5);
                marker.color = enemyColor;
            }

            markerIndex++;
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
