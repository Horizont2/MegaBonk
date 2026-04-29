using UnityEngine;

public class StoryExtractionPoint : MonoBehaviour
{
    private bool isPlayerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowPrompt("[E] Escape to Camp");
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
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();

            // «¡≈–≤√¿™ÃŒ À”“ ≤ ¬¿À»ÃŒ ¬ “¿¡≤–!
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.EvacuateRunToStash();
            }

            if (GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.FadeAndLoadScene("CampScene");
            }
        }
    }
}