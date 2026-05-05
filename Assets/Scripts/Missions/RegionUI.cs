using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Image))]
public class RegionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Region Data")]
    public RegionData myRegionData;

    [Header("UI References")]
    public GameObject lockIcon;
    public Image borderImage;
    [Tooltip("Новий шар з таким самим спрайтом для ефекту бурі")]
    public Image stormLayer;

    private Image regionImage;
    private MapPanelUI mainMapUI;

    [Header("Colors - Fill States")]
    public Color lockedColor = new Color(0.1f, 0.12f, 0.18f, 0.8f); // Темне тло під бурею
    public Color availableColor = new Color(1f, 0.9f, 0.6f, 0.15f);
    public Color conqueredColor = new Color(0.8f, 0.7f, 1f, 0.25f);

    [Header("Colors - Hover")]
    public Color hoverAvailableColor = new Color(0.6f, 0.4f, 1f, 0.4f);
    public Color hoverLockedColor = new Color(0.8f, 0.2f, 0.2f, 0.4f);
    public Color hoverConqueredColor = new Color(1f, 0.8f, 0.2f, 0.4f);

    [Header("Storm Visuals")]
    public Color darkFogColor = new Color(0.08f, 0.1f, 0.15f, 0.95f); // Густий, майже чорний туман
    public Color lightFogColor = new Color(0.15f, 0.18f, 0.25f, 0.95f); // Трохи світліший сизий відтінок

    private float breathSpeed;
    private float breathOffset;

    private Color targetImageColor;
    private bool isHovered = false;
    void Awake()
    {
        regionImage = GetComponent<Image>();
        regionImage.alphaHitTestMinimumThreshold = 0.1f;
        mainMapUI = FindFirstObjectByType<MapPanelUI>(FindObjectsInactive.Include);

        // Генеруємо унікальний ритм "дихання" для кожного регіону
        breathSpeed = Random.Range(0.4f, 0.8f);
        breathOffset = Random.Range(0f, 10f);
    }

    void OnEnable()
    {
        MapTableInteract.OnMapFullyOpened += TriggerRevealAnimation; // Підписка на відкриття мапи
        UpdateRegionVisuals(false);
    }

    void OnDisable()
    {
        MapTableInteract.OnMapFullyOpened -= TriggerRevealAnimation;
    }

    void Update()
    {
        // 1. Анімація землі
        regionImage.color = Color.Lerp(regionImage.color, targetImageColor, Time.deltaTime * 10f);

        // 2. Органічний Туман Війни
        if (myRegionData != null && myRegionData.currentState == RegionState.Locked && stormLayer != null)
        {
            // Математика плавної пульсації від 0 до 1
            float pulse = (Mathf.Sin(Time.time * breathSpeed + breathOffset) + 1f) / 2f;

            // Плавно переливаємося між двома темними відтінками
            stormLayer.color = Color.Lerp(darkFogColor, lightFogColor, pulse);
        }
    }

    private void UpdateRegionVisuals(bool hover)
    {
        isHovered = hover;
        RegionState state = myRegionData != null ? myRegionData.currentState : RegionState.Locked;

        if (lockIcon != null) lockIcon.SetActive(state == RegionState.Locked);

        if (state == RegionState.Locked)
        {
            targetImageColor = hover ? hoverLockedColor : lockedColor;
            if (stormLayer != null && !myRegionData.isNewlyUnlocked) stormLayer.gameObject.SetActive(true);
        }
        else if (state == RegionState.Available)
        {
            targetImageColor = hover ? hoverAvailableColor : availableColor;
            // Якщо регіон вже давно доступний (не новий), шторму немає
            if (stormLayer != null && !myRegionData.isNewlyUnlocked) stormLayer.gameObject.SetActive(false);
        }
        else if (state == RegionState.Conquered)
        {
            targetImageColor = hover ? hoverConqueredColor : conqueredColor;
            if (stormLayer != null) stormLayer.gameObject.SetActive(false);
        }
    }

    // Тригериться, коли мапа повністю з'явилася на екрані
    private void TriggerRevealAnimation()
    {
        if (myRegionData != null && myRegionData.currentState == RegionState.Available && myRegionData.isNewlyUnlocked)
        {
            if (stormLayer != null) StartCoroutine(DissolveStormRoutine());
            myRegionData.isNewlyUnlocked = false; // Скидаємо прапорець
        }
    }

    // Анімація розвіювання бурі (AAA Juice)
    private IEnumerator DissolveStormRoutine()
    {
        stormLayer.gameObject.SetActive(true);
        float duration = 2.5f; // Шторм розсіюється 2.5 секунди
        float elapsed = 0f;

        Vector3 startScale = stormLayer.transform.localScale;
        Vector3 targetScale = startScale * 1.4f; // Хмари наближаються (розходяться)

        Color startColor = stormLayer.color;
        Color targetTransparent = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // Якщо є звук розсіювання вітру:
        // if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_WindGust);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); // Ease-Out (швидко на початку, плавно в кінці)

            stormLayer.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            stormLayer.color = Color.Lerp(startColor, targetTransparent, smoothT);

            yield return null;
        }

        stormLayer.gameObject.SetActive(false);
        stormLayer.transform.localScale = startScale; // Повертаємо масштаб на місце
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        UpdateRegionVisuals(true);
    }
    public void OnPointerExit(PointerEventData eventData) { UpdateRegionVisuals(false); }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (mainMapUI != null && myRegionData != null)
        {
            // ТЕПЕР ПЕРЕДАЄМО ДВА ПАРАМЕТРИ
            mainMapUI.OpenPanel(myRegionData, transform.position);
        }
    }
}