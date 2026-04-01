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
        UpdateCrystalsUI();
        CheckContinueStatus();
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
        // Start a new active run
        PlayerPrefs.SetInt("IsRunActive", 1);
        PlayerPrefs.Save();

        // Reset the survival timer for the new run
        GameManager.survivalTime = 0f;

        // Load the main gameplay scene
        SceneManager.LoadScene(gameSceneName);
    }

    // Called when "CONTINUE" is clicked
    public void ContinueGame()
    {
        // Load the scene to continue the run
        SceneManager.LoadScene(gameSceneName);
    }

    // Placeholder for "OPTIONS"
    public void OpenOptions()
    {
        Debug.Log("Options clicked! (Show options panel)");
    }

    // Placeholder for "ACHIEVEMENTS"
    public void OpenAchievements()
    {
        Debug.Log("Achievements clicked! (Show achievements panel)");
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