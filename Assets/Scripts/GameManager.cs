using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup gameOverPanel;

    [Header("Settings")]
    public float fadeDuration = 2f;
    public float waitBeforeRestart = 1.5f;

    public void TriggerGameOver()
    {
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // Плавно збільшуємо прозорість (Alpha) панелі від 0 до 1
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            gameOverPanel.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        // Чекаємо кілька секунд перед рестартом
        yield return new WaitForSeconds(waitBeforeRestart);

        // Перезавантажуємо поточну сцену (скидання гри)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}