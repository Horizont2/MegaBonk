using UnityEngine;

public class NPC_Dialogue : MonoBehaviour
{
    private bool isPlayerInRange = false;
    private bool hasTalked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTalked)
        {
            isPlayerInRange = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowPrompt("[E] Talk to Stranger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
        }
    }

    private void Update()
    {
        if (isPlayerInRange && !hasTalked && Input.GetKeyDown(KeyCode.E))
        {
            hasTalked = true;
            isPlayerInRange = false;

            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();

            // Повертаємо ковбоя до гравця
            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            Vector3 lookPos = player.position - transform.position;
            lookPos.y = 0;
            transform.rotation = Quaternion.LookRotation(lookPos);

            // ЗАПУСКАЄМО ДІАЛОГ
            if (Level1_QuestManager.Instance != null)
            {
                Level1_QuestManager.Instance.StartIntroDialogue();
            }
        }
    }
}