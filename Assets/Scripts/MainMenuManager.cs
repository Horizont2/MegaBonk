using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI crystalsText;
    public Button continueButton;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";
    public string shopSceneName = "ShopScene"; // Õ‡Á‚‡ Ú‚Ó∫ø ÒˆÂÌË Á Ï‡„‡ÁËÌÓÏ

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        UpdateCrystalsUI();
        CheckContinueStatus();
    }

    private void CheckContinueStatus()
    {
        if (continueButton != null)
        {
            bool isActive = PlayerPrefs.GetInt("IsRunActive", 0) == 1;
            continueButton.interactable = isActive;

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

    public void StartNewRun()
    {
        PlayerPrefs.SetInt("IsRunActive", 1);
        PlayerPrefs.SetInt("IsContinuing", 0);
        PlayerPrefs.Save();

        GameManager.survivalTime = 0f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        PlayerPrefs.SetInt("IsContinuing", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // --- ÕŒ¬»… Ã≈“Œƒ ƒÀﬂ Ã¿√¿«»Õ” ---
    public void OpenShop()
    {
        SceneManager.LoadScene(shopSceneName);
    }

    public void OpenOptions()
    {
        Debug.Log("Options clicked! (Show options panel)");
    }

    public void OpenAchievements()
    {
        Debug.Log("Achievements clicked! (Show achievements panel)");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}