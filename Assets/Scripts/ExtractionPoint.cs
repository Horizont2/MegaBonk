using UnityEngine;

public class ExtractionPoint : MonoBehaviour
{
    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.HidePrompt();

                // «бер≥гаЇмо з≥бран≥ д≥аманти гравц€
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) SaveManager.AddCrystals(pc.crystalsCollected);

                // ћј√≤я: ѕереносимо з≥бран≥ ресурси на склад (з авто-продажем надлишку)
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.EvacuateRunToStash();
                }

                GlobalHUD.Instance.FadeAndLoadScene("CampScene");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowPrompt("Press E to Evacuate");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
        }
    }
}