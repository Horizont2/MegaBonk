using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Hover/click effects for menu buttons. Attach to each Button GameObject.
/// Provides: lift on hover, press squish, optional color tint, and click sound.
/// </summary>
public class UIButtonEffects : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover")]
    public float hoverLift = 3f;
    public float hoverScaleMultiplier = 1.03f;
    public float transitionSpeed = 12f;

    [Header("Press")]
    public float pressScale = 0.95f;

    [Header("Optional Shadow")]
    [Tooltip("Assign a shadow Image below the button to animate on hover")]
    public Image shadowImage;
    public float shadowNormalAlpha = 0.15f;
    public float shadowHoverAlpha = 0.35f;

    private RectTransform rect;
    private Vector2 restPosition;
    private Vector3 restScale;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool initialized = false;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        // Cache rest state after MenuAnimator finishes (delayed 1 frame)
        StartCoroutine(CacheRestState());
    }

    private IEnumerator CacheRestState()
    {
        // Wait 2 seconds for entry animations to finish
        yield return new WaitForSecondsRealtime(2f);
        restPosition = rect.anchoredPosition;
        restScale = rect.localScale;
        initialized = true;

        if (shadowImage != null)
        {
            Color c = shadowImage.color;
            c.a = shadowNormalAlpha;
            shadowImage.color = c;
        }
    }

    private void Update()
    {
        if (!initialized) return;

        Button btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;

        // Target calculation
        Vector2 targetPos = restPosition;
        Vector3 targetScale = restScale;

        if (isPressed)
        {
            targetScale = restScale * pressScale;
        }
        else if (isHovered)
        {
            targetPos = restPosition + Vector2.up * hoverLift;
            targetScale = restScale * hoverScaleMultiplier;
        }

        // Smooth transition
        float dt = Time.unscaledDeltaTime * transitionSpeed;
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, dt);
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, dt);

        // Shadow alpha
        if (shadowImage != null)
        {
            float targetAlpha = isHovered ? shadowHoverAlpha : shadowNormalAlpha;
            Color c = shadowImage.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, dt);
            shadowImage.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Button btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Button btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }
}
