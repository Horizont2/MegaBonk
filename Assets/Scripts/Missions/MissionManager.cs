using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance; // Щоб інші скрипти легко сюди зверталися

    [Header("Level Configuration")]
    public LevelData currentLevelData; // Сюди перетягуємо наш Level_1_Forest

    [Header("UI Setup")]
    public GameObject missionUIPrefab; // Префаб плашки місії
    public Transform missionUIParent;  // Об'єкт із Vertical Layout Group на Канвасі

    // Внутрішній клас для відстеження прогресу кожної місії
    private class ActiveMission
    {
        public MissionData data;
        public int currentProgress;
        public MissionUIElement uiElement;
        public bool isCompleted;
    }

    private List<ActiveMission> activeMissions = new List<ActiveMission>();

    private void Awake()
    {
        // Робимо Синглтон
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeMissions();
    }

    private void InitializeMissions()
    {
        if (currentLevelData == null || currentLevelData.levelMissions.Length == 0) return;

        foreach (MissionData mission in currentLevelData.levelMissions)
        {
            // Створюємо UI плашку
            GameObject uiObj = Instantiate(missionUIPrefab, missionUIParent);
            MissionUIElement uiElement = uiObj.GetComponent<MissionUIElement>();

            uiElement.Setup(mission.missionDescription, 0, mission.targetAmount);

            // Додаємо в список активних місій
            activeMissions.Add(new ActiveMission
            {
                data = mission,
                currentProgress = 0,
                uiElement = uiElement,
                isCompleted = false
            });
        }
    }

    // Цей метод будуть викликати вороги при смерті або гравець при зборі луту
    public void AddProgress(MissionType type, int amount = 1)
    {
        foreach (ActiveMission mission in activeMissions)
        {
            if (!mission.isCompleted && mission.data.missionType == type)
            {
                mission.currentProgress += amount;

                // Захист від переповнення (щоб не було 51/50)
                if (mission.currentProgress > mission.data.targetAmount)
                    mission.currentProgress = mission.data.targetAmount;

                mission.uiElement.UpdateProgress(mission.currentProgress, mission.data.targetAmount);

                if (mission.currentProgress >= mission.data.targetAmount)
                {
                    CompleteMission(mission);
                }
            }
        }
    }

    private void CompleteMission(ActiveMission mission)
    {
        mission.isCompleted = true;
        mission.uiElement.CompleteMission();

        // ВИДАЧА НАГОРОД (Зберігаємо в PlayerPrefs)
        int currentWood = PlayerPrefs.GetInt("HubWood", 0);
        int currentMetal = PlayerPrefs.GetInt("HubMetal", 0);
        int currentDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);

        PlayerPrefs.SetInt("HubWood", currentWood + mission.data.woodReward);
        PlayerPrefs.SetInt("HubMetal", currentMetal + mission.data.metalReward);
        PlayerPrefs.SetInt("PlayerDiamonds", currentDiamonds + mission.data.diamondReward);
        PlayerPrefs.Save();

        Debug.Log($"Місію '{mission.data.missionName}' виконано! Отримано {mission.data.woodReward} дерева та {mission.data.metalReward} металу.");
    }
}