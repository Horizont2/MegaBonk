using UnityEngine;
using UnityEngine.EventSystems;

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

    private void Update()
    {
        float currentZoom = Mathf.Lerp(mapRect.localScale.x, targetZoom, Time.deltaTime * smoothZoomSpeed);
        mapRect.localScale = new Vector3(currentZoom, currentZoom, 1f);

        mapRect.anchoredPosition = Vector2.Lerp(mapRect.anchoredPosition, targetPosition, Time.deltaTime * smoothDragSpeed);

        // --- НОВЕ: ПАРАЛАКС ---
        if (parallaxLayer != null)
        {
            // Фон рухається разом з мапою, але повільніше, створюючи ілюзію глибини
            parallaxLayer.anchoredPosition = mapRect.anchoredPosition * parallaxStrength;
            // Також можна трохи скейлити фон разом із зумом
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
        // Ідеальний вирахунок координат для Screen Space - Camera
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentViewport, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentViewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 prevLocalPoint);

        targetPosition += (localPoint - prevLocalPoint);
    }

    private void ClampPosition()
    {
        float viewWidth = parentViewport.rect.width;
        float viewHeight = parentViewport.rect.height;

        // 1. ДИНАМІЧНИЙ МІНІМАЛЬНИЙ ЗУМ
        // Ніколи не даємо гравцю віддалити мапу так, щоб вона стала меншою за Viewport
        float minZoomX = viewWidth / mapRect.rect.width;
        float minZoomY = viewHeight / mapRect.rect.height;
        float dynamicMinZoom = Mathf.Max(minZoom, Mathf.Max(minZoomX, minZoomY));

        targetZoom = Mathf.Clamp(targetZoom, dynamicMinZoom, maxZoom);

        // 2. ВИТРИМКА МЕЖ (Обмеження)
        float currentScale = mapRect.localScale.x;
        float mapWidth = mapRect.rect.width * currentScale;
        float mapHeight = mapRect.rect.height * currentScale;

        float maxX = Mathf.Max(0, (mapWidth - viewWidth) / 2f);
        float maxY = Mathf.Max(0, (mapHeight - viewHeight) / 2f);

        // Обмежуємо цільову позицію (щоб гравець не міг тягнути мапу далі)
        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -maxY, maxY);

        // 3. ЖОРСТКЕ ОБМЕЖЕННЯ ПОТОЧНОЇ ПОЗИЦІЇ
        // Це блокує "вилітання" за краї екрана під час швидкого свайпу або зуму
        Vector2 clampedPos = mapRect.anchoredPosition;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -maxX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, -maxY, maxY);
        mapRect.anchoredPosition = clampedPos;
    }
}