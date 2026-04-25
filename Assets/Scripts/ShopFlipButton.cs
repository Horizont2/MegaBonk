using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopFlipButton : MonoBehaviour
{
    [Header("Main Scripts")]
    public ShopManager shopManager;

    [Header("UI Elements on Button")]
    public Image iconImage;           // Перетягни сюди іконку, яка лежить на кнопці
    public TextMeshProUGUI buttonText; // Перетягни сюди текст кнопки

    [Header("Hero State (Default)")]
    public Sprite heroIcon;
    public string heroText = "HEROES";
    public Color heroColor = new Color(1f, 0.4f, 0f); // Помаранчевий

    [Header("Weapon State")]
    public Sprite weaponIcon;
    public string weaponText = "ARSENAL";
    public Color weaponColor = new Color(0f, 0.8f, 1f); // Синій

    [Header("Settings")]
    public float flipDuration = 0.3f;

    private bool isFlipping = false;
    private bool isHeroState = true;

    // Цю функцію треба прив'язати до події OnClick() самої кнопки!
    private void Start()
    {
        // Синхронізуємо вигляд кнопки з головним менеджером при старті
        if (shopManager != null)
        {
            isHeroState = (shopManager.currentMode == ShopManager.ShopMode.Heroes);

            iconImage.sprite = isHeroState ? heroIcon : weaponIcon;
            buttonText.text = isHeroState ? heroText : weaponText;

            buttonText.color = isHeroState ? heroColor : weaponColor;
            iconImage.color = isHeroState ? heroColor : weaponColor;
        }
    }
    public void OnClickFlip()
    {
        if (isFlipping) return;

        // Запускаємо візуальний переворот
        StartCoroutine(FlipAnimation());

        // Кажемо головному менеджеру змінити магазин
        if (shopManager != null)
        {
            shopManager.ToggleShopMode();
        }
    }

    private IEnumerator FlipAnimation()
    {
        isFlipping = true;
        float halfDuration = flipDuration / 2f;
        Vector3 startScale = transform.localScale;

        // Сплющуємо до нуля (ефект повороту ребром до камери)
        Vector3 flatScale = new Vector3(startScale.x, 0f, startScale.z);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDuration;
            transform.localScale = Vector3.Lerp(startScale, flatScale, t);
            yield return null;
        }

        // --- МАГІЯ ВІДБУВАЄТЬСЯ ТУТ (коли кнопка сплющена до невидимості) ---
        isHeroState = !isHeroState;

        // Підставляємо нові дані
        iconImage.sprite = isHeroState ? heroIcon : weaponIcon;
        buttonText.text = isHeroState ? heroText : weaponText;

        // Змінюємо кольори (щоб неон перемикався)
        buttonText.color = isHeroState ? heroColor : weaponColor;
        iconImage.color = isHeroState ? heroColor : weaponColor;

        // Розгортаємо назад
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDuration;
            transform.localScale = Vector3.Lerp(flatScale, startScale, t);
            yield return null;
        }

        transform.localScale = startScale;
        isFlipping = false;
    }
}