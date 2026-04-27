using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("UI Setup")]
    public GameObject missionUIPrefab;
    public Transform missionUIParent;

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
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadMissions();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "CampScene")
        {
            ClearCompletedMissionsUI();
        }
    }

    private void ClearCompletedMissionsUI()
    {
        for (int i = activeMissions.Count - 1; i >= 0; i--)
        {
            if (activeMissions[i].isCompleted)
            {
                if (activeMissions[i].uiElement != null)
                {
                    Destroy(activeMissions[i].uiElement.gameObject);
                }
                activeMissions.RemoveAt(i);
            }
        }
        SaveMissions();
    }

    // --- НОВА СИСТЕМА ЗБЕРЕЖЕННЯ (ПІДТРИМУЄ БАГАТО МІСІЙ) ---
    public void SaveMissions()
    {
        PlayerPrefs.SetInt("ActiveMissionCount", activeMissions.Count);

        for (int i = 0; i < activeMissions.Count; i++)
        {
            var m = activeMissions[i];
            PlayerPrefs.SetString("MissionName_" + i, m.data.missionName);
            PlayerPrefs.SetString("MissionDesc_" + i, m.data.missionDescription);
            PlayerPrefs.SetString("MissionType_" + i, m.data.missionType.ToString());
            PlayerPrefs.SetInt("MissionWood_" + i, m.data.woodReward);
            PlayerPrefs.SetInt("MissionStone_" + i, m.data.stoneReward);
            PlayerPrefs.SetInt("MissionFood_" + i, m.data.foodReward);
            PlayerPrefs.SetInt("MissionDiamond_" + i, m.data.diamondReward);
            PlayerPrefs.SetInt("MissionTarget_" + i, m.targetAmount);
            PlayerPrefs.SetInt("MissionProgress_" + i, m.currentProgress);
        }
        PlayerPrefs.Save();
    }

    private void LoadMissions()
    {
        int count = PlayerPrefs.GetInt("ActiveMissionCount", 0);

        for (int i = 0; i < count; i++)
        {
            MissionData loadedData = ScriptableObject.CreateInstance<MissionData>();
            loadedData.missionName = PlayerPrefs.GetString("MissionName_" + i);
            loadedData.missionDescription = PlayerPrefs.GetString("MissionDesc_" + i);

            string typeStr = PlayerPrefs.GetString("MissionType_" + i);
            if (System.Enum.TryParse(typeStr, out MissionType parsedType))
                loadedData.missionType = parsedType;

            loadedData.woodReward = PlayerPrefs.GetInt("MissionWood_" + i);
            loadedData.stoneReward = PlayerPrefs.GetInt("MissionStone_" + i);
            loadedData.foodReward = PlayerPrefs.GetInt("MissionFood_" + i);
            loadedData.diamondReward = PlayerPrefs.GetInt("MissionDiamond_" + i);

            int target = PlayerPrefs.GetInt("MissionTarget_" + i);
            int progress = PlayerPrefs.GetInt("MissionProgress_" + i);

            ActiveMission newMission = new ActiveMission
            {
                data = loadedData,
                currentProgress = progress,
                targetAmount = target,
                isCompleted = false
            };

            activeMissions.Add(newMission);
            CreateUIForMission(newMission);
        }
    }

    // --- ДОДАВАННЯ НОВОЇ МІСІЇ (Викликати з Дошки Оголошень) ---
    public void AddNewMission(MissionData data, int targetAmount)
    {
        ActiveMission newMission = new ActiveMission
        {
            data = data,
            currentProgress = 0,
            targetAmount = targetAmount,
            isCompleted = false
        };

        activeMissions.Add(newMission);
        CreateUIForMission(newMission);
        SaveMissions();
    }

    private void CreateUIForMission(ActiveMission mission)
    {
        if (missionUIPrefab == null || missionUIParent == null) return;

        GameObject uiObj = Instantiate(missionUIPrefab, missionUIParent);
        mission.uiElement = uiObj.GetComponent<MissionUIElement>();

        // ТУТ ВИПРАВЛЕНО: Завжди береться опис, а не ім'я
        string desc = string.IsNullOrEmpty(mission.data.missionDescription) ? mission.data.missionName : mission.data.missionDescription;
        mission.uiElement.Setup(desc, mission.currentProgress, mission.targetAmount);
    }

    public void AddProgress(MissionType type, int amount = 1)
    {
        bool wasUpdated = false;
        foreach (ActiveMission mission in activeMissions)
        {
            if (!mission.isCompleted && mission.data.missionType == type)
            {
                mission.currentProgress += amount;

                if (mission.currentProgress > mission.targetAmount)
                    mission.currentProgress = mission.targetAmount;

                if (mission.uiElement != null)
                    mission.uiElement.UpdateProgress(mission.currentProgress, mission.targetAmount);

                if (mission.currentProgress >= mission.targetAmount)
                {
                    CompleteMission(mission);
                }
                wasUpdated = true;
            }
        }

        // Зберігаємо змінений прогрес (наприклад, скільки секунд вижили)
        if (wasUpdated) SaveMissions();
    }

    private void CompleteMission(ActiveMission mission)
    {
        mission.isCompleted = true;
        if (mission.uiElement != null) mission.uiElement.CompleteMission();

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddStashResources(mission.data.woodReward, mission.data.stoneReward, mission.data.foodReward);
            ResourceManager.Instance.diamonds += mission.data.diamondReward;
            ResourceManager.Instance.UpdateUI();
        }

        SaveMissions();
    }
}