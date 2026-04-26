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
    public int extraCapacity = 0; // Цей бонус дає Склад!

    [Header("UI Texts")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI diamondsText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        LoadResources();
    }

    private void Start()
    {
        UpdateUI();
    }

    // Встановлюємо додаткове місце від Складу
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
        SaveResources();
        UpdateUI();
    }

    public void AddResources(int addWood, int addStone, int addFood)
    {
        // Додаємо ресурси, але не дозволяємо перевищити ЛІМІТ!
        wood = Mathf.Min(wood + addWood, GetMax("Wood"));
        stone = Mathf.Min(stone + addStone, GetMax("Stone"));
        food = Mathf.Min(food + addFood, GetMax("Food"));
        SaveResources();
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Тепер пишемо "150 / 200"
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