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
        // Оскільки NoticeBoardManager вже передає нам масштабовані значення, 
        // multiplier тут зазвичай дорівнює 1f, тому ми беремо дані напряму.
        myMissionData = baseData;

        int finalTarget = Mathf.RoundToInt(baseData.targetAmount * multiplier);
        int finalWood = Mathf.RoundToInt(baseData.woodReward * multiplier);
        int finalStone = Mathf.RoundToInt(baseData.stoneReward * multiplier);
        int finalFood = Mathf.RoundToInt(baseData.foodReward * multiplier);
        int finalDiamond = Mathf.RoundToInt(baseData.diamondReward * multiplier);

        titleText.text = baseData.missionName;

        // Темно-бордовий (#8B0000) для цілі, щоб контрастувало з пергаментом
        descText.text = $"{baseData.missionDescription}\n\n<color=#8B0000><b>Target: {finalTarget}</b></color>";

        // Будуємо нагороди ГОРИЗОНТАЛЬНО в один рядок з темними кольорами
        string rewText = "<b>Rewards:</b> ";
        if (finalWood > 0) rewText += $"<color=#5C4033>{finalWood} Wood</color>  ";    // Темно-коричневий
        if (finalStone > 0) rewText += $"<color=#4A4A4A>{finalStone} Stone</color>  ";   // Темно-сірий
        if (finalFood > 0) rewText += $"<color=#B85E00>{finalFood} Food</color>  ";      // Темно-помаранчевий
        if (finalDiamond > 0) rewText += $"<color=#005500>{finalDiamond} Gems</color>";  // Темно-зелений

        rewardText.text = rewText;
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
        // Викликаємо нову функцію (яка виправляє помилку)
        // Вона автоматично збереже місію у масив
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.AddNewMission(myMissionData, myMissionData.targetAmount);
        }
    }

    private IEnumerator FlyToCornerRoutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

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