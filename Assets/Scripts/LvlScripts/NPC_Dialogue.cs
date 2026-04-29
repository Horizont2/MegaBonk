using UnityEngine;

public class NPC_Dialogue : MonoBehaviour
{
    [Header("Settings")]
    public string npcName = "Stranger";

    private bool isPlayerInRange = false;
    private bool hasTalked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTalked)
        {
            isPlayerInRange = true;
            if (GlobalHUD.Instance != null)
                GlobalHUD.Instance.ShowPrompt("[E] Talk to " + npcName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (GlobalHUD.Instance != null)
                GlobalHUD.Instance.HidePrompt();
        }
    }

    private void Update()
    {
        if (isPlayerInRange && !hasTalked && Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        hasTalked = true;
        isPlayerInRange = false;

        if (GlobalHUD.Instance != null)
            GlobalHUD.Instance.HidePrompt();

        // ѕовертаЇмо ковбо€ обличч€м до гравц€
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);

        // ѕередаЇмо сигнал режисеру р≥вн€
        if (Level1_QuestManager.Instance != null)
        {
            Level1_QuestManager.Instance.AdvanceQuest();
        }
    }
}