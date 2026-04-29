using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("STASH (Ñêëàä ó Òàáîð³)")]
    public int stashWood = 0;
    public int stashStone = 0;
    public int stashFood = 0;
    public int diamonds = 0;

    [Header("Base Capacities (Ñêëàä)")]
    public int baseMaxWood = 200;
    public int baseMaxStone = 100;
    public int baseMaxFood = 50;
    public int extraCapacity = 0;

    [Header("RUN INVENTORY (Ç³áðàíå â Ïîäîðîæ³)")]
    public int runWood = 0;
    public int runStone = 0;
    public int runFood = 0;

    [Header("Backpack Capacities (Ðþêçàê)")]
    public int baseRunMaxWood = 100;
    public int baseRunMaxStone = 50;
    public int baseRunMaxFood = 30;
    public int extraRunCapacity = 0; // Äëÿ ìàéáóòíüî¿ ïðîêà÷êè ðþêçàêà â Ìàãàçèí³

    [Header("UI Texts")]
    public TextMeshProUGUI inventoryTitleText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI diamondsText;

    [Header("UI Sliders")]
    public Slider woodSlider;
    public Slider stoneSlider;
    public Slider foodSlider;
    public float sliderLerpSpeed = 5f;

    [Header("UI Popups")]
    public ResourcePopup woodPopup;
    public ResourcePopup stonePopup;
    public ResourcePopup foodPopup;

    private bool isCamp => SceneManager.GetActiveScene().name == "CampScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadStash();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isCamp) ClearRunInventory();

        SetupSlidersMax();
        UpdateUI();
    }

    private void Update()
    {
        int targetWood = isCamp ? stashWood : runWood;
        int targetStone = isCamp ? stashStone : runStone;
        int targetFood = isCamp ? stashFood : runFood;

        if (woodSlider != null) woodSlider.value = Mathf.Lerp(woodSlider.value, targetWood, Time.deltaTime * sliderLerpSpeed);
        if (stoneSlider != null) stoneSlider.value = Mathf.Lerp(stoneSlider.value, targetStone, Time.deltaTime * sliderLerpSpeed);
        if (foodSlider != null) foodSlider.value = Mathf.Lerp(foodSlider.value, targetFood, Time.deltaTime * sliderLerpSpeed);
    }

    private void SetupSlidersMax()
    {
        // Ñëàéäåðè ï³äëàøòîâóþòüñÿ ï³ä ïîòî÷íó ñöåíó (Ñêëàä àáî Ðþêçàê)
        int maxW = isCamp ? GetMax("Wood") : GetRunMax("Wood");
        int maxS = isCamp ? GetMax("Stone") : GetRunMax("Stone");
        int maxF = isCamp ? GetMax("Food") : GetRunMax("Food");

        if (woodSlider != null) woodSlider.maxValue = maxW;
        if (stoneSlider != null) stoneSlider.maxValue = maxS;
        if (foodSlider != null) foodSlider.maxValue = maxF;
    }

    // --- Ë²Ì²ÒÈ ÑÊËÀÄÓ ---
    public void SetExtraCapacity(int bonusAmount)
    {
        extraCapacity = bonusAmount;
        SetupSlidersMax();
        UpdateUI();
    }

    public int GetMax(string type)
    {
        if (type == "Wood") return baseMaxWood + extraCapacity;
        if (type == "Stone") return baseMaxStone + extraCapacity;
        if (type == "Food") return baseMaxFood + extraCapacity;
        return 999999;
    }

    // --- Ë²Ì²ÒÈ ÐÞÊÇÀÊÀ ---
    public void SetExtraRunCapacity(int bonusAmount)
    {
        extraRunCapacity = bonusAmount;
        SetupSlidersMax();
        UpdateUI();
    }

    public int GetRunMax(string type)
    {
        if (type == "Wood") return baseRunMaxWood + extraRunCapacity;
        if (type == "Stone") return baseRunMaxStone + extraRunCapacity;
        if (type == "Food") return baseRunMaxFood + extraRunCapacity;
        return 999;
    }

    // --- ÄÎÄÀÂÀÍÍß Ï²Ä ×ÀÑ ÃÐÈ (ÐÞÊÇÀÊ) ---
    public void AddRunResources(int addWood, int addStone, int addFood)
    {
        if (isCamp) return;

        int oldWood = runWood;
        int oldStone = runStone;
        int oldFood = runFood;

        // Æîðñòêî îáìåæóºìî ê³ëüê³ñòü ç³áðàíîãî ë³ì³òàìè ðþêçàêà
        runWood = Mathf.Min(runWood + addWood, GetRunMax("Wood"));
        runStone = Mathf.Min(runStone + addStone, GetRunMax("Stone"));
        runFood = Mathf.Min(runFood + addFood, GetRunMax("Food"));

        int actualAddedWood = runWood - oldWood;
        int actualAddedStone = runStone - oldStone;
        int actualAddedFood = runFood - oldFood;

        // Ïîêàçóºìî ïîïàïè ò³ëüêè ÿêùî ðåñóðñ ä³éñíî äîäàâñÿ (ÿêùî ðþêçàê ïîâíèé - ïîïàïó íå áóäå)
        if (woodPopup != null && actualAddedWood > 0) woodPopup.ShowChange(actualAddedWood);
        if (stonePopup != null && actualAddedStone > 0) stonePopup.ShowChange(actualAddedStone);
        if (foodPopup != null && actualAddedFood > 0) foodPopup.ShowChange(actualAddedFood);

        UpdateUI();
    }

    // --- ÄÎÄÀÂÀÍÍß ÏÐßÌÎ ÍÀ ÑÊËÀÄ (Ì³ñ³¿ òà Áóä³âë³) ---
    public void AddStashResources(int addWood, int addStone, int addFood)
    {
        int oldWood = stashWood; int oldStone = stashStone; int oldFood = stashFood;

        stashWood = Mathf.Min(stashWood + addWood, GetMax("Wood"));
        stashStone = Mathf.Min(stashStone + addStone, GetMax("Stone"));
        stashFood = Mathf.Min(stashFood + addFood, GetMax("Food"));

        int actualAddedWood = stashWood - oldWood;
        int actualAddedStone = stashStone - oldStone;
        int actualAddedFood = stashFood - oldFood;

        if (woodPopup != null && actualAddedWood > 0) woodPopup.ShowChange(actualAddedWood);
        if (stonePopup != null && actualAddedStone > 0) stonePopup.ShowChange(actualAddedStone);
        if (foodPopup != null && actualAddedFood > 0) foodPopup.ShowChange(actualAddedFood);

        SaveStash();
        UpdateUI();
    }

    // --- ÂÈÒÐÀÒÈ Â ÒÀÁÎÐ² (äëÿ áóä³âíèöòâà) ---
    public bool CanAffordStash(int costWood, int costStone, int costFood)
    {
        return (stashWood >= costWood && stashStone >= costStone && stashFood >= costFood);
    }

    public void SpendStashResources(int costWood, int costStone, int costFood)
    {
        stashWood -= costWood;
        stashStone -= costStone;
        stashFood -= costFood;

        if (woodPopup != null && costWood > 0) woodPopup.ShowChange(-costWood);
        if (stonePopup != null && costStone > 0) stonePopup.ShowChange(-costStone);
        if (foodPopup != null && costFood > 0) foodPopup.ShowChange(-costFood);

        SaveStash();
        UpdateUI();
    }

    // --- ÌÀÃ²ß ÅÂÀÊÓÀÖ²¯ ---
    public void EvacuateRunToStash()
    {
        int maxW = GetMax("Wood");
        int maxS = GetMax("Stone");
        int maxF = GetMax("Food");

        // Êëàäåìî â ñêëàä ñê³ëüêè âë³çå
        int woodToStore = Mathf.Min(maxW - stashWood, runWood);
        int stoneToStore = Mathf.Min(maxS - stashStone, runStone);
        int foodToStore = Mathf.Min(maxF - stashFood, runFood);

        stashWood += woodToStore;
        stashStone += stoneToStore;
        stashFood += foodToStore;

        // Òå, ùî íå âë³çëî â ñêëàä, êîíâåðòóºìî â ä³àìàíòè (õî÷à ç ë³ì³òîì ðþêçàêà öüîãî áóäå ìåíøå, àëå ëîã³êà íàä³éíà)
        int excessWood = runWood - woodToStore;
        int excessStone = runStone - stoneToStore;
        int excessFood = runFood - foodToStore;

        int bonusDiamonds = (excessWood / 5) + (excessStone / 3) + (excessFood / 2);
        diamonds += bonusDiamonds;

        ClearRunInventory();
        SaveStash();
    }

    public void ClearRunInventory()
    {
        runWood = 0; runStone = 0; runFood = 0;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (inventoryTitleText != null)
        {
            inventoryTitleText.text = isCamp ? "CAMP STASH" : "BACKPACK";
            inventoryTitleText.color = isCamp ? new Color(1f, 0.8f, 0.2f) : Color.white;
        }

        if (isCamp)
        {
            int maxStashWood = GetMax("Wood");
            int maxStashStone = GetMax("Stone");
            int maxStashFood = GetMax("Food");

            if (woodText) woodText.text = $"{stashWood} / {maxStashWood}";
            if (stoneText) stoneText.text = $"{stashStone} / {maxStashStone}";
            if (foodText) foodText.text = $"{stashFood} / {maxStashFood}";
        }
        else
        {
            int maxRunWood = GetRunMax("Wood");
            int maxRunStone = GetRunMax("Stone");
            int maxRunFood = GetRunMax("Food");

            // ßêùî ðþêçàê çàïîâíåíèé, òåêñò ñòàº ÷åðâîíèì
            string wColor = runWood >= maxRunWood ? "<color=#FF4444>" : "<color=#FFFFFF>";
            string sColor = runStone >= maxRunStone ? "<color=#FF4444>" : "<color=#FFFFFF>";
            string fColor = runFood >= maxRunFood ? "<color=#FF4444>" : "<color=#FFFFFF>";

            if (woodText) woodText.text = $"{wColor}{runWood}</color> / {maxRunWood}";
            if (stoneText) stoneText.text = $"{sColor}{runStone}</color> / {maxRunStone}";
            if (foodText) foodText.text = $"{fColor}{runFood}</color> / {maxRunFood}";
        }

        if (diamondsText) diamondsText.text = diamonds.ToString();
    }

    private void SaveStash()
    {
        PlayerPrefs.SetInt("Stash_Wood", stashWood);
        PlayerPrefs.SetInt("Stash_Stone", stashStone);
        PlayerPrefs.SetInt("Stash_Food", stashFood);
        PlayerPrefs.SetInt("PlayerDiamonds", diamonds);
        PlayerPrefs.Save();
    }

    private void LoadStash()
    {
        stashWood = PlayerPrefs.GetInt("Stash_Wood", 50);
        stashStone = PlayerPrefs.GetInt("Stash_Stone", 20);
        stashFood = PlayerPrefs.GetInt("Stash_Food", 10);
        diamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
    }
}