using UnityEngine;
using UnityEngine.EventSystems;

public class MapDragger : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform mapRect;
    private RectTransform parentCanvasRect;

    [Header("Drag Settings")]
    public float dragSpeed = 1f;

    private Vector2 startDragPosition;
    private Vector2 startMapPosition;

    private void Awake()
    {
        mapRect = GetComponent<RectTransform>();

        // Шукаємо Canvas, у якому лежить ця мапа (наприклад MapCanvas)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            parentCanvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Запам'ятовуємо позицію мишки та мапи на момент початку кліку
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, eventData.position, eventData.pressEventCamera, out startDragPosition);
        startMapPosition = mapRect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, eventData.position, eventData.pressEventCamera, out currentMousePosition))
        {
            Vector2 dragDelta = (currentMousePosition - startDragPosition) * dragSpeed;
            Vector2 newPosition = startMapPosition + dragDelta;

            // ОБМЕЖЕННЯ В ИХОДУ ЗА КРАЇ (CLAMPING)
            // Вираховуємо різницю між розміром мапи (наприклад 2000x2000) та розміром екрану (наприклад 1920x1080)
            float mapWidth = mapRect.rect.width * mapRect.localScale.x;
            float mapHeight = mapRect.rect.height * mapRect.localScale.y;
            float canvasWidth = parentCanvasRect.rect.width;
            float canvasHeight = parentCanvasRect.rect.height;

            // Скільки мапа може рухатися вліво/вправо/вгору/вниз (половина різниці)
            float maxX = Mathf.Max(0, (mapWidth - canvasWidth) / 2f);
            float maxY = Mathf.Max(0, (mapHeight - canvasHeight) / 2f);

            // Обмежуємо координати
            newPosition.x = Mathf.Clamp(newPosition.x, -maxX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, -maxY, maxY);

            mapRect.anchoredPosition = newPosition;
        }
    }
}