using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MapInteractiveViewer : MonoBehaviour, IDragHandler, IScrollHandler
{
    private RectTransform mapRect;
    private RectTransform parentViewport;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.2f;
    public float minZoom = 1.0f;
    public float maxZoom = 4f;
    public float smoothZoomSpeed = 10f;
    private float targetZoom = 1.2f;

    [Header("Drag Settings")]
    public float smoothDragSpeed = 15f;
    private Vector2 targetPosition;

    [Header("Parallax Effect")]
    [Tooltip("Шар з намальованими хмарами або заднім фоном")]
    public RectTransform parallaxLayer;
    [Tooltip("Наскільки сильно рухається фон (0.1 = повільно, 0.5 = швидко)")]
    public float parallaxStrength = 0.15f;

    private void Awake()
    {
        mapRect = GetComponent<RectTransform>();
        parentViewport = transform.parent.GetComponent<RectTransform>();
        targetPosition = mapRect.anchoredPosition;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    // --- НОВЕ: Автоматично центруємо мапу при кожному її відкритті ---
    private void OnEnable()
    {
        // Запускаємо через корутину (чекаємо 1 кадр), щоб всі RegionUI встигли завантажитись
        StartCoroutine(AutoFocusRoutine());
    }

    private IEnumerator AutoFocusRoutine()
    {
        yield return null; 
        AutoFocusOnLatestRegion();
    }

    private void Update()
    {
        float currentZoom = Mathf.Lerp(mapRect.localScale.x, targetZoom, Time.deltaTime * smoothZoomSpeed);
        mapRect.localScale = new Vector3(currentZoom, currentZoom, 1f);

        mapRect.anchoredPosition = Vector2.Lerp(mapRect.anchoredPosition, targetPosition, Time.deltaTime * smoothDragSpeed);

        if (parallaxLayer != null)
        {
            parallaxLayer.anchoredPosition = mapRect.anchoredPosition * parallaxStrength;
            parallaxLayer.localScale = Vector3.one * (1f + (currentZoom - 1f) * (parallaxStrength / 2f));
        }

        ClampPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y;
        targetZoom += scrollDelta * zoomSpeed;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentViewport, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentViewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 prevLocalPoint);

        targetPosition += (localPoint - prevLocalPoint);
    }

    // --- НОВА ФУНКЦІЯ: Фокусування на конкретному регіоні ---
    public void FocusOnNode(RectTransform nodeRect, bool instant = true)
    {
        // Встановлюємо приємний зум для огляду
        targetZoom = Mathf.Clamp(2.5f, minZoom, maxZoom);

        // Вираховуємо локальну позицію вузла відносно мапи
        Vector2 nodeLocalPos = nodeRect.localPosition;
        
        // Зсуваємо мапу в протилежний бік
        targetPosition = -nodeLocalPos * targetZoom;

        if (instant)
        {
            mapRect.localScale = new Vector3(targetZoom, targetZoom, 1f);
            mapRect.anchoredPosition = targetPosition;
            ClampPosition(); 
            targetPosition = mapRect.anchoredPosition; 
        }
    }

    // --- НОВА ФУНКЦІЯ: Пошук найактуальнішого регіону ---
    public void AutoFocusOnLatestRegion()
    {
        RegionUI[] allRegions = GetComponentsInChildren<RegionUI>(true);
        if (allRegions.Length == 0) allRegions = FindObjectsByType<RegionUI>(FindObjectsSortMode.None);

        RegionUI targetNode = null;

        // Пріоритет 1: Щойно розблокований регіон (Сюди гравець має піти зараз)
        foreach (var node in allRegions)
        {
            if (node.myRegionData != null && node.myRegionData.currentState == RegionState.Available && node.myRegionData.isNewlyUnlocked)
            {
                targetNode = node;
                break;
            }
        }

        // Пріоритет 2: Будь-який доступний регіон (Якщо гравець ще не пройшов поточну зону)
        if (targetNode == null)
        {
            foreach (var node in allRegions)
            {
                if (node.myRegionData != null && node.myRegionData.currentState == RegionState.Available)
                {
                    targetNode = node;
                    break;
                }
            }
        }

        // Пріоритет 3: Останній пройдений регіон (Якщо гра пройдена повністю)
        if (targetNode == null)
        {
            foreach (var node in allRegions)
            {
                if (node.myRegionData != null && node.myRegionData.currentState == RegionState.Conquered)
                {
                    targetNode = node; // Не робимо break, щоб цикл дійшов до останнього
                }
            }
        }

        // Якщо знайшли щось логічне - центруємо камеру на ньому!
        if (targetNode != null)
        {
            FocusOnNode(targetNode.GetComponent<RectTransform>(), true);
        }
    }

    private void ClampPosition()
    {
        float viewWidth = parentViewport.rect.width;
        float viewHeight = parentViewport.rect.height;

        float minZoomX = viewWidth / mapRect.rect.width;
        float minZoomY = viewHeight / mapRect.rect.height;
        float dynamicMinZoom = Mathf.Max(minZoom, Mathf.Max(minZoomX, minZoomY));

        targetZoom = Mathf.Clamp(targetZoom, dynamicMinZoom, maxZoom);

        float currentScale = mapRect.localScale.x;
        float mapWidth = mapRect.rect.width * currentScale;
        float mapHeight = mapRect.rect.height * currentScale;

        float maxX = Mathf.Max(0, (mapWidth - viewWidth) / 2f);
        float maxY = Mathf.Max(0, (mapHeight - viewHeight) / 2f);

        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -maxY, maxY);

        Vector2 clampedPos = mapRect.anchoredPosition;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -maxX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, -maxY, maxY);
        mapRect.anchoredPosition = clampedPos;
    }
}