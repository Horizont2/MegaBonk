using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public GameObject questWindow;
    public TMP_Text activeQuestText;

    void Start()
    {
        // Na start gry upewniamy się, że tekst questa jest ukryty
        activeQuestText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Zamknij pod ESC
        if (Input.GetKeyDown(KeyCode.Escape) && questWindow.activeSelf)
        {
            CloseQuestWindow();
        }
    }

    public void AcceptQuest(string questName)
    {
        activeQuestText.text = "Aktywny quest: " + questName;
        
        // Pokazujemy tekst na ekranie po akceptacji!
        activeQuestText.gameObject.SetActive(true); 
        
        CloseQuestWindow();
    }

    public void CloseQuestWindow()
    {
        questWindow.SetActive(false);

        // ZARZĄDZANIE KURSOREM po zamknięciu okna przez UI lub ESC:
        Cursor.visible = false; // Ukryj kursor
        Cursor.lockState = CursorLockMode.Locked; // Zablokuj go na środku
    }
}