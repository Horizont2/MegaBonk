using UnityEngine;
using TMPro;
using System.Collections;

public class Level1_QuestManager : MonoBehaviour
{
    public static Level1_QuestManager Instance;

    [Header("UI Reference")]
    public MissionUIElement objectiveUI;
    public TextMeshProUGUI subtitleText;

    [Header("Quest Settings")]
    public int requiredWood = 15;

    [Header("Enemies (Wave 1 & 2)")]
    public GameObject skeletonsWave1;
    public GameObject skeletonsHordeWave2;
    public GameObject evacuationHorse;

    [Header("Cinematic Settings")]
    public float spawnDistanceBehind = 15f;
    public float typingSpeed = 0.04f;

    private int currentQuestStep = 0;
    private int startingWood = 0;
    private bool isAmbushTriggered = false;
    private Transform playerTransform;

    private int totalSkeletonsW1 = 0;
    private int defeatedSkeletonsW1 = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (subtitleText != null) subtitleText.text = "";

        if (skeletonsWave1 != null)
        {
            totalSkeletonsW1 = skeletonsWave1.transform.childCount;
            skeletonsWave1.SetActive(false);
        }

        if (skeletonsHordeWave2 != null) skeletonsHordeWave2.SetActive(false);
        if (evacuationHorse != null) evacuationHorse.SetActive(false);

        Invoke("FindPlayer", 0.5f);
        UpdateObjectiveUI();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    public void StartIntroDialogue()
    {
        StartCoroutine(IntroDialogueRoutine());
    }

    private IEnumerator IntroDialogueRoutine()
    {
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Thank the heavens you're here! My cart is busted and this forest is cursed.", 2.5f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: I need wood to fix the wheels. Gather 15 pieces, or we're not getting out of here alive!", 3f));

        AdvanceQuest();

        // Підказка для збору дерева
        StartCoroutine(ShowTutorialHint("[TIP] Walk up to a tree and press Left Mouse Button to attack and gather wood.", 5f));
    }

    public void AdvanceQuest()
    {
        if (currentQuestStep > 0 && objectiveUI != null) objectiveUI.CompleteMission();

        currentQuestStep++;

        if (currentQuestStep == 1)
        {
            if (ResourceManager.Instance != null) startingWood = ResourceManager.Instance.runWood;
        }
        else if (currentQuestStep == 2)
        {
            StartCoroutine(TriggerAmbushWave1Routine());
        }
        else if (currentQuestStep == 3)
        {
            StartCoroutine(TriggerHordeAndFleeRoutine());
        }

        UpdateObjectiveUI();
    }

    private void Update()
    {
        if (currentQuestStep == 1 && ResourceManager.Instance != null && objectiveUI != null)
        {
            int gatheredWood = ResourceManager.Instance.runWood - startingWood;
            objectiveUI.UpdateProgress(gatheredWood, requiredWood);

            if (gatheredWood >= requiredWood && !isAmbushTriggered)
            {
                AdvanceQuest();
            }
        }
    }

    private IEnumerator TriggerAmbushWave1Routine()
    {
        isAmbushTriggered = true;

        Vector3 spawnPos = playerTransform.position - (playerTransform.forward * spawnDistanceBehind);
        spawnPos.y = playerTransform.position.y;

        skeletonsWave1.transform.position = spawnPos;
        skeletonsWave1.transform.LookAt(playerTransform);
        skeletonsWave1.SetActive(true);

        Coroutine cameraTurn = StartCoroutine(CinematicCameraLook(spawnPos, 3f));
        Coroutine riseAnim = StartCoroutine(RiseFromGroundAnim(skeletonsWave1.transform, 1.5f));

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Watch out! They're crawling from the dirt!", 2f));

        // Підказка для бою
        StartCoroutine(ShowTutorialHint("[TIP] Enemies are attacking! Use Left Mouse Button to fight back and watch your health.", 5f));
    }

    public void EnemyDefeated()
    {
        if (currentQuestStep == 2)
        {
            defeatedSkeletonsW1++;
            if (objectiveUI != null) objectiveUI.UpdateProgress(defeatedSkeletonsW1, totalSkeletonsW1);

            if (defeatedSkeletonsW1 >= totalSkeletonsW1)
            {
                AdvanceQuest();
            }
        }
    }

    private IEnumerator TriggerHordeAndFleeRoutine()
    {
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Good job! Wait... do you hear that?", 1.5f));

        Vector3 hordePos = playerTransform.position + (playerTransform.right * spawnDistanceBehind);
        hordePos.y = playerTransform.position.y;

        skeletonsHordeWave2.transform.position = hordePos;
        skeletonsHordeWave2.transform.LookAt(playerTransform);
        skeletonsHordeWave2.SetActive(true);

        StartCoroutine(CinematicCameraLook(hordePos, 3));
        StartCoroutine(RiseFromGroundAnim(skeletonsHordeWave2.transform, 1.5f));

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: IT'S A WHOLE ARMY! THERE'S TOO MANY! RUN TO THE HORSE, NOW!!", 3f));

        if (evacuationHorse != null) evacuationHorse.SetActive(true);

        // Підказка для втечі
        StartCoroutine(ShowTutorialHint("[TIP] You can't defeat them all! Hold SHIFT to sprint and reach the Extraction Point!", 6f));
    }

    // --- ЕФЕКТИ ТА АНІМАЦІЇ ---

    private IEnumerator ShowTutorialHint(string text, float duration)
    {
        if (subtitleText == null) yield break;
        // Робимо текст світло-блакитним, щоб відрізнити від діалогів
        subtitleText.text = $"<color=#88CCFF>{text}</color>";
        yield return new WaitForSeconds(duration);
        subtitleText.text = "";
    }

    private IEnumerator ShowSubtitleTypewriter(string text, float stayDuration)
    {
        if (subtitleText == null) yield break;

        subtitleText.text = "";
        foreach (char c in text)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(stayDuration);
        subtitleText.text = "";
    }

    private IEnumerator RiseFromGroundAnim(Transform group, float duration)
    {
        Vector3 finalPos = group.position;
        group.position = finalPos - new Vector3(0, 3f, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.position = Vector3.Lerp(group.position, finalPos, elapsed / duration);
            yield return null;
        }
        group.position = finalPos;
    }

    private IEnumerator CinematicCameraLook(Vector3 targetPosition, float duration)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        // Зберігаємо всі активні скрипти на камері і вимикаємо їх (щоб твій скрипт камери не заважав розвороту)
        MonoBehaviour[] camScripts = mainCam.GetComponents<MonoBehaviour>();
        foreach (var script in camScripts)
        {
            if (script != this) script.enabled = false;
        }

        Quaternion startRot = mainCam.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(targetPosition - mainCam.transform.position);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // Вмикаємо всі скрипти камери назад
        foreach (var script in camScripts)
        {
            if (script != null) script.enabled = true;
        }
    }

    private void UpdateObjectiveUI()
    {
        if (objectiveUI == null) return;
        switch (currentQuestStep)
        {
            case 0: objectiveUI.Setup("Main Quest", "Investigate the Outpost", 0, 1); break;
            case 1: objectiveUI.Setup("Stranger's Request", "Gather Wood", 0, requiredWood); break;
            case 2: objectiveUI.Setup("Ambush!", "Survive the Skeletons", 0, totalSkeletonsW1); break;
            case 3: objectiveUI.Setup("Escape!", "REACH THE HORSE BEFORE THEY KILL YOU!", 0, 1); break;
        }
    }
}