using UnityEngine;
using UnityEngine.UI;

public class MinimapIconTracker : MonoBehaviour
{
    [Header("References")]
    public RectTransform minimapRect; // Твій MinimapBase (220x220)
    public Image iconImage;
    public Camera minimapCamera; // Перетягни сюди камеру мінімапи

    [Header("Settings")]
    public float iconMargin = 10f;

    private Transform player;
    private RectTransform iconRect;
    private float minimapRadius;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        iconRect = GetComponent<RectTransform>();

        if (minimapRect != null)
            minimapRadius = (minimapRect.sizeDelta.x / 2f) - iconMargin;

        if (minimapCamera == null)
            minimapCamera = FindFirstObjectByType<MinimapCamera>().GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (player == null || minimapCamera == null) return;

        GameObject nearestHorse = FindNearestExtractionPoint();
        if (nearestHorse == null)
        {
            iconImage.enabled = false;
            return;
        }

        iconImage.enabled = true;

        // 1. Отримуємо відносну позицію коня до гравця
        Vector3 relPos = nearestHorse.transform.position - player.position;

        // 2. МАГІЯ ТОЧНОСТІ: Вираховуємо реальний масштаб на основі камери
        float unitsInView;
        if (minimapCamera.orthographic)
        {
            unitsInView = minimapCamera.orthographicSize * 2f;
        }
        else
        {
            // Для Perspective камери на висоті 40
            unitsInView = 2f * minimapCamera.transform.position.y * Mathf.Tan(minimapCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        // Скільки пікселів UI припадає на 1 ігровий метр
        float pixelsPerMeter = minimapRect.sizeDelta.x / unitsInView;

        // 3. Рахуємо фінальну позицію
        Vector2 uiPos = new Vector2(relPos.x, relPos.z) * pixelsPerMeter;

        // 4. Обмежуємо колом, якщо об'єкт за межами видимості
        if (uiPos.magnitude > minimapRadius)
        {
            uiPos = uiPos.normalized * minimapRadius;
        }

        iconRect.anchoredPosition = uiPos;
    }

    private GameObject FindNearestExtractionPoint()
    {
        // Переконайся, що у коней є тег "ExtractionPoint"
        GameObject[] points = GameObject.FindGameObjectsWithTag("ExtractionPoint");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject p in points)
        {
            float dist = Vector3.Distance(player.position, p.transform.position);
            if (dist < minDist) { minDist = dist; nearest = p; }
        }
        return nearest;
    }
}