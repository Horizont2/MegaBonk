using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MapPathManager : MonoBehaviour
{
    public static MapPathManager Instance;

    [Header("Prefabs & Setup")]
    public GameObject pathLinePrefab;
    public Transform linesContainer;

    [Header("Settings")]
    [Tooltip("Швидкість промальовування (менше = повільніше і красивіше)")]
    public float drawSpeed = 150f;
    [Tooltip("Товщина лінії. Я зменшив її до 10")]
    public float lineHeight = 10f;

    private HashSet<string> drawnPaths = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        // Коли мапа відкривається, запускаємо малювання з невеличкою затримкою
        StartCoroutine(DrawAllPathsWithDelay());
    }

    // Метод для оновлення ліній (наприклад, одразу після захоплення)
    public void RefreshPaths()
    {
        StartCoroutine(DrawAllPathsWithDelay());
    }

    private IEnumerator DrawAllPathsWithDelay()
    {
        // Чекаємо секунду, поки екран мапи плавно з'явиться (Fade In)
        yield return new WaitForSeconds(0.4f);

        // 1. Очищаємо старі лінії
        foreach (Transform child in linesContainer)
        {
            Destroy(child.gameObject);
        }
        drawnPaths.Clear();

        // 2. Знаходимо всі UI регіонів на мапі
        RegionUI[] allRegions = FindObjectsByType<RegionUI>(FindObjectsSortMode.None);

        // 3. Збираємо тільки ЗАХОПЛЕНІ регіони
        Dictionary<int, RectTransform> regionRects = new Dictionary<int, RectTransform>();
        Dictionary<int, RegionData> regionDatas = new Dictionary<int, RegionData>();

        foreach (RegionUI rUI in allRegions)
        {
            if (rUI.myRegionData != null && rUI.myRegionData.currentState == RegionState.Conquered)
            {
                regionRects[rUI.myRegionData.regionID] = rUI.GetComponent<RectTransform>();
                regionDatas[rUI.myRegionData.regionID] = rUI.myRegionData;
            }
        }

        // 4. Малюємо лінії між сусідами
        foreach (var kvp in regionDatas)
        {
            RegionData region = kvp.Value;
            RectTransform startRect = regionRects[region.regionID];

            if (region.neighboringRegions != null)
            {
                foreach (RegionData neighbor in region.neighboringRegions)
                {
                    // Якщо сусід теж захоплений і існує на мапі
                    if (neighbor.currentState == RegionState.Conquered && regionRects.ContainsKey(neighbor.regionID))
                    {
                        RectTransform endRect = regionRects[neighbor.regionID];
                        DrawPathBetweenRegions(region.regionID, startRect, neighbor.regionID, endRect, true);
                    }
                }
            }
        }
    }

    private void DrawPathBetweenRegions(int regionA_ID, RectTransform posA, int regionB_ID, RectTransform posB, bool animate)
    {
        // Створюємо унікальний ключ, щоб не малювати дорогу туди і назад двічі
        string pathID = Mathf.Min(regionA_ID, regionB_ID) + "_" + Mathf.Max(regionA_ID, regionB_ID);

        if (drawnPaths.Contains(pathID)) return;
        drawnPaths.Add(pathID);

        StartCoroutine(AnimatePathRoutine(posA, posB, animate));
    }

    private IEnumerator AnimatePathRoutine(RectTransform start, RectTransform end, bool animate)
    {
        GameObject lineObj = Instantiate(pathLinePrefab, linesContainer);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        // Локальні координати вирішують всі проблеми з кривим відображенням
        Vector3 startPos = linesContainer.InverseTransformPoint(start.position);
        Vector3 endPos = linesContainer.InverseTransformPoint(end.position);

        Vector2 dir = endPos - startPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.localPosition = startPos;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        RectTransform inkGraphic = lineRect.GetChild(0).GetComponent<RectTransform>();
        inkGraphic.anchorMin = new Vector2(0, 0.5f);
        inkGraphic.anchorMax = new Vector2(0, 0.5f);
        inkGraphic.pivot = new Vector2(0, 0.5f);
        inkGraphic.anchoredPosition = Vector2.zero;

        // Встановлюємо правильну товщину (lineHeight)
        inkGraphic.sizeDelta = new Vector2(distance, lineHeight);

        if (animate)
        {
            lineRect.sizeDelta = new Vector2(0, lineHeight);
            float currentWidth = 0f;

            // Звук можна додати сюди, якщо потрібно

            while (currentWidth < distance)
            {
                currentWidth += Time.deltaTime * drawSpeed;
                lineRect.sizeDelta = new Vector2(currentWidth, lineHeight);
                yield return null;
            }
        }

        lineRect.sizeDelta = new Vector2(distance, lineHeight);
    }
}