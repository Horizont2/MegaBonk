using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public GameObject questWindow;
    private bool isPlayerNearby = false;

    void Update()
    {
        // Jeśli gracz jest blisko i wciśnie E
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            bool isActive = questWindow.activeSelf;
            questWindow.SetActive(!isActive);

            // ZARZĄDZANIE KURSOREM:
            if (!isActive) // Jeśli okno właśnie się otwiera
            {
                Cursor.visible = true; // Pokaż kursor
                Cursor.lockState = CursorLockMode.None; // Odblokuj go z centrum ekranu
            }
            else // Jeśli okno się zamyka (bo wcisnąłeś E drugi raz)
            {
                Cursor.visible = false; // Ukryj kursor
                Cursor.lockState = CursorLockMode.Locked; // Zablokuj z powrotem na środku
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            questWindow.SetActive(false);

            // Ukrywamy kursor, gdy gracz odejdzie od NPC bez zamykania okna
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}