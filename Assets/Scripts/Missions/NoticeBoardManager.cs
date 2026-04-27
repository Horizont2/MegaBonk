using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NoticeBoardManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject boardCanvas;
    public Transform paperLayoutGroup;
    public GameObject missionPaperPrefab;
    public TextMeshProUGUI emptyBoardMessage;
    public Button embarkButton;

    [Header("Scene Transition")]
    public string worldSceneName = "WorldScene";

    [Header("Mission Database")]
    public MissionData[] baseMissions;

    [Header("Restock System")]
    public int maxMissionsOnBoard = 3;
    public float restockTimeMinutes = 5f;

    private List<GameObject> activePapers = new List<GameObject>();
    private bool isPlayerNear = false;
    private bool isBoardOpen = false;

    private void Start()
    {
        if (embarkButton != null) embarkButton.onClick.AddListener(EmbarkOnJourney);
        boardCanvas.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (!isBoardOpen) OpenBoard();
            else CloseBoard();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowPrompt("Press E to Open Board");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (isBoardOpen) CloseBoard();
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
        }
    }

    public void OpenBoard()
    {
        isBoardOpen = true;
        boardCanvas.SetActive(true);
        CheckAndGenerateMissions();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseBoard()
    {
        isBoardOpen = false;
        boardCanvas.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CheckAndGenerateMissions()
    {
        string lastRestockStr = PlayerPrefs.GetString("LastMissionRestockTime", "");
        bool needsRestock = string.IsNullOrEmpty(lastRestockStr);

        if (!needsRestock)
        {
            DateTime lastRestock = DateTime.Parse(lastRestockStr);
            if ((DateTime.Now - lastRestock).TotalMinutes >= restockTimeMinutes)
                needsRestock = true;
        }

        if (needsRestock)
        {
            GenerateNewMissions();

            PlayerPrefs.SetString("LastMissionRestockTime", DateTime.Now.ToString());
            PlayerPrefs.Save();
        }

        UpdateEmptyMessage();
    }

    private void GenerateNewMissions()
    {
        foreach (Transform child in paperLayoutGroup) Destroy(child.gameObject);
        activePapers.Clear();

        int missionsToSpawn = UnityEngine.Random.Range(1, maxMissionsOnBoard + 1);

        int powerLevel = 0;
        powerLevel += PlayerPrefs.GetInt("UpgradeLevel_MetaHealth", 0);
        powerLevel += PlayerPrefs.GetInt("UpgradeLevel_MetaDamage", 0);
        powerLevel += PlayerPrefs.GetInt("UpgradeLevel_MetaArmor", 0);
        powerLevel = Mathf.Min(powerLevel, 30);

        float rewardMultiplier = 1f + (powerLevel * 0.15f);
        float goalMultiplier = 1f + (powerLevel * 0.03f);

        List<MissionData> availableMissions = new List<MissionData>(baseMissions);

        for (int i = 0; i < missionsToSpawn; i++)
        {
            if (availableMissions.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableMissions.Count);
            MissionData baseMission = availableMissions[randomIndex];
            availableMissions.RemoveAt(randomIndex);

            MissionData scaledMission = ScriptableObject.Instantiate(baseMission);

            int rawTarget = Mathf.RoundToInt(scaledMission.targetAmount * goalMultiplier);
            scaledMission.targetAmount = Mathf.Clamp(RoundToNearestFive(rawTarget), 5, 400);

            scaledMission.woodReward = RoundToNearestFive(scaledMission.woodReward * rewardMultiplier);
            scaledMission.stoneReward = RoundToNearestFive(scaledMission.stoneReward * rewardMultiplier);
            scaledMission.foodReward = RoundToNearestFive(scaledMission.foodReward * rewardMultiplier);
            scaledMission.diamondReward = RoundToNearestFive(scaledMission.diamondReward * rewardMultiplier);

            GameObject paperObj = Instantiate(missionPaperPrefab, paperLayoutGroup);
            MissionPaperUI paperUI = paperObj.GetComponent<MissionPaperUI>();

            paperUI.SetupPaper(scaledMission, 1f);

            // “”“ ¬»ѕ–ј¬Ћ≈Ќќ: ѕравильно додаЇмо м≥с≥ю при кл≥ку на "Accept"
            paperUI.acceptButton.onClick.AddListener(UpdateEmptyMessage);

            activePapers.Add(paperObj);
        }
    }

    private int RoundToNearestFive(int value)
    {
        if (value <= 0) return 0;
        return Mathf.RoundToInt(value / 5f) * 5;
    }

    private int RoundToNearestFive(float value)
    {
        if (value <= 0) return 0;
        return Mathf.RoundToInt(value / 5f) * 5;
    }

    public void UpdateEmptyMessage()
    {
        StartCoroutine(CheckEmptyRoutine());
    }

    private System.Collections.IEnumerator CheckEmptyRoutine()
    {
        yield return new WaitForEndOfFrame();
        int paperCount = paperLayoutGroup.childCount;
        if (emptyBoardMessage != null) emptyBoardMessage.gameObject.SetActive(paperCount == 0);
    }

    private void EmbarkOnJourney()
    {
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.FadeAndLoadScene(worldSceneName);
        else SceneManager.LoadScene(worldSceneName);
    }
}