#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class SmartFontChanger : EditorWindow
{
    public TMP_FontAsset titleFont; // Сюди покладемо Cinzel
    public TMP_FontAsset bodyFont;  // Сюди покладемо Montserrat

    // Додаємо нову кнопку у верхнє меню Unity
    [MenuItem("Tools/AAA Font Auto-Assigner")]
    public static void ShowWindow()
    {
        GetWindow<SmartFontChanger>("Font Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("1. Assign your fonts here:", EditorStyles.boldLabel);
        titleFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Title Font (Cinzel)", titleFont, typeof(TMP_FontAsset), false);
        bodyFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Body Font (Montserrat)", bodyFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);
        GUILayout.Label("2. Select your AAA_Panel in the Hierarchy.", EditorStyles.boldLabel);

        GUILayout.Space(10);
        if (GUILayout.Button("Apply Fonts to Selected UI", GUILayout.Height(40)))
        {
            ApplyFonts();
        }
    }

    private void ApplyFonts()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Please select a UI Canvas or Panel (e.g., AAA_Panel) in the hierarchy first!");
            return;
        }

        // Шукаємо всі тексти у вибраному об'єкті
        TextMeshProUGUI[] allTexts = Selection.activeGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        int changedCount = 0;

        foreach (var txt in allTexts)
        {
            Undo.RecordObject(txt, "Change Font"); // Дозволяє натиснути Ctrl+Z, якщо щось піде не так

            string objName = txt.gameObject.name.ToLower();

            // ЛОГІКА: Якщо об'єкт називається TitleText, LvlText або Header - даємо йому Cinzel
            if (objName.Contains("title") || objName.Contains("lvl") || objName.Contains("header"))
            {
                if (titleFont != null) txt.font = titleFont;
            }
            // Всі інші тексти (Desc, Info, Wood, Stone, Progress тощо) отримують Montserrat
            else
            {
                if (bodyFont != null) txt.font = bodyFont;
            }

            changedCount++;
            EditorUtility.SetDirty(txt); // Зберігаємо зміни
        }

        Debug.Log($"<color=#00FF00>Success!</color> Updated fonts for {changedCount} text objects in {Selection.activeGameObject.name}.");
    }
}
#endif