using UnityEngine;
using UnityEngine.Playables;
using TMPro;
using System.Collections;
using Unity.Cinemachine; // Для Unity 6

public class CampDirector : MonoBehaviour
{
    [Header("Cinematic & UI")]
    public PlayableDirector timelineDirector;
    public PlayerController player;

    public TextMeshProUGUI subtitleText;
    public float typingSpeed = 0.04f;

    private void Start()
    {
        // Перевіряємо, чи був туторіал пройдений раніше
        bool tutorialPlayed = PlayerPrefs.GetInt("CampTutorialPlayed", 0) == 1;

        if (!tutorialPlayed)
        {
            StartCampTutorial();
        }
        else
        {
            // ЯКЩО ТУТОРІАЛ ВЖЕ БУВ:
            // Нам треба негайно вимкнути все кінематографічне, щоб камера не "липла"
            StopCutsceneImmediately();
        }
    }

    private void StartCampTutorial()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<PlayerController>();
        }

        if (player != null) player.enabled = false;

        // Ховаємо інтерфейс GlobalHUD
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(false);

        // Вимикаємо скрипти колізій і вмикаємо Cinemachine Brain
        SetCinematicMode(true);

        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = true;

        if (timelineDirector != null) timelineDirector.Play();
        StartCoroutine(TutorialDialogueRoutine());
    }

    private void StopCutsceneImmediately()
    {
        if (timelineDirector != null) timelineDirector.Stop();

        // Переконуємось, що інтерфейс видимий
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.SetGameplayPanelsActive(true);

        // Повертаємо керування гравцю та вимикаємо Cinemachine
        SetCinematicMode(false);

        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;
    }

    private void SetCinematicMode(bool isCinematic)
    {
        CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
        if (cf != null) cf.isCinematicMode = isCinematic;

        CameraCollision cc = Camera.main.GetComponent<CameraCollision>();
        if (cc != null) cc.isCinematicMode = isCinematic;
    }

    private IEnumerator TutorialDialogueRoutine()
    {
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Welcome to your new Camp. This is your safe haven between dangerous journeys.", 2f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Up there is the Camp Stash. All the resources you manage to bring back from the forest are stored here safely.", 2.5f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: You can use those resources to rebuild this place. Walk up to a plot and hold [E]. Restored buildings will generate resources over time!", 3f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: Check the Notice Board over there. You can take on special missions to earn valuable Diamonds.", 2.5f));
        yield return StartCoroutine(ShowSubtitleTypewriter("Stranger: At the edge of the camp is the mysterious Shop. Use your Diamonds there to buy permanent meta-upgrades for your future runs.", 3.2f));
        yield return StartCoroutine(ShowTutorialHint("[TIP] Try to prioritize upgrading your Storage Vault early on, so you have enough space for all your hard-earned loot!", 3.8f));

        EndTutorial();
    }

    private void EndTutorial()
    {
        PlayerPrefs.SetInt("CampTutorialPlayed", 1);
        PlayerPrefs.Save();

        if (player != null) player.enabled = true;
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

    private IEnumerator ShowTutorialHint(string text, float duration)
    {
        if (subtitleText == null) yield break;
        subtitleText.text = "";
        string visibleText = "";
        foreach (char c in text)
        {
            visibleText += c;
            subtitleText.text = $"<color=#88CCFF>{visibleText}</color>";
            yield return new WaitForSeconds(typingSpeed);
        }
        yield return new WaitForSeconds(duration);
        subtitleText.text = "";
    }
}