using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class Level1_QuestManager : MonoBehaviour
{
    public static Level1_QuestManager Instance;

    [Header("Cinematic & UI")]
    public PlayableDirector introDirector;
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

    // Захист від подвійного запуску діалогу
    private bool isDialogueStarted = false;

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

        Invoke("FindPlayer", 0.1f);

        // ФІКС 1: Одразу в першому кадрі вмикаємо режим кіно, щоб камера не стрибала
        if (introDirector != null)
        {
            SetCinematicMode(true);
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = true;

            introDirector.Play();
        }

        StartCoroutine(LevelStartRoutine());
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private IEnumerator LevelStartRoutine()
    {
        if (introDirector != null)
        {
            // Блокуємо гравця та UI
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) pObj.GetComponent<PlayerController>().enabled = false;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

            yield return null; // Чекаємо один кадр, щоб таймлайн точно ініціалізувався

            while (introDirector.state == PlayState.Playing)
            {
                yield return null;
            }

            // Повертаємо контроль після таймлайну
            if (pObj != null) pObj.GetComponent<PlayerController>().enabled = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

            CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
            if (cf != null)
            {
                Vector3 currentRot = Camera.main.transform.eulerAngles;
                cf.SyncRotation(currentRot.y, currentRot.x);
            }
            SetCinematicMode(false);

            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
        }

        // ФІКС 2: Більше ніякого авто-запуску діалогу! Просто показуємо ціль
        UpdateObjectiveUI();
    }

    // ФІКС 3: Цей метод тепер захищений від повторних натискань
    public void StartIntroDialogue()
    {
        if (isDialogueStarted) return;
        isDialogueStarted = true;
        StartCoroutine(IntroDialogueRoutine());
    }

    private void SetCinematicMode(bool isCinematic)
    {
        CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
        if (cf != null) cf.isCinematicMode = isCinematic;
        CameraCollision cc = Camera.main.GetComponent<CameraCollision>();
        if (cc != null) cc.isCinematicMode = isCinematic;
    }

    private IEnumerator IntroDialogueRoutine()
    {
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Thank the heavens you're here! My cart is busted and this forest is cursed.", 2.5f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: I need wood to fix the wheels. Gather 15 pieces, or we're not getting out of here alive!", 3f));

        AdvanceQuest();
        StartCoroutine(ShowTutorialHint("[TIP] Walk up to a tree and press Left Mouse Button to attack and gather wood.", 5f));
    }

    public void AdvanceQuest()
    {
        if (currentQuestStep > 0 && objectiveUI != null) objectiveUI.CompleteMission();
        currentQuestStep++;

        if (currentQuestStep == 1 && ResourceManager.Instance != null) startingWood = ResourceManager.Instance.runWood;
        else if (currentQuestStep == 2) StartCoroutine(TriggerAmbushWave1Routine());
        else if (currentQuestStep == 3) StartCoroutine(TriggerHordeAndFleeRoutine());

        UpdateObjectiveUI();
    }

    private void Update()
    {
        if (currentQuestStep == 1 && ResourceManager.Instance != null && objectiveUI != null)
        {
            int gatheredWood = ResourceManager.Instance.runWood - startingWood;
            objectiveUI.UpdateProgress(gatheredWood, requiredWood);
            if (gatheredWood >= requiredWood && !isAmbushTriggered) AdvanceQuest();
        }
    }

    private Vector3 GetTerrainPos(Vector3 pos)
    {
        if (Terrain.activeTerrain != null)
        {
            float terrainHeight = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
            return new Vector3(pos.x, terrainHeight, pos.z);
        }
        return pos;
    }

    // --- ЗАСІДКА (ДИНАМІЧНА КАМЕРА З ДРОНА) ---
    private IEnumerator TriggerAmbushWave1Routine()
    {
        isAmbushTriggered = true;

        PlayerController pController = playerTransform.GetComponent<PlayerController>();
        if (pController != null) pController.enabled = false;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

        Vector3 spawnPos = playerTransform.position - (playerTransform.forward * spawnDistanceBehind);
        spawnPos = GetTerrainPos(spawnPos); // ФІКС: Ставимо чітко на землю!

        skeletonsWave1.transform.position = spawnPos;
        skeletonsWave1.transform.LookAt(playerTransform);
        skeletonsWave1.SetActive(true);

        foreach (EnemyAI ai in skeletonsWave1.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null) ai.isCinematicFrozen = true;
        }

        // ФІКС: Камера летить 3.5 сек, а вилазять вони за 2.5 сек (щоб ти встиг побачити їх повністю на землі)
        Coroutine cameraFly = StartCoroutine(DroneCameraFlyAndTrack(spawnPos, 3.5f));
        Coroutine riseAnim = StartCoroutine(RiseFromGroundAnim(skeletonsWave1.transform, 2.5f));

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Watch out! They're crawling from the dirt!", 2f));

        yield return cameraFly;

        foreach (EnemyAI ai in skeletonsWave1.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null) ai.isCinematicFrozen = false;
        }

        if (pController != null) pController.enabled = true;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

        StartCoroutine(ShowTutorialHint("[TIP] Enemies are attacking! Use Left Mouse Button to fight back and watch your health.", 5f));
    }

    public void EnemyDefeated()
    {
        if (currentQuestStep == 2)
        {
            defeatedSkeletonsW1++;
            if (objectiveUI != null) objectiveUI.UpdateProgress(defeatedSkeletonsW1, totalSkeletonsW1);
            if (defeatedSkeletonsW1 >= totalSkeletonsW1) AdvanceQuest();
        }
    }

    // --- ФІНАЛ: ЖАХ І ВТЕЧА ДО КОНЯ ---
    private IEnumerator TriggerHordeAndFleeRoutine()
    {
        PlayerController pController = playerTransform.GetComponent<PlayerController>();
        if (pController != null) pController.enabled = false;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Good job! Wait... do you hear that?", 1.5f));

        Vector3 hordePos = playerTransform.position + (playerTransform.right * spawnDistanceBehind);
        hordePos = GetTerrainPos(hordePos); // ФІКС: Ставимо орду на реальну землю!

        skeletonsHordeWave2.transform.position = hordePos;
        skeletonsHordeWave2.transform.LookAt(playerTransform);
        skeletonsHordeWave2.SetActive(true);

        foreach (EnemyAI ai in skeletonsHordeWave2.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null)
            {
                ai.MakeInvincibleAndFurious();
                ai.isCinematicFrozen = true;
            }
        }

        StartCoroutine(RiseFromGroundAnim(skeletonsHordeWave2.transform, 2.5f)); // Орда вилазить швидше
        yield return StartCoroutine(DroneCameraFlyAndTrack(hordePos, 3f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: IT'S A WHOLE ARMY! THERE'S TOO MANY!", 2f));

        if (evacuationHorse != null)
        {
            evacuationHorse.SetActive(true);
            yield return StartCoroutine(DroneCameraFlyAndTrack(evacuationHorse.transform.position, 2.5f));
        }

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: RUN TO THE HORSE, NOW!!", 2f));

        foreach (EnemyAI ai in skeletonsHordeWave2.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null) ai.isCinematicFrozen = false;
        }

        if (pController != null) pController.enabled = true;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

        StartCoroutine(ShowTutorialHint("[TIP] You can't kill them! Hold SHIFT to sprint and reach the Extraction Point!", 6f));
    }

    // --- ДРОН-КАМЕРА: ВИСОКО І ДИНАМІЧНО ---
    private IEnumerator DroneCameraFlyAndTrack(Vector3 targetPosition, float flyDuration)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        SetCinematicMode(true);

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 cinematicPos = targetPosition + new Vector3(0, 10f, -8f);
            cinematicPos += new Vector3(Mathf.Sin(elapsed) * 3f, 0, Mathf.Cos(elapsed) * 3f);

            float t = Mathf.SmoothStep(0, 1, elapsed / 1.5f);
            mainCam.transform.position = Vector3.Lerp(startPos, cinematicPos, t);

            Quaternion cinematicRot = Quaternion.LookRotation(targetPosition - mainCam.transform.position);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, cinematicRot, t);

            yield return null;
        }

        CameraFollow cf = mainCam.GetComponent<CameraFollow>();
        if (cf != null)
        {
            Vector3 currentRot = mainCam.transform.eulerAngles;
            cf.SyncRotation(currentRot.y, currentRot.x);
        }
        SetCinematicMode(false);
    }

    private IEnumerator ShowTutorialHint(string text, float duration)
    {
        if (subtitleText == null) yield break;
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
        // Зменшив глибину до -2.5, щоб голови з'являлися майже одразу
        group.position = finalPos - new Vector3(0, 2.5f, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Математична функція SmoothStep для плавного старту і сповільнення в кінці
            t = t * t * (3f - 2f * t);

            group.position = Vector3.Lerp(finalPos - new Vector3(0, 2.5f, 0), finalPos, t);
            yield return null;
        }
        group.position = finalPos;
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