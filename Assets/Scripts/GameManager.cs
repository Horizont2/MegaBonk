using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup gameOverPanel;
    public TextMeshProUGUI timerText;

    [Header("Settings")]
    public float fadeDuration = 2f;
    public float waitBeforeRestart = 1.5f;

    public static float survivalTime = 0f;
    private bool isGameOver = false;

    private void Start()
    {
        survivalTime = 0f;
        isGameOver = false;
    }

    private void Update()
    {
        // ESC Key logic: Return to main menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }

        if (isGameOver) return;

        survivalTime += Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void TriggerGameOver()
    {
        isGameOver = true;

        // Mark that there is no active run anymore (0 = false)
        PlayerPrefs.SetInt("IsRunActive", 0);
        PlayerPrefs.Save();

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            gameOverPanel.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        yield return new WaitForSeconds(waitBeforeRestart);

        // Load the Main Menu scene instead of restarting the current scene
        SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToMenu()
    {
        // If we quit via ESC, we keep "IsRunActive" as 1 (true) so the player can "Continue"
        PlayerPrefs.SetInt("IsRunActive", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainMenu");
    }
}