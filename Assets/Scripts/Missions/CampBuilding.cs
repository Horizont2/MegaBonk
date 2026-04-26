using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Спеціальний клас для збереження налаштувань кожного рівня
[System.Serializable]
public class BuildingLevel
{
    public int costWood;
    public int costStone;
    public int costFood;
    public int productionValue; // Наприклад: 10 дерева, або 500 ліміту складу
    public string productionDescription; // Наприклад: "+10 LOGS/MIN"
}

public class CampBuilding : MonoBehaviour
{
    [Header("Unique ID (ОБОВ'ЯЗКОВО ДЛЯ ЗБЕРЕЖЕННЯ)")]
    public string buildingID = "Building_01";

    [Header("Building Objects")]
    public GameObject ghostModel;
    public GameObject realModel;

    [Header("Building Info")]
    public string buildingName = "BUILDING";
    [TextArea] public string description = "Building description.";
    public bool isStorageVault = false; // Постав галочку ТІЛЬКИ для Складу!

    [Header("Levels & Upgrades")]
    public int currentLevel = 0; // 0 = Не побудовано
    public BuildingLevel[] levels; // Налаштуй рівні в Інспекторі!

    [Header("Hold To Build Mechanic")]
    public float holdTimeRequired = 1.5f;
    private float currentHoldTime = 0f;

    [Header("UI References")]
    public GameObject uiCanvas;
    public Image holdFillImage;
    public TextMeshProUGUI titleTMP;
    public TextMeshProUGUI descTMP;
    public TextMeshProUGUI prodTMP;
    public TextMeshProUGUI costWoodTMP;
    public TextMeshProUGUI costStoneTMP;
    public TextMeshProUGUI costFoodTMP;
    public TextMeshProUGUI buildHintTMP; // Напис "Press E to Build/Upgrade"

    [Header("Cinematic Effects")]
    public ParticleSystem buildDustVFX;
    public AudioSource buildAudio;
    public float buildDuration = 2.5f;

    private bool playerInRange = false;
    private bool isAnimating = false;

    private void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (holdFillImage != null) holdFillImage.fillAmount = 0f;

        // ЗАВАНТАЖЕННЯ ЗБЕРЕЖЕННЯ
        currentLevel = PlayerPrefs.GetInt("SaveBld_" + buildingID, 0);

        if (currentLevel == 0)
        {
            ghostModel.SetActive(true);
            realModel.SetActive(false);
        }
        else
        {
            ghostModel.SetActive(false);
            realModel.SetActive(true);
            ApplyBuildingEffects(); // Вмикаємо ліміти чи генерацію
        }
    }

    private void Update()
    {
        if (isAnimating || !playerInRange) return;

        // Якщо вже максимальний рівень - нічого не робимо
        if (currentLevel >= levels.Length) return;

        BuildingLevel nextLevelData = levels[currentLevel];

        if (Input.GetKey(KeyCode.E) && ResourceManager.Instance.CanAfford(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood))
        {
            currentHoldTime += Time.deltaTime;
            if (holdFillImage != null) holdFillImage.fillAmount = currentHoldTime / holdTimeRequired;

            if (currentHoldTime >= holdTimeRequired)
            {
                currentHoldTime = 0f;
                // Списуємо ресурси
                ResourceManager.Instance.SpendResources(nextLevelData.costWood, nextLevelData.costStone, nextLevelData.costFood);

                // ПІДВИЩУЄМО РІВЕНЬ І ЗБЕРІГАЄМО
                currentLevel++;
                PlayerPrefs.SetInt("SaveBld_" + buildingID, currentLevel);
                PlayerPrefs.Save();

                StartCoroutine(BuildSequence());
            }
        }
        else
        {
            if (currentHoldTime > 0)
            {
                currentHoldTime -= Time.deltaTime * 2f;
                currentHoldTime = Mathf.Max(0, currentHoldTime);
                if (holdFillImage != null) holdFillImage.fillAmount = currentHoldTime / holdTimeRequired;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentLevel < levels.Length)
        {
            playerInRange = true;
            UpdateUIData();
            if (uiCanvas != null) uiCanvas.SetActive(true);
            StartCoroutine(PopUpUI()); // Анімація появи UI
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            currentHoldTime = 0f;
            if (holdFillImage != null) holdFillImage.fillAmount = 0f;
            if (uiCanvas != null) uiCanvas.SetActive(false);
        }
    }

    private void UpdateUIData()
    {
        if (currentLevel >= levels.Length) return; // Макс. рівень

        BuildingLevel nextLevelData = levels[currentLevel];

        if (titleTMP) titleTMP.text = buildingName + $" (LVL {currentLevel + 1})";
        if (descTMP) descTMP.text = description;

        // КРАСИВА ЛОГІКА АПГРЕЙДІВ (Зелений текст)
        if (currentLevel == 0)
        {
            // Ще не збудовано
            if (prodTMP) prodTMP.text = nextLevelData.productionDescription;
            if (buildHintTMP) buildHintTMP.text = "Press E to Build";
        }
        else
        {
            // Апгрейд (показуємо старе -> нове зеленим)
            BuildingLevel currentData = levels[currentLevel - 1];
            if (prodTMP) prodTMP.text = $"{currentData.productionDescription}\n<color=#00FF00>➔ {nextLevelData.productionDescription}</color>";
            if (buildHintTMP) buildHintTMP.text = "Press E to Upgrade";
        }

        // Відображення цін (Можна додати перевірку і фарбувати в червоний, якщо не вистачає)
        string woodColor = ResourceManager.Instance.wood >= nextLevelData.costWood ? "#FFFFFF" : "#FF4444";
        string stoneColor = ResourceManager.Instance.stone >= nextLevelData.costStone ? "#FFFFFF" : "#FF4444";

        if (costWoodTMP) costWoodTMP.text = $"<color={woodColor}>Logs: {nextLevelData.costWood}</color>";
        if (costStoneTMP) costStoneTMP.text = $"<color={stoneColor}>Stones: {nextLevelData.costStone}</color>";
        if (costFoodTMP) costFoodTMP.text = $"Food: {nextLevelData.costFood}";
    }

    private IEnumerator PopUpUI()
    {
        uiCanvas.transform.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            uiCanvas.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(0.01f, 0.01f, 0.01f), Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }

    private IEnumerator BuildSequence()
    {
        isAnimating = true;

        if (uiCanvas != null) uiCanvas.SetActive(false);
        ghostModel.SetActive(false);

        realModel.SetActive(true);
        Vector3 finalPosition = realModel.transform.position;

        // Якщо це перший рівень - виповзає з-під землі. Якщо апгрейд - просто трясеться/димить на місці.
        if (currentLevel == 1) realModel.transform.position = finalPosition - new Vector3(0, 5f, 0);

        if (buildDustVFX != null) buildDustVFX.Play();
        if (buildAudio != null) buildAudio.Play();

        float timer = 0f;
        Vector3 startPosition = realModel.transform.position;

        while (timer < buildDuration)
        {
            timer += Time.deltaTime;
            if (currentLevel == 1)
            {
                float progress = timer / buildDuration;
                realModel.transform.position = Vector3.Lerp(startPosition, finalPosition, Mathf.SmoothStep(0f, 1f, progress));
            }
            yield return null;
        }

        realModel.transform.position = finalPosition;
        ApplyBuildingEffects();

        // Якщо гравець все ще поруч після будівництва - оновлюємо UI для наступного рівня
        if (playerInRange && currentLevel < levels.Length)
        {
            UpdateUIData();
            if (uiCanvas != null) uiCanvas.SetActive(true);
            StartCoroutine(PopUpUI());
        }

        isAnimating = false;
    }

    private void ApplyBuildingEffects()
    {
        BuildingLevel currentData = levels[currentLevel - 1];

        // Якщо це склад - повідомляємо ResourceManager збільшити ліміт!
        if (isStorageVault && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetExtraCapacity(currentData.productionValue);
        }
        else if (!isStorageVault)
        {
            // Якщо це шахта/лісоруб - оновлюємо генерацію
            StopAllCoroutines(); // Зупиняємо стару генерацію
            StartCoroutine(ProductionRoutine(currentData.productionValue));
        }
    }

    private IEnumerator ProductionRoutine(int amountPerMinute)
    {
        if (amountPerMinute <= 0) yield break; // Якщо це кузня (яка не генерує ресурси напряму)

        while (true)
        {
            yield return new WaitForSeconds(60f);

            if (buildingID.Contains("Lumberjack")) ResourceManager.Instance.AddResources(amountPerMinute, 0, 0);
            else if (buildingID.Contains("Stone")) ResourceManager.Instance.AddResources(0, amountPerMinute, 0);
            else if (buildingID.Contains("Hunter")) ResourceManager.Instance.AddResources(0, 0, amountPerMinute);

            Debug.Log($"{buildingName} Level {currentLevel} Згенерувала +{amountPerMinute} ресурсів!");
        }
    }
}