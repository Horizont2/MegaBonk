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
    public string shopSceneName = "ShopScene";

    [Header("Hero Spawning (NEW)")]
    public GameObject[] heroPrefabs;   // Префаби героїв (перетягни сюди ті самі, що й у грі)
    public GameObject[] weaponPrefabs; // Префаби зброї
    public Transform heroSpawnPoint;   // Перетягни сюди створений пустий об'єкт з п'єдесталу

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        UpdateCrystalsUI();
        CheckContinueStatus();

        // Викликаємо спавн героя при старті меню
        SpawnSelectedHero();
    }

    private void SpawnSelectedHero()
    {
        int selectedHeroID = PlayerPrefs.GetInt("SelectedHeroID", 0);
        int selectedWeaponID = PlayerPrefs.GetInt("SelectedWeaponID", 0);

        if (heroPrefabs != null && selectedHeroID >= 0 && selectedHeroID < heroPrefabs.Length && heroPrefabs[selectedHeroID] != null)
        {
            // 1. Створюємо модельку героя
            GameObject currentVisual = Instantiate(heroPrefabs[selectedHeroID], heroSpawnPoint.position, heroSpawnPoint.rotation);

            // --- НОВИЙ РЯДОК: Зменшуємо героя на 30% ---
            currentVisual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            // -------------------------------------------

            // 2. Вмикаємо Idle анімацію
            Animator anim = currentVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsGrounded", true);
                anim.SetFloat("Speed", 0f);
            }

            // 3. Шукаємо кістку руки і даємо йому зброю
            Transform socket = FindDeepChild(currentVisual.transform, "handslot.r");
            if (socket != null && weaponPrefabs != null && selectedWeaponID >= 0 && selectedWeaponID < weaponPrefabs.Length && weaponPrefabs[selectedWeaponID] != null)
            {
                Instantiate(weaponPrefabs[selectedWeaponID], socket.position, socket.rotation, socket);
            }
        }
    }

    // Рекурсивний пошук кістки (скопійовано з PlayerController)
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
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