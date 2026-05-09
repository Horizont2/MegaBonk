using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UpgradeButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image bgImage;       // Підкладка картки (щоб змінювати колір)
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statText;
    public Button buttonComponent;

    [Header("AAA Colors")]
    // Нормальний стан: Світліший, благородний синій, щоб картка виділялася на фоні панелі
    public Color normalColor = new Color(0.12f, 0.18f, 0.35f, 0.6f);

    // При наведенні: Тепле, насичене бурштинове/золоте світіння
    public Color hoverColor = new Color(0.7f, 0.55f, 0.12f, 0.8f);

    // При виборі (кліку/рулетці): Яскраве, чисте непрозоре золото (чорний текст на ньому читатиметься ідеально)
    public Color selectedColor = new Color(1f, 0.84f, 0f, 1f);

    private void Awake()
    {
        if (buttonComponent == null) buttonComponent = GetComponent<Button>();
        ResetVisuals();
    }

    // Коли мишка наводиться на картку
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonComponent.interactable)
        {
            bgImage.color = hoverColor;
            transform.localScale = new Vector3(1.02f, 1.02f, 1.02f); // Мікро-збільшення
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Hover);
        }
    }

    // Коли мишка йде з картки
    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonComponent.interactable)
        {
            ResetVisuals();
        }
    }

    public void ResetVisuals()
    {
        bgImage.color = normalColor;
        transform.localScale = Vector3.one;
    }

    // Викликається рулеткою або при кліку
    public void HighlightAsSelected()
    {
        bgImage.color = selectedColor;
        transform.localScale = new Vector3(1.05f, 1.05f, 1.05f); // Потужний акцент
        titleText.color = Color.black; // Текст стає чорним на золотому фоні
        descriptionText.color = Color.black;
    }

    public void ResetTextColors()
    {
        titleText.color = new Color(1f, 0.84f, 0f, 1f); // Повертаємо золото
        descriptionText.color = Color.gray;
    }
}