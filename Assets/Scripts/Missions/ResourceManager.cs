using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("Current Resources")]
    public int wood = 0;
    public int stone = 0;
    public int food = 0;
    public int diamonds = 0;

    [Header("Base Capacities (Limits)")]
    public int baseMaxWood = 200;
    public int baseMaxStone = 100;
    public int baseMaxFood = 50;
    public int extraCapacity = 0;

    [Header("UI Texts")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI diamondsText;

    [Header("UI Popups")]
    public ResourcePopup woodPopup;
    public ResourcePopup stonePopup;
    public ResourcePopup foodPopup;

    private void Awake()
    {
        // ÐÎÁÈÌÎ ÌÅÍÅÄÆÅÐ "Â²×ÍÈÌ"
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Òåïåð â³í íå çíèêíå!
        }
        else
        {
            // ßêùî ìè çàâàíòàæèëè íîâó ñöåíó ³ òàì º ñâ³é ResourceManager - âèäàëÿºìî éîãî
            Destroy(gameObject);
            return;
        }

        LoadResources();
    }

    private void Start()
    {
        UpdateUI();
    }

    public void SetExtraCapacity(int bonusAmount)
    {
        extraCapacity = bonusAmount;
        UpdateUI();
    }

    public int GetMax(string type)
    {
        if (type == "Wood") return baseMaxWood + extraCapacity;
        if (type == "Stone") return baseMaxStone + extraCapacity;
        if (type == "Food") return baseMaxFood + extraCapacity;
        return 999999;
    }

    public bool CanAfford(int costWood, int costStone, int costFood)
    {
        return (wood >= costWood && stone >= costStone && food >= costFood);
    }

    public void SpendResources(int costWood, int costStone, int costFood)
    {
        wood -= costWood;
        stone -= costStone;
        food -= costFood;

        if (woodPopup != null && costWood > 0) woodPopup.ShowChange(-costWood);
        if (stonePopup != null && costStone > 0) stonePopup.ShowChange(-costStone);
        // ÂÈÏÐÀÂËÅÍÎ ÒÓÒ: costFood çàì³ñòü foodCost
        if (foodPopup != null && costFood > 0) foodPopup.ShowChange(-costFood);

        SaveResources();
        UpdateUI();
    }

    public void AddResources(int addWood, int addStone, int addFood)
    {
        int oldWood = wood; int oldStone = stone; int oldFood = food;

        wood = Mathf.Min(wood + addWood, GetMax("Wood"));
        stone = Mathf.Min(stone + addStone, GetMax("Stone"));
        food = Mathf.Min(food + addFood, GetMax("Food"));

        int actualAddedWood = wood - oldWood;
        int actualAddedStone = stone - oldStone;
        int actualAddedFood = food - oldFood;

        if (woodPopup != null && actualAddedWood > 0) woodPopup.ShowChange(actualAddedWood);
        if (stonePopup != null && actualAddedStone > 0) stonePopup.ShowChange(actualAddedStone);
        if (foodPopup != null && actualAddedFood > 0) foodPopup.ShowChange(actualAddedFood);

        SaveResources();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (woodText) woodText.text = $"{wood} / {GetMax("Wood")}";
        if (stoneText) stoneText.text = $"{stone} / {GetMax("Stone")}";
        if (foodText) foodText.text = $"{food} / {GetMax("Food")}";
        if (diamondsText) diamondsText.text = diamonds.ToString();
    }

    private void SaveResources()
    {
        PlayerPrefs.SetInt("Res_Wood", wood);
        PlayerPrefs.SetInt("Res_Stone", stone);
        PlayerPrefs.SetInt("Res_Food", food);
        PlayerPrefs.SetInt("PlayerDiamonds", diamonds);
        PlayerPrefs.Save();
    }

    private void LoadResources()
    {
        wood = PlayerPrefs.GetInt("Res_Wood", 50);
        stone = PlayerPrefs.GetInt("Res_Stone", 20);
        food = PlayerPrefs.GetInt("Res_Food", 10);
        diamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
    }
}