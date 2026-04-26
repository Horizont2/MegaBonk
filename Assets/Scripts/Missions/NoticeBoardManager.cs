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

    // Çì³íí³ äëÿ òð³ãåðà
    private bool isPlayerNear = false;
    private bool isBoardOpen = false;

    private void Start()
    {
        if (embarkButton != null)
            embarkButton.onClick.AddListener(EmbarkOnJourney);

        boardCanvas.SetActive(false); // Õîâàºìî äîøêó íà ñòàðò³
    }

    private void Update()
    {
        // ßêùî ãðàâåöü ïîðó÷ ³ òèñíå E
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (!isBoardOpen) OpenBoard();
            else CloseBoard();
        }
    }

    // Òð³ãåðí³ çîíè
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            // ÄÎÄÀÉ ÖÅÉ ÐßÄÎÊ:
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowPrompt("Press E to Open Board");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (isBoardOpen) CloseBoard();

            // ÄÎÄÀÉ ÖÅÉ ÐßÄÎÊ:
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
        }
    }

    public void OpenBoard()
    {
        isBoardOpen = true;
        boardCanvas.SetActive(true);
        CheckAndGenerateMissions();

        if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();

        // ÂÌÈÊÀªÌÎ ÌÈØÊÓ
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseBoard()
    {
        isBoardOpen = false;
        boardCanvas.SetActive(false);

        // ÂÈÌÈÊÀªÌÎ ÌÈØÊÓ (Õîâàºìî)
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

        int missionsToSpawn = UnityEngine.Random.Range(0, maxMissionsOnBoard + 1);
        if (missionsToSpawn == 0) return;

        int forgeLevel = PlayerPrefs.GetInt("SaveBld_Forge_01", 1);
        int storageLevel = PlayerPrefs.GetInt("SaveBld_Storage_01", 1);
        float playerPowerMultiplier = 1f + ((forgeLevel + storageLevel) * 0.2f);

        List<MissionData> availableMissions = new List<MissionData>(baseMissions);

        for (int i = 0; i < missionsToSpawn; i++)
        {
            if (availableMissions.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableMissions.Count);
            MissionData selectedMission = availableMissions[randomIndex];
            availableMissions.RemoveAt(randomIndex);

            GameObject paperObj = Instantiate(missionPaperPrefab, paperLayoutGroup);
            MissionPaperUI paperUI = paperObj.GetComponent<MissionPaperUI>();
            paperUI.SetupPaper(selectedMission, playerPowerMultiplier);

            paperUI.acceptButton.onClick.AddListener(UpdateEmptyMessage);
            activePapers.Add(paperObj);
        }
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
        SceneManager.LoadScene(worldSceneName);
    }
}