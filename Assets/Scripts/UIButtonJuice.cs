using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private RectTransform rectTransform;

    [Header("Idle Animation")]
    public float bobIntensity = 5f;
    public float bobSpeed = 2f;

    private float randomOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        randomOffset = Random.Range(0, 100f);
    }

    void Update()
    {
        // Постійне легке погойдування вгору-вниз
        float newY = Mathf.Sin(Time.unscaledTime * bobSpeed + randomOffset) * bobIntensity;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleRoutine(originalScale * 1.15f));
        // Можна додати звук наведення тут
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleRoutine(originalScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.01f)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * 15f);
            yield return null;
        }
        rectTransform.localScale = targetScale;
    }
}