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

    private float nextSurvivalTick = 1f; // Таймер для зарахування місії

    private void Start()
    {
        survivalTime = 0f;
        nextSurvivalTick = 1f;
        isGameOver = false;
    }

    private void Update()
    {
        if (isGameOver) return;

        survivalTime += Time.deltaTime;

        // ЩОСЕКУНДИ ДОДАЄМО ПРОГРЕС МІСІЇ
        if (survivalTime >= nextSurvivalTick)
        {
            nextSurvivalTick += 1f;
            if (MissionManager.Instance != null)
            {
                // Якщо твій тип місії називається інакше, зміни MissionType.Survive на свій
                MissionManager.Instance.AddProgress(MissionType.Survive, 1);
            }
        }

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

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene("Menu");
        else SceneManager.LoadScene("Menu");
    }

    public void ReturnToMenu()
    {
        PlayerPrefs.SetInt("IsRunActive", 1);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);
        }

        PlayerPrefs.Save();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene("Menu");
        else SceneManager.LoadScene("Menu");
    }
}