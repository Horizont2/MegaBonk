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
    public string campSceneName = "CampScene";

    [Header("Hero Spawning")]
    public GameObject[] heroPrefabs;
    public GameObject[] weaponPrefabs;
    public Transform heroSpawnPoint;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(AnimateCrystals());
        CheckContinueStatus();
        SpawnSelectedHero();
    }

    private System.Collections.IEnumerator AnimateCrystals()
    {
        if (crystalsText == null) yield break;

        int targetCrystals = PlayerPrefs.GetInt("PlayerDiamonds", 0);
        int currentCount = 0;
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentCount = (int)Mathf.Lerp(0, targetCrystals, elapsed / duration);
            crystalsText.text = currentCount.ToString("N0");
            yield return null;
        }
        crystalsText.text = targetCrystals.ToString("N0");
    }

    private void SpawnSelectedHero()
    {
        int selectedHeroID = PlayerPrefs.GetInt("SelectedHeroID", 0);
        int selectedWeaponID = PlayerPrefs.GetInt("SelectedWeaponID", 0);

        if (heroPrefabs != null && selectedHeroID >= 0 && selectedHeroID < heroPrefabs.Length && heroPrefabs[selectedHeroID] != null)
        {
            GameObject currentVisual = Instantiate(heroPrefabs[selectedHeroID], heroSpawnPoint.position, heroSpawnPoint.rotation);
            currentVisual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Animator anim = currentVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsGrounded", true);
                anim.SetFloat("Speed", 0f);
            }

            Transform socket = FindDeepChild(currentVisual.transform, "handslot.r");
            if (socket != null && weaponPrefabs != null && selectedWeaponID >= 0 && selectedWeaponID < weaponPrefabs.Length && weaponPrefabs[selectedWeaponID] != null)
            {
                Instantiate(weaponPrefabs[selectedWeaponID], socket.position, socket.rotation, socket);
            }
        }
    }

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
            bool hasSave = PlayerPrefs.GetInt("HasCampSave", 0) == 1;
            continueButton.interactable = hasSave;

            CanvasGroup cg = continueButton.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = hasSave ? 1f : 0.5f;
        }
    }

    public void UpdateCrystalsUI()
    {
        int targetCrystals = SaveManager.GetTotalCrystals();
        StartCoroutine(AnimateCrystalCount(targetCrystals));
    }

    private System.Collections.IEnumerator AnimateCrystalCount(int targetCount)
    {
        int currentCount = 0;
        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentCount = (int)Mathf.Lerp(0, targetCount, elapsed / duration);
            if (crystalsText != null)
                crystalsText.text = currentCount.ToString("N0");

            yield return null;
        }

        if (crystalsText != null)
            crystalsText.text = targetCount.ToString("N0");
    }

    // НОВЕ: Метод для вимкнення Канвасу меню перед завантаженням
    private void HideMenuBeforeLoad()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas.enabled = false;
        else gameObject.SetActive(false);
    }

    // --- ЛОГІКА КНОПОК ---
    public void StartNewRun()
    {
        PlayerPrefs.DeleteKey("HasCampSave");
        PlayerPrefs.SetInt("IsRunActive", 0);
        PlayerPrefs.SetInt("IsContinuing", 0);
        PlayerPrefs.Save();

        if (ResourceManager.Instance != null) ResourceManager.Instance.ClearRunInventory();

        HideMenuBeforeLoad();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(campSceneName);
        else SceneManager.LoadScene(campSceneName);
    }

    public void ContinueGame()
    {
        PlayerPrefs.SetInt("IsContinuing", 1);
        PlayerPrefs.Save();

        HideMenuBeforeLoad();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(campSceneName);
        else SceneManager.LoadScene(campSceneName);
    }

    public void OpenShop()
    {
        HideMenuBeforeLoad();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(shopSceneName);
        else SceneManager.LoadScene(shopSceneName);
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