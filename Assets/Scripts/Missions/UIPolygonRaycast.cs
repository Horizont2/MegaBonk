using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PolygonCollider2D))]
public class UIPolygonRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    private PolygonCollider2D myCollider;

    void Awake()
    {
        myCollider = GetComponent<PolygonCollider2D>();
    }

    // Цей метод змушує Unity ігнорувати прозорі кути і рахувати клік ТІЛЬКИ всередині колайдера
    public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
    {
        Vector3 worldPoint;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(GetComponent<RectTransform>(), screenPos, eventCamera, out worldPoint);

        return myCollider.OverlapPoint(worldPoint);
    }
}