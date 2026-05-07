using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance;

    [Header("UI Panels & Animation")]
    public GameObject settingsPanel;
    public CanvasGroup panelCanvasGroup; // НОВЕ: Для плавного зникнення
    public RectTransform panelRect;      // НОВЕ: Для ефекту виринання (Scale)
    public float animationSpeed = 10f;   // Швидкість анімації вікна

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Gameplay Toggles")]
    public Toggle damagePopupsToggle;
    public Toggle screenShakeToggle;
    public Toggle limitFPSToggle;

    [Header("Checkmark Graphics (Для анімації)")]
    public Graphic damageCheckmark;
    public Graphic screenShakeCheckmark;
    public Graphic fpsCheckmark;

    [Header("Buttons (Для анімації)")]
    public Button closeButton;
    public TextMeshProUGUI closeButtonText;
    public Button saveButton;

    private Coroutine panelAnimCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (damagePopupsToggle) damagePopupsToggle.onValueChanged.AddListener(OnDamagePopupsChanged);
        if (screenShakeToggle) screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        if (limitFPSToggle) limitFPSToggle.onValueChanged.AddListener(OnFPSLimitChanged);

        if (closeButton != null) closeButton.gameObject.AddComponent<AutoButtonAnimator>().Setup(closeButtonText, true);
        if (saveButton != null) saveButton.gameObject.AddComponent<AutoButtonAnimator>().Setup(null, false);
    }

    private void Update()
    {
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            AnimateCheckmark(damageCheckmark, damagePopupsToggle.isOn);
            AnimateCheckmark(screenShakeCheckmark, screenShakeToggle.isOn);
            AnimateCheckmark(fpsCheckmark, limitFPSToggle.isOn);
        }
    }

    private void AnimateCheckmark(Graphic checkmark, bool isOn)
    {
        if (checkmark == null) return;
        float targetAlpha = isOn ? 1f : 0f;
        Vector3 targetScale = isOn ? Vector3.one : Vector3.zero;
        float speed = 18f * Time.unscaledDeltaTime;

        checkmark.rectTransform.localScale = Vector3.Lerp(checkmark.rectTransform.localScale, targetScale, speed);
        Color c = checkmark.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, speed);
        checkmark.color = c;
    }

    // --- ЛОГІКА ВІДКРИТТЯ З АНІМАЦІЄЮ ---
    public void OpenSettings()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);

        if (SceneManager.GetActiveScene().name != "Menu") Time.timeScale = 0f;

        settingsPanel.SetActive(true);

        settingsPanel.transform.SetAsLastSibling();

        if (masterSlider) masterSlider.value = PlayerPrefs.GetFloat("Settings_MasterVol", 1f);
        if (musicSlider) musicSlider.value = PlayerPrefs.GetFloat("Settings_MusicVol", 1f);
        if (sfxSlider) sfxSlider.value = PlayerPrefs.GetFloat("Settings_SFXVol", 1f);

        if (damagePopupsToggle) damagePopupsToggle.isOn = PlayerPrefs.GetInt("Settings_DamagePopups", 1) == 1;
        if (screenShakeToggle) screenShakeToggle.isOn = PlayerPrefs.GetInt("Settings_ScreenShake", 1) == 1;
        if (limitFPSToggle) limitFPSToggle.isOn = PlayerPrefs.GetInt("Settings_FPSLimit", 1) == 1;

        ForceCheckmarkState(damageCheckmark, damagePopupsToggle.isOn);
        ForceCheckmarkState(screenShakeCheckmark, screenShakeToggle.isOn);
        ForceCheckmarkState(fpsCheckmark, limitFPSToggle.isOn);

        if (saveButton != null)
        {
            TextMeshProUGUI btnText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = SceneManager.GetActiveScene().name == "Menu" ? "SAVE & CLOSE" : "SAVE & QUIT TO MENU";
        }

        // Запуск анімації появи
        if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);
        panelAnimCoroutine = StartCoroutine(AnimatePanelIn());
    }

    private IEnumerator AnimatePanelIn()
    {
        if (panelCanvasGroup == null || panelRect == null) yield break;

        panelCanvasGroup.alpha = 0f;
        panelRect.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * animationSpeed;
            float easeOutQuart = 1f - Mathf.Pow(1f - t, 4f);

            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeOutQuart);
            panelRect.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 0.8f), Vector3.one, easeOutQuart);

            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;
    }

    // --- ЛОГІКА ЗАКРИТТЯ З АНІМАЦІЄЮ ---
    public void CloseSettings()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        PlayerPrefs.Save();

        if (panelAnimCoroutine != null) StopCoroutine(panelAnimCoroutine);
        panelAnimCoroutine = StartCoroutine(AnimatePanelOut());
    }

    private IEnumerator AnimatePanelOut()
    {
        if (panelCanvasGroup == null || panelRect == null)
        {
            FinishClosing();
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * (animationSpeed * 1.5f);
            float easeInQuad = t * t;

            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, easeInQuad);
            panelRect.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.9f, 0.9f, 0.9f), easeInQuad);

            yield return null;
        }

        FinishClosing();
    }

    private void FinishClosing()
    {
        settingsPanel.SetActive(false);

        if (SceneManager.GetActiveScene().name == "Menu") return;

        bool isPauseMenuOpen = GlobalHUD.Instance != null && GlobalHUD.Instance.pausePanelGroup.gameObject.activeSelf;
        if (!isPauseMenuOpen)
        {
            Time.timeScale = 1f;
        }
    }

    private void ForceCheckmarkState(Graphic checkmark, bool isOn)
    {
        if (checkmark == null) return;
        checkmark.rectTransform.localScale = isOn ? Vector3.one : Vector3.zero;
        Color c = checkmark.color;
        c.a = isOn ? 1f : 0f;
        checkmark.color = c;
    }

    public void ReturnToMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_Click);
        PlayerPrefs.Save();

        if (SceneManager.GetActiveScene().name == "Menu")
        {
            CloseSettings();
            return;
        }

        Time.timeScale = 1f;

        if (PlayerPrefs.GetInt("IsRegionMission", 0) == 1)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
                PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
                PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);
            }
        }

        PlayerPrefs.SetInt("IsRunActive", 1);
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene("Menu");
        else SceneManager.LoadScene("Menu");
    }

    private void OnMasterVolumeChanged(float value) { if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value); }
    private void OnMusicVolumeChanged(float value) { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value); }
    private void OnSFXVolumeChanged(float value) { if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value); }

    private void OnDamagePopupsChanged(bool isOn) { if (AudioManager.Instance != null && settingsPanel.activeSelf) AudioManager.Instance.PlayUI(AudioID.UI_Hover); PlayerPrefs.SetInt("Settings_DamagePopups", isOn ? 1 : 0); }
    private void OnScreenShakeChanged(bool isOn) { if (AudioManager.Instance != null && settingsPanel.activeSelf) AudioManager.Instance.PlayUI(AudioID.UI_Hover); PlayerPrefs.SetInt("Settings_ScreenShake", isOn ? 1 : 0); }
    private void OnFPSLimitChanged(bool isOn) { if (AudioManager.Instance != null && settingsPanel.activeSelf) AudioManager.Instance.PlayUI(AudioID.UI_Hover); PlayerPrefs.SetInt("Settings_FPSLimit", isOn ? 1 : 0); Application.targetFrameRate = isOn ? 60 : -1; }
}

// Внутрішній клас для анімації кнопок
public class AutoButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI textToColor;
    private bool isCloseButton;
    private Vector3 targetScale = Vector3.one;
    private Color targetColor;

    private Color normalColor = new Color(1f, 0.84f, 0f, 0.8f);
    private Color hoverColor = new Color(1f, 0.26f, 0.26f, 1f);

    public void Setup(TextMeshProUGUI textRef, bool isCloseBtn)
    {
        textToColor = textRef;
        isCloseButton = isCloseBtn;
        targetColor = normalColor;
        if (textToColor != null) textToColor.color = normalColor;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 15f);
        if (textToColor != null) textToColor.color = Color.Lerp(textToColor.color, targetColor, Time.unscaledDeltaTime * 15f);
    }

    public void OnPointerEnter(PointerEventData eventData) { if (AudioManager.Instance != null) AudioManager.Instance.PlayUI("UI_Hover"); targetScale = Vector3.one * 1.05f; if (isCloseButton) targetColor = hoverColor; }
    public void OnPointerExit(PointerEventData eventData) { targetScale = Vector3.one; if (isCloseButton) targetColor = normalColor; }
    public void OnPointerDown(PointerEventData eventData) { targetScale = Vector3.one * 0.95f; }
    public void OnPointerUp(PointerEventData eventData) { targetScale = Vector3.one * 1.05f; }
}