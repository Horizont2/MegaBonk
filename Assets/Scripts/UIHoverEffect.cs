using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Додаємо для доступу до Button

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Налаштування анімації")]
    public float hoverScale = 1.05f;
    public float clickScale = 0.95f;
    public float speed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Button myButton; // Посилання на кнопку

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        myButton = GetComponent<Button>(); // Отримуємо компонент кнопки
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Якщо кнопка є і вона вимкнена - нічого не робимо
        if (myButton != null && !myButton.interactable) return;
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (myButton != null && !myButton.interactable) return;
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (myButton != null && !myButton.interactable) return;
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (myButton != null && !myButton.interactable) return;
        targetScale = originalScale * hoverScale;
    }
}