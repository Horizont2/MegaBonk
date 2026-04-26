using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("UI Setup")]
    public GameObject missionUIPrefab;
    public Transform missionUIParent;  // Сюди будемо кидати плашки в HUD_Canvas

    public class ActiveMission
    {
        public MissionData data;
        public int currentProgress;
        public int targetAmount;
        public MissionUIElement uiElement;
        public bool isCompleted;
    }

    private List<ActiveMission> activeMissions = new List<ActiveMission>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadAcceptedMissions();
    }

    // Завантажуємо місії, які гравець взяв на дошці
    private void LoadAcceptedMissions()
    {
        if (PlayerPrefs.HasKey("ActiveMissionType"))
        {
            // Створюємо "фейковий" MissionData для збереження прогресу
            MissionData savedMission = ScriptableObject.CreateInstance<MissionData>();
            savedMission.missionName = PlayerPrefs.GetString("ActiveMissionName");
            savedMission.missionType = (MissionType)System.Enum.Parse(typeof(MissionType), PlayerPrefs.GetString("ActiveMissionType"));
            savedMission.woodReward = PlayerPrefs.GetInt("ActiveMissionRewardWood");
            savedMission.metalReward = PlayerPrefs.GetInt("ActiveMissionRewardMetal");
            savedMission.diamondReward = PlayerPrefs.GetInt("ActiveMissionRewardDiamond");

            int target = PlayerPrefs.GetInt("ActiveMissionTarget");

            AddActiveMissionUI(savedMission, target);
        }
    }

    // Додає UI плашку на екран (викликається з дошки або при старті)
    public void AddActiveMissionUI(MissionData data, int targetAmount)
    {
        GameObject uiObj = Instantiate(missionUIPrefab, missionUIParent);
        MissionUIElement uiElement = uiObj.GetComponent<MissionUIElement>();

        uiElement.Setup(data.missionName, 0, targetAmount);

        activeMissions.Add(new ActiveMission
        {
            data = data,
            currentProgress = 0,
            targetAmount = targetAmount,
            uiElement = uiElement,
            isCompleted = false
        });
    }

    public void AddProgress(MissionType type, int amount = 1)
    {
        foreach (ActiveMission mission in activeMissions)
        {
            if (!mission.isCompleted && mission.data.missionType == type)
            {
                mission.currentProgress += amount;

                if (mission.currentProgress > mission.targetAmount)
                    mission.currentProgress = mission.targetAmount;

                mission.uiElement.UpdateProgress(mission.currentProgress, mission.targetAmount);

                if (mission.currentProgress >= mission.targetAmount)
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

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(mission.data.woodReward, mission.data.metalReward, 0);
        }

        // Видаляємо з пам'яті, бо вона виконана
        PlayerPrefs.DeleteKey("ActiveMissionType");
        PlayerPrefs.Save();
    }
}