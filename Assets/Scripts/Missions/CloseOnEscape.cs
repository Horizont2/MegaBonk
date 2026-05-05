using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseOnEscape : MonoBehaviour
{
    private Button closeButton;

    void Awake()
    {
        closeButton = GetComponent<Button>();
    }

    void Update()
    {
        // якщо кнопка активна на екран≥ ≥ ми тиснемо Esc Ч симулюЇмо кл≥к по н≥й
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            closeButton.onClick.Invoke();
        }
    }
}