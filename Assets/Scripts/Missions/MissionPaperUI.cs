using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class MissionPaperUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI rewardText;
    public Button acceptButton;

    private MissionData myMissionData;
    private int scaledTargetAmount;
    private int scaledWoodReward;
    private int scaledMetalReward;
    private int scaledDiamondReward;

    [Header("Animation Settings")]
    public float flyDuration = 0.8f;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        acceptButton.onClick.AddListener(AcceptMission);
    }

    public void SetupPaper(MissionData baseData, float multiplier)
    {
        myMissionData = baseData;

        scaledTargetAmount = Mathf.RoundToInt(baseData.targetAmount * multiplier);
        scaledWoodReward = Mathf.RoundToInt(baseData.woodReward * multiplier);
        scaledMetalReward = Mathf.RoundToInt(baseData.metalReward * multiplier);
        scaledDiamondReward = Mathf.RoundToInt(baseData.diamondReward * multiplier);

        titleText.text = baseData.missionName;
        descText.text = $"{baseData.missionDescription}\n\n<color=#FF5555>Target: {scaledTargetAmount}</color>";
        rewardText.text = $"Rewards:\n<color=#00FF00>+{scaledWoodReward} Wood</color>\n<color=#AAAAAA>+{scaledMetalReward} Stone</color>";
    }

    private void AcceptMission()
    {
        acceptButton.interactable = false;

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        SaveMissionForWorld();
        StartCoroutine(FlyToCornerRoutine());
    }

    private void SaveMissionForWorld()
    {
        PlayerPrefs.SetString("ActiveMissionType", myMissionData.missionType.ToString());
        PlayerPrefs.SetString("ActiveMissionName", myMissionData.missionName);
        PlayerPrefs.SetInt("ActiveMissionTarget", scaledTargetAmount);
        PlayerPrefs.SetInt("ActiveMissionRewardWood", scaledWoodReward);
        PlayerPrefs.SetInt("ActiveMissionRewardMetal", scaledMetalReward);
        PlayerPrefs.SetInt("ActiveMissionRewardDiamond", scaledDiamondReward);
        PlayerPrefs.Save();

        // Кажемо MissionManager додати це в UI миттєво
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.AddActiveMissionUI(myMissionData, scaledTargetAmount);
        }
    }

    private IEnumerator FlyToCornerRoutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        // Правий верхній кут
        Vector2 targetPos = new Vector2(Screen.width / 2f - 50f, Screen.height / 2f - 50f);

        float timer = 0f;
        while (timer < flyDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / flyDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothProgress);
            rectTransform.localScale = Vector3.Lerp(startScale, new Vector3(0.1f, 0.1f, 0.1f), smoothProgress);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(progress, 3));

            yield return null;
        }
        Destroy(gameObject);
    }
}