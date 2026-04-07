using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Button component
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI crystalsText;
    public Button continueButton; // Reference to the Continue button

    [Header("Settings")]
    public string gameSceneName = "GameScene"; // Ensure this matches your actual gameplay scene name!

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        UpdateCrystalsUI();
        CheckContinueStatus();

        // Start menu music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic("menu");
    }

    private void CheckContinueStatus()
    {
        if (continueButton != null)
        {
            // If IsRunActive is 1, button is interactable
            bool isActive = PlayerPrefs.GetInt("IsRunActive", 0) == 1;
            continueButton.interactable = isActive;

            // Optional: change alpha to look "disabled" when not active
            CanvasGroup cg = continueButton.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = isActive ? 1f : 0.5f;
        }
    }

    public void UpdateCrystalsUI()
    {
        if (crystalsText != null)
        {
            crystalsText.text = "CRYSTALS: " + SaveManager.GetTotalCrystals().ToString();
        }
    }

    // Called when "NEW RUN" (or the big PLAY button) is clicked
    public void StartNewRun()
    {
        PlayerPrefs.SetInt("IsRunActive", 1);
        PlayerPrefs.SetInt("IsContinuing", 0); // NEW: Mark as fresh run
        PlayerPrefs.Save();

        GameManager.survivalTime = 0f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        PlayerPrefs.SetInt("IsContinuing", 1); // NEW: Mark as a continued run
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // Placeholder for "OPTIONS"
    public void OpenOptions()
    {
        Debug.Log("Options clicked! (Show options panel)");
    }

    public void OpenAchievements()
    {
        AchievementsPanelUI panel = FindObjectOfType<AchievementsPanelUI>(true);
        if (panel != null) panel.Toggle();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("buttonClick");
    }

    // Called when "QUIT" is clicked
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}