using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for hover detection

public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float smoothTime = 0.1f;

    [Header("Click Settings")]
    public Color clickColor = Color.cyan;

    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;

    private void Awake()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        if (buttonImage != null) originalColor = buttonImage.color;
    }

    // When mouse enters the button area
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GetComponent<Button>().interactable)
            transform.localScale = hoverScale;
    }

    // When mouse leaves
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    // When mouse is pressed down
    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonImage != null && GetComponent<Button>().interactable)
            buttonImage.color = clickColor;
    }

    // When mouse is released
    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonImage != null)
            buttonImage.color = originalColor;
    }
}