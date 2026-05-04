using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class RegionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Region Data")]
    public RegionData myRegionData;

    [Header("UI References")]
    public GameObject lockIcon;
    public Image borderImage;

    private Image regionImage;
    private MapPanelUI mainMapUI;

    [Header("Colors - Fill States (Туман Війни)")]
    // Туман війни: легкий холодний синювато-сірий відтінок для незвіданих земель
    public Color lockedColor = new Color(0.15f, 0.18f, 0.25f, 0.15f);
    // Доступна для атаки територія: теплий, злегка золотистий відтінок
    public Color availableColor = new Color(1f, 0.9f, 0.6f, 0.15f);
    // Захоплена: фірмовий лавандовий
    public Color conqueredColor = new Color(0.8f, 0.7f, 1f, 0.25f);

    [Header("Colors - Fill Hover")]
    public Color hoverAvailableColor = new Color(0.6f, 0.4f, 1f, 0.4f);
    public Color hoverLockedColor = new Color(0.8f, 0.2f, 0.2f, 0.4f);
    public Color hoverConqueredColor = new Color(1f, 0.8f, 0.2f, 0.4f);

    [Header("Colors - Border (Контури)")]
    public Color borderNormalColor = new Color(0.2f, 0.18f, 0.15f, 0.5f); // Напівпрозорий темно-коричневий (чорнило)
    public Color borderHoverColor = new Color(1f, 1f, 1f, 1f); // Білий при наведенні
    public Color borderPulseColor = new Color(1f, 0.8f, 0.2f, 0.8f); // Золотий для пульсації доступного регіону

    [Header("Storm Visuals (Locked Regions)")]
    public Color lightningFlashColor = new Color(0.8f, 0.9f, 1f, 0.4f); // Колір спалаху блискавки
    private float nextLightningTime;

    private Color targetImageColor;
    private Color targetBorderColor;
    private bool isHovered = false;

    void Awake()
    {
        regionImage = GetComponent<Image>();
        regionImage.alphaHitTestMinimumThreshold = 0.1f;
        mainMapUI = FindFirstObjectByType<MapPanelUI>(FindObjectsInactive.Include);

        // Рандомізуємо перший удар блискавки для кожного регіону
        nextLightningTime = Time.time + Random.Range(2f, 10f);
    }

    void OnEnable()
    {
        // Підписуємося на оновлення мапи
        MapProgressionManager.OnMapStateChanged += RefreshState;
        UpdateRegionVisuals(false);
    }

    void OnDisable()
    {
        // Відписуємося, щоб не було витоку пам'яті
        MapProgressionManager.OnMapStateChanged -= RefreshState;
    }

    private void RefreshState()
    {
        UpdateRegionVisuals(isHovered);
    }

    void Update()
    {
        // МАГІЯ НАВІГАЦІЇ: Плавно пульсуємо кордоном, якщо регіон доступний для атаки і мишка не на ньому
        if (myRegionData != null && myRegionData.currentState == RegionState.Available && !isHovered)
        {
            float pulse = Mathf.PingPong(Time.time * 1.5f, 1f); // Швидкість пульсації
            targetBorderColor = Color.Lerp(borderNormalColor, borderPulseColor, pulse);
        }

        // Застосовуємо кольори
        regionImage.color = Color.Lerp(regionImage.color, targetImageColor, Time.deltaTime * 12f);

        if (borderImage != null)
        {
            borderImage.color = Color.Lerp(borderImage.color, targetBorderColor, Time.deltaTime * 12f);
        }

        if (myRegionData != null && myRegionData.currentState == RegionState.Locked && !isHovered)
        {
            if (Time.time >= nextLightningTime)
            {
                // Різкий спалах! (Ми напряму змінюємо колір Image, минаючи Lerp)
                regionImage.color = lightningFlashColor;

                // Наступна блискавка вдарить через 4-15 секунд
                nextLightningTime = Time.time + Random.Range(4f, 15f);
            }
        }
    }

    private void UpdateRegionVisuals(bool hover)
    {
        isHovered = hover;
        RegionState state = myRegionData != null ? myRegionData.currentState : RegionState.Locked;

        if (lockIcon != null) lockIcon.SetActive(state == RegionState.Locked);

        // Встановлюємо базові цілі залежно від стану
        if (state == RegionState.Locked)
        {
            targetImageColor = hover ? hoverLockedColor : lockedColor;
            targetBorderColor = hover ? borderHoverColor : borderNormalColor;
        }
        else if (state == RegionState.Available)
        {
            targetImageColor = hover ? hoverAvailableColor : availableColor;
            // Колір кордону тут встановлюється для стану Hover. Якщо не hover, він керується пульсацією в Update()
            targetBorderColor = hover ? borderHoverColor : borderNormalColor;
        }
        else if (state == RegionState.Conquered)
        {
            targetImageColor = hover ? hoverConqueredColor : conqueredColor;
            targetBorderColor = hover ? borderHoverColor : borderNormalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        UpdateRegionVisuals(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateRegionVisuals(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        regionImage.color = new Color(1f, 1f, 1f, 0.8f);
        if (borderImage != null) borderImage.color = new Color(1f, 0.8f, 0.2f, 1f);

        if (mainMapUI != null && myRegionData != null)
        {
            mainMapUI.OpenPanel(myRegionData);
        }
    }
}