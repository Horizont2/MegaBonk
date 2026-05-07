using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RegionResourceSpawner : MonoBehaviour
{
    [Header("References")]
    public RegionData myRegionData;
    public RectTransform spawnArea;

    [Header("Resource Prefabs")]
    public GameObject woodPrefab;
    public GameObject stonePrefab;
    public GameObject foodPrefab;
    public GameObject diamondPrefab;

    [Header("Spawning Settings")]
    public float spawnIntervalMin = 20f;
    public float spawnIntervalMax = 45f;
    [Tooltip("Радіус розбросу іконок, щоб вони не злипалися і не вилазили за регіон")]
    public float scatterRadius = 40f;

    private List<GameObject> activeNodes = new List<GameObject>();

    void Start()
    {
        InitializeTimersIfConquered();
        StartCoroutine(SpawnRoutine());
    }

    private void InitializeTimersIfConquered()
    {
        if (myRegionData == null || myRegionData.currentState != RegionState.Conquered) return;

        InitializeTimerKey(ResourceRewardType.Wood);
        InitializeTimerKey(ResourceRewardType.Stone);
        InitializeTimerKey(ResourceRewardType.Food);
        InitializeTimerKey(ResourceRewardType.Diamond);
    }

    private void InitializeTimerKey(ResourceRewardType type)
    {
        string prefsKey = $"LastCollect_{myRegionData.regionID}_{type}";
        if (!PlayerPrefs.HasKey(prefsKey))
        {
            PlayerPrefs.SetString(prefsKey, System.DateTime.Now.ToBinary().ToString());
            PlayerPrefs.Save();
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));

            // Очищаємо список від зібраних (знищених) іконок
            activeNodes.RemoveAll(item => item == null);

            if (myRegionData != null && myRegionData.currentState == RegionState.Conquered)
            {
                CheckAndSpawnResources();
            }
        }
    }

    private void CheckAndSpawnResources()
    {
        int currentLevel = PlayerPrefs.GetInt("RegionLevel_" + myRegionData.regionID, 1);
        if (myRegionData.upgradeLevels == null || myRegionData.upgradeLevels.Length < currentLevel) return;

        RegionLevelData levelData = myRegionData.upgradeLevels[currentLevel - 1];

        // Пробуємо заспавнити кожен тип ресурсу окремо
        TrySpawnNode(ResourceRewardType.Wood, levelData.passiveWood, woodPrefab);
        TrySpawnNode(ResourceRewardType.Stone, levelData.passiveStone, stonePrefab);
        TrySpawnNode(ResourceRewardType.Food, levelData.passiveFood, foodPrefab);
        TrySpawnNode(ResourceRewardType.Diamond, levelData.passiveDiamonds, diamondPrefab);
    }

    private void TrySpawnNode(ResourceRewardType type, int passiveIncome, GameObject prefab)
    {
        // Якщо регіон не приносить цей ресурс, або префаб не призначено - ігноруємо
        if (passiveIncome <= 0 || prefab == null) return;

        // ПЕРЕВІРКА НА СТАКИ: Чи іконка ЦЬОГО ресурсу вже висить над регіоном?
        foreach (GameObject node in activeNodes)
        {
            if (node != null)
            {
                MapResourceNode script = node.GetComponent<MapResourceNode>();
                if (script != null && script.resourceType == type)
                {
                    return; // Іконка вже є! Ресурси стакаються всередині неї. Скасовуємо спавн.
                }
            }
        }

        // Якщо іконки ще немає, створюємо її
        GameObject newNode = Instantiate(prefab, spawnArea.parent);
        MapResourceNode nodeScript = newNode.GetComponent<MapResourceNode>();

        if (nodeScript != null)
        {
            nodeScript.Setup(myRegionData);

            // Розумна перевірка: чи накопичився хоча б 1 ресурс?
            if (nodeScript.GetCurrentAccumulated() <= 0)
            {
                Destroy(newNode);
                return;
            }
        }

        activeNodes.Add(newNode);

        // Розкидаємо навколо центру, щоб іконки різних ресурсів не накладалися
        RectTransform nodeRect = newNode.GetComponent<RectTransform>();
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        nodeRect.position = spawnArea.position;
        nodeRect.anchoredPosition += randomOffset;

        StartCoroutine(PopUpAnimation(nodeRect));
    }

    private IEnumerator PopUpAnimation(RectTransform rect)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        rect.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float popT = Mathf.Sin(t * Mathf.PI * 0.5f) * (1f + Mathf.Sin(t * Mathf.PI) * 0.2f);

            if (rect != null) rect.localScale = Vector3.one * popT;
            yield return null;
        }

        if (rect != null) rect.localScale = Vector3.one;
    }
}