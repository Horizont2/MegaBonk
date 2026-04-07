using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup gameOverPanel;
    public TextMeshProUGUI timerText;

    [Header("Death Stats")]
    public DeathStatsScreen deathStatsScreen;

    [Header("Settings")]
    public float fadeDuration = 2f;

    public static float survivalTime = 0f;
    private bool isGameOver = false;

    private void Start()
    {
        survivalTime = 0f;
        isGameOver = false;
        GameStats.Reset();

        // Start gameplay music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic("gameplay");
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

        // Sync final stats from player
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            GameStats.highestLevel = pc.currentLevel;
            GameStats.crystalsCollected = pc.crystalsCollected;
        }

        // Accumulate lifetime stats for achievements
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.AccumulateRunStats();
            AchievementManager.Instance.CheckAll();
        }

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // Fade in dark overlay
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (gameOverPanel != null)
                gameOverPanel.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        // Pause the game so stats screen works in frozen time
        Time.timeScale = 0f;

        // Show death stats screen
        if (deathStatsScreen != null)
        {
            deathStatsScreen.Show(
                survivalTime,
                GameStats.totalKills,
                GameStats.totalDamageDealt,
                GameStats.totalDamageTaken,
                GameStats.highestLevel,
                GameStats.crystalsCollected
            );
        }
        else
        {
            // Fallback: go straight to menu if no stats screen is assigned
            yield return new WaitForSecondsRealtime(1.5f);
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void ReturnToMenu()
    {
        PlayerPrefs.SetInt("IsRunActive", 1);

        // Save the exact player position before leaving
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);
        }

        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }
}