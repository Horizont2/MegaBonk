using UnityEngine;
using System.Collections;

public class Level1_QuestManager : MonoBehaviour
{
    public static Level1_QuestManager Instance;

    [Header("Quest Settings")]
    public int requiredWood = 15;

    [Tooltip("Пустий об'єкт, в якому лежать всі скелети для засідки")]
    public GameObject skeletonsGroup;
    public GameObject evacuationHorse;

    [Header("Ambush Settings")]
    public float spawnDistanceBehind = 8f;
    public float cameraTurnSpeed = 4f;

    private int currentQuestStep = 0;
    private int startingWood = 0;
    private bool isAmbushTriggered = false;
    private Transform playerTransform;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Ховаємо скелетів та коня на початку
        if (skeletonsGroup != null) skeletonsGroup.SetActive(false);
        if (evacuationHorse != null) evacuationHorse.SetActive(false);

        Invoke("FindPlayer", 0.5f); // Чекаємо півсекунди, щоб гравець точно з'явився на сцені
        UpdateObjectiveUI();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    public void AdvanceQuest()
    {
        currentQuestStep++;

        if (currentQuestStep == 1) // Перехід до збору дерева
        {
            if (ResourceManager.Instance != null)
                startingWood = ResourceManager.Instance.runWood;
        }
        else if (currentQuestStep == 2) // Засідка!
        {
            StartCoroutine(TriggerAmbushRoutine());
        }

        UpdateObjectiveUI();
    }

    private void Update()
    {
        // Динамічно оновлюємо текст збору дерева
        if (currentQuestStep == 1 && ResourceManager.Instance != null)
        {
            int gatheredWood = ResourceManager.Instance.runWood - startingWood;

            if (GlobalHUD.Instance != null)
                GlobalHUD.Instance.SetLevelObjective($"<color=#FFD700>Objective:</color>\nGather Wood ({gatheredWood}/{requiredWood})");

            if (gatheredWood >= requiredWood && !isAmbushTriggered)
            {
                AdvanceQuest();
            }
        }
    }

    private IEnumerator TriggerAmbushRoutine()
    {
        isAmbushTriggered = true;

        if (playerTransform != null && skeletonsGroup != null)
        {
            // 1. Обчислюємо позицію ЗА спиною гравця
            Vector3 behindPosition = playerTransform.position - (playerTransform.forward * spawnDistanceBehind);
            behindPosition.y = playerTransform.position.y; // Тримаємо їх на рівні землі

            // 2. Переміщаємо групу скелетів туди і розвертаємо їх обличчям до гравця
            skeletonsGroup.transform.position = behindPosition;
            skeletonsGroup.transform.LookAt(playerTransform);

            // 3. Активуємо ворогів
            skeletonsGroup.SetActive(true);

            // 4. Примусово розвертаємо гравця (і його камеру) до ворогів
            yield return StartCoroutine(ForcePlayerLookAt(behindPosition));
        }
    }

    private IEnumerator ForcePlayerLookAt(Vector3 targetPosition)
    {
        if (playerTransform == null) yield break;

        // Визначаємо напрямок, куди треба дивитися
        Vector3 direction = (targetPosition - playerTransform.position).normalized;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Плавний розворот
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * cameraTurnSpeed;
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, t);
            yield return null;
        }
    }

    // Цей метод треба викликати зі скрипта здоров'я скелетів (EnemyHealth), коли вони вмирають
    public void EnemyDefeated()
    {
        if (currentQuestStep == 2)
        {
            bool allDead = true;
            foreach (Transform child in skeletonsGroup.transform)
            {
                if (child.gameObject.activeSelf) allDead = false;
            }

            if (allDead)
            {
                AdvanceQuest(); // Перехід до евакуації
                if (evacuationHorse != null) evacuationHorse.SetActive(true);
            }
        }
    }

    private void UpdateObjectiveUI()
    {
        if (GlobalHUD.Instance == null) return;

        switch (currentQuestStep)
        {
            case 0:
                GlobalHUD.Instance.SetLevelObjective("<color=#FFD700>Objective:</color>\nInvestigate the Outpost");
                break;
            case 1:
                // Оновлюється в Update()
                break;
            case 2:
                GlobalHUD.Instance.SetLevelObjective("<color=#FF4444>Objective:</color>\nSurvive the Ambush!");
                break;
            case 3:
                GlobalHUD.Instance.SetLevelObjective("<color=#00FF00>Objective:</color>\nEscape! Reach the Horse!");
                break;
        }
    }
}