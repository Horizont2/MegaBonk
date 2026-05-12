using UnityEngine;

public class NPC_Dialogue : MonoBehaviour
{
    [Header("Quest UI")]
    public GameObject questMarker; // Посилання на літаючий знак оклику

    private bool isPlayerInRange = false;
    private bool hasTalked = false;

    private void Start()
    {
        // Переконуємося, що знак оклику увімкнений на початку сцени
        if (questMarker != null) questMarker.SetActive(true);
    }

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

            // ВИМИКАЄМО знак оклику, бо діалог розпочато
            if (questMarker != null) questMarker.SetActive(false);

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