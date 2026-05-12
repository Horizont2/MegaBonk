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
        if (subtitleText != null)
        {
            subtitleText.text = "";
            // Скидаємо обмеження на кількість видимих символів на старті
            subtitleText.maxVisibleCharacters = 99999;
        }

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
            if (pObj != null) pObj.GetComponent<PlayerController>().isControlBlocked = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

            yield return null; // Чекаємо один кадр, щоб таймлайн точно ініціалізувався

            while (introDirector.state == PlayState.Playing)
            {
                yield return null;
            }

            // --- ПОВЕРНУТО ТА ПОКРАЩЕНО ФІКС КАМЕРИ ---
            CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
            if (cf != null)
            {
                // Запам'ятовуємо, куди дивилася камера в останній кадр катсцени
                Vector3 currentRot = Camera.main.transform.eulerAngles;

                // Перевірка на "перекручені" кути (наприклад, 350 градусів замість -10)
                float pitchX = currentRot.x;
                if (pitchX > 180f) pitchX -= 360f;

                // Передаємо ці кути звичайній камері, щоб не було різкого стрибка
                cf.SyncRotation(currentRot.y, pitchX);
            }
            // -----------------------------

            // Повертаємо контроль після таймлайну
            if (pObj != null) pObj.GetComponent<PlayerController>().isControlBlocked = false;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

            SetCinematicMode(false);

            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            // Викликаємо оновлення UI ТІЛЬКИ ТУТ, щоб плашка виїхала красиво
            UpdateObjectiveUI();
        }
        else
        {
            UpdateObjectiveUI();
        }
    }

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

    private IEnumerator TriggerAmbushWave1Routine()
    {
        isAmbushTriggered = true;

        PlayerController pController = playerTransform.GetComponent<PlayerController>();
        if (pController != null) pController.isControlBlocked = true;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

        Vector3 spawnPos = playerTransform.position - (playerTransform.forward * spawnDistanceBehind);
        spawnPos = GetTerrainPos(spawnPos);

        skeletonsWave1.transform.position = spawnPos;
        skeletonsWave1.transform.LookAt(playerTransform);
        skeletonsWave1.SetActive(true);

        foreach (EnemyAI ai in skeletonsWave1.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null) ai.isCinematicFrozen = true;
        }

        Coroutine cameraFly = StartCoroutine(DroneCameraFlyAndTrack(spawnPos, 3.5f));
        Coroutine riseAnim = StartCoroutine(RiseFromGroundAnim(skeletonsWave1.transform, 2.5f));

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Watch out! They're crawling from the dirt!", 2f));

        yield return cameraFly;

        foreach (EnemyAI ai in skeletonsWave1.GetComponentsInChildren<EnemyAI>())
        {
            if (ai != null) ai.isCinematicFrozen = false;
        }

        if (pController != null) pController.isControlBlocked = false;
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

    private IEnumerator TriggerHordeAndFleeRoutine()
    {
        PlayerController pController = playerTransform.GetComponent<PlayerController>();
        if (pController != null) pController.isControlBlocked = true;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Good job! Wait... do you hear that?", 1.5f));

        Vector3 hordePos = playerTransform.position + (playerTransform.right * spawnDistanceBehind);
        hordePos = GetTerrainPos(hordePos);

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

        StartCoroutine(RiseFromGroundAnim(skeletonsHordeWave2.transform, 2.5f));
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

        if (pController != null) pController.isControlBlocked = false;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

        StartCoroutine(ShowTutorialHint("[TIP] You can't kill them! Hold SHIFT to sprint and reach the Extraction Point!", 6f));
    }

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
            // Той самий захист від перекручених кутів
            float pitchX = currentRot.x;
            if (pitchX > 180f) pitchX -= 360f;
            cf.SyncRotation(currentRot.y, pitchX);
        }
        SetCinematicMode(false);
    }

    // ФІКСОВАНА ТУТОРІАЛ-ПІДКАЗКА
    private IEnumerator ShowTutorialHint(string text, float duration)
    {
        if (subtitleText == null) yield break;

        // Відключаємо обмеження, щоб підказка показалася повністю відразу
        subtitleText.maxVisibleCharacters = 99999;
        subtitleText.text = $"<color=#88CCFF>{text}</color>";

        yield return new WaitForSeconds(duration);
        subtitleText.text = "";
    }

    // ФІКСОВАНИЙ РОЗУМНИЙ TYPEWRITER
    private IEnumerator ShowSubtitleTypewriter(string text, float stayDuration)
    {
        if (subtitleText == null) yield break;

        // 1. Одразу передаємо весь текст. Це змусить TextMeshPro розрахувати 
        // фінальні розміри, перенесення на нові рядки та центрування.
        subtitleText.text = text;

        // 2. Примусово оновлюємо сітку (Mesh) тексту в цьому ж кадрі, 
        // щоб отримати точну кількість символів.
        subtitleText.ForceMeshUpdate();
        int totalCharacters = subtitleText.textInfo.characterCount;

        // 3. Робимо всі символи "невидимими"
        subtitleText.maxVisibleCharacters = 0;

        // 4. Плавно збільшуємо кількість видимих символів. 
        // Оскільки фінальний макет вже прорахований, текст не буде скакати!
        for (int i = 0; i <= totalCharacters; i++)
        {
            subtitleText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(stayDuration);
        subtitleText.text = "";

        // 5. Повертаємо параметр у стандартний стан для майбутніх текстів
        subtitleText.maxVisibleCharacters = 99999;
    }

    private IEnumerator RiseFromGroundAnim(Transform group, float duration)
    {
        Vector3 finalPos = group.position;
        group.position = finalPos - new Vector3(0, 2.5f, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

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