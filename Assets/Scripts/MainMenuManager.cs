using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    [Header("Crystal Bar")]
    public TextMeshProUGUI crystalCountText;

    [Header("Buttons")]
    public Button bonkButton;
    public Button continueButton;
    public Button achievementsButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Continue Disabled Look")]
    [Tooltip("CanvasGroup on the Continue button to control alpha when disabled")]
    public CanvasGroup continueCanvasGroup;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Wire up button events
        if (bonkButton != null) bonkButton.onClick.AddListener(StartNewRun);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (achievementsButton != null) achievementsButton.onClick.AddListener(OpenAchievements);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        UpdateCrystalsUI();
        CheckContinueStatus();

        // Start menu music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic("menu");
    }

    private void CheckContinueStatus()
    {
        if (continueButton == null) return;

        bool hasActiveRun = PlayerPrefs.GetInt("IsRunActive", 0) == 1;
        continueButton.interactable = hasActiveRun;

        if (continueCanvasGroup != null)
            continueCanvasGroup.alpha = hasActiveRun ? 1f : 0.3f;
    }

    public void UpdateCrystalsUI()
    {
        if (crystalCountText != null)
            crystalCountText.text = SaveManager.GetTotalCrystals().ToString("N0");
    }

    // ─── BUTTON ACTIONS ───

    public void StartNewRun()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("buttonClick");

        PlayerPrefs.SetInt("IsRunActive", 1);
        PlayerPrefs.SetInt("IsContinuing", 0);

        // Clear saved run state
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.DeleteKey("SavedXP");
        PlayerPrefs.DeleteKey("SavedXPToNext");
        PlayerPrefs.DeleteKey("SavedHealth");
        PlayerPrefs.DeleteKey("SavedMaxHealth");
        PlayerPrefs.DeleteKey("SavedCrystals");
        PlayerPrefs.DeleteKey("SavedSurvivalTime");
        PlayerPrefs.DeleteKey("SavedTotalKills");
        PlayerPrefs.DeleteKey("SavedDamageDealt");
        PlayerPrefs.DeleteKey("SavedDamageTaken");
        PlayerPrefs.Save();

        GameManager.survivalTime = 0f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("buttonClick");

        PlayerPrefs.SetInt("IsContinuing", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenAchievements()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("buttonClick");

        AchievementsPanelUI panel = FindObjectOfType<AchievementsPanelUI>(true);
        if (panel != null) panel.Toggle();
    }

    public void OpenOptions()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("buttonClick");
        Debug.Log("Options clicked! (Show options panel)");
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("buttonClick");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
