using UnityEngine;
using UnityEngine.SceneManagement; // Потрібно для зміни сцен

public class ScenePortal : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad = "ShopScene"; // Назва твоєї сцени з цвинтарем
    public KeyCode interactKey = KeyCode.E;   // Клавіша для входу

    private bool canTeleport = false;

    private void OnTriggerEnter(Collider medical)
    {
        if (medical.CompareTag("Player"))
        {
            canTeleport = true;
            // Тут можна вивести на екран напис: "Натисніть Е, щоб увійти"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canTeleport = false;
        }
    }

    private void Update()
    {
        if (canTeleport && Input.GetKeyDown(interactKey))
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        // Перед завантаженням ми маємо зберегти прогрес сезону
        // Наш SmartSeasonManager вже робить це в OnApplicationQuit, 
        // але краще викликати збереження примусово перед виходом.
        FindObjectOfType<SmartSeasonManager>()?.SendMessage("SaveProgress");

        SceneManager.LoadScene(sceneToLoad);
    }
}