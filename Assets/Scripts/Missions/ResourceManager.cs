using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("STASH (Склад у Таборі)")]
    public int stashWood = 0;
    public int stashStone = 0;
    public int stashFood = 0;
    public int diamonds = 0;

    [Header("RUN INVENTORY (Зібране в Подорожі)")]
    public int runWood = 0;
    public int runStone = 0;
    public int runFood = 0;

    [Header("Base Capacities (Склад)")]
    public int baseMaxWood = 200;
    public int baseMaxStone = 100;
    public int baseMaxFood = 50;
    public int extraCapacity = 0;

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
        int maxW = isCamp ? GetMax("Wood") : 9999;
        int maxS = isCamp ? GetMax("Stone") : 9999;
        int maxF = isCamp ? GetMax("Food") : 9999;

        if (woodSlider != null) woodSlider.maxValue = maxW;
        if (stoneSlider != null) stoneSlider.maxValue = maxS;
        if (foodSlider != null) foodSlider.maxValue = maxF;
    }

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

    // --- ДОДАВАННЯ ПІД ЧАС ГРИ ---
    public void AddRunResources(int addWood, int addStone, int addFood)
    {
        if (isCamp) return;

        runWood += addWood;
        runStone += addStone;
        runFood += addFood;

        if (woodPopup != null && addWood > 0) woodPopup.ShowChange(addWood);
        if (stonePopup != null && addStone > 0) stonePopup.ShowChange(addStone);
        if (foodPopup != null && addFood > 0) foodPopup.ShowChange(addFood);

        UpdateUI();
    }

    // --- ДОДАВАННЯ ПРЯМО НА СКЛАД (Місії та Будівлі) ---
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

    // --- ВИТРАТИ В ТАБОРІ (для будівництва) ---
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

    // --- МАГІЯ ЕВАКУАЦІЇ ---
    public void EvacuateRunToStash()
    {
        int maxW = GetMax("Wood");
        int maxS = GetMax("Stone");
        int maxF = GetMax("Food");

        int woodToStore = Mathf.Min(maxW - stashWood, runWood);
        int stoneToStore = Mathf.Min(maxS - stashStone, runStone);
        int foodToStore = Mathf.Min(maxF - stashFood, runFood);

        stashWood += woodToStore;
        stashStone += stoneToStore;
        stashFood += foodToStore;

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
        // НОВЕ: Змінюємо заголовок і колір залежно від сцени
        if (inventoryTitleText != null)
        {
            inventoryTitleText.text = isCamp ? "CAMP STASH" : "BACKPACK";
            // Жовтуватий колір для табору, білий для рюкзака
            inventoryTitleText.color = isCamp ? new Color(1f, 0.8f, 0.2f) : Color.white;
        }

        if (woodText) woodText.text = isCamp ? $"{stashWood} / {GetMax("Wood")}" : runWood.ToString();
        if (stoneText) stoneText.text = isCamp ? $"{stashStone} / {GetMax("Stone")}" : runStone.ToString();
        if (foodText) foodText.text = isCamp ? $"{stashFood} / {GetMax("Food")}" : runFood.ToString();
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