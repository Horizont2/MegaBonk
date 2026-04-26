using UnityEngine;
using UnityEngine.SceneManagement;

public class ExtractionPortal : MonoBehaviour
{
    [Header("Transition Settings")]
    public string campSceneName = "CampScene";
    public string promptMessage = "Press E to Return to Camp";

    [Header("Visuals (Optional)")]
    public ParticleSystem portalVFX; // Якщо хочеш, щоб портал світився
    public AudioSource extractAudio;

    private bool isPlayerNear = false;
    private PlayerController playerRef;
    private bool isExtracting = false;

    private void Start()
    {
        if (portalVFX != null && !portalVFX.isPlaying)
        {
            portalVFX.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isExtracting) return;

        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerRef = other.GetComponent<PlayerController>();

            if (GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.ShowPrompt(promptMessage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isExtracting) return;

        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerRef = null;

            if (GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.HidePrompt();
            }
        }
    }

    private void Update()
    {
        if (isPlayerNear && !isExtracting && Input.GetKeyDown(KeyCode.E))
        {
            StartExtraction();
        }
    }

    private void StartExtraction()
    {
        isExtracting = true; // Блокуємо повторні натискання
        isPlayerNear = false;

        if (extractAudio != null) extractAudio.Play();

        // 1. ХОВАЄМО ПІДКАЗКУ
        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.HidePrompt();
        }

        // 2. ЗБЕРІГАЄМО ЛУТ ТА ПРОГРЕС
        if (playerRef != null)
        {
            // Зберігаємо кристали, які гравець зібрав за цей забіг
            SaveManager.AddCrystals(playerRef.crystalsCollected);

            // Якщо є інші параметри для збереження (наприклад досвід) - їх можна додати сюди
            PlayerPrefs.SetFloat("SavedXP", playerRef.currentXP);
            PlayerPrefs.SetInt("SavedLevel", playerRef.currentLevel);
            PlayerPrefs.Save();
        }

        // 3. ПЛАВНИЙ ПЕРЕХІД
        if (GlobalHUD.Instance != null)
        {
            GlobalHUD.Instance.FadeAndLoadScene(campSceneName);
        }
        else
        {
            // Резервний варіант, якщо HUD відсутній
            SceneManager.LoadScene(campSceneName);
        }
    }
}