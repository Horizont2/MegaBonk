using UnityEngine;

public class ShopTeleporter : MonoBehaviour
{
    public string shopSceneName = "ShopScene"; // Назва сцени магазину
    private bool isPlayerNear = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (GlobalHUD.Instance != null)
                GlobalHUD.Instance.ShowPrompt("Press E to Enter Shop");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (GlobalHUD.Instance != null)
                GlobalHUD.Instance.HidePrompt();
        }
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            isPlayerNear = false; // Блокуємо повторні натискання
            if (GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.HidePrompt(); // Ховаємо текст
                GlobalHUD.Instance.FadeAndLoadScene(shopSceneName); // Затемнюємо екран і переходимо
            }
        }
    }
}