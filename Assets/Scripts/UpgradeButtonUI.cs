using UnityEngine;
using UnityEngine.UI;
using TMPro; // Для роботи з TextMeshPro

public class UpgradeButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statText;
    public Button buttonComponent;

    private void Awake()
    {
        if (buttonComponent == null) buttonComponent = GetComponent<Button>();
    }
}