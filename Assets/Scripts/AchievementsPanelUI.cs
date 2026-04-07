using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Achievements list panel for the main menu. Builds UI programmatically.
/// Call Show() / Hide() to toggle. Attach to any GameObject in MainMenu scene.
/// </summary>
public class AchievementsPanelUI : MonoBehaviour
{
    [Header("Colors")]
    public Color unlockedBgColor = new Color(0.18f, 0.22f, 0.18f, 0.95f);
    public Color lockedBgColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
    public Color unlockedTextColor = new Color(1f, 0.85f, 0.2f);
    public Color lockedTextColor = new Color(0.4f, 0.4f, 0.4f);
    public Color descColor = new Color(0.7f, 0.7f, 0.7f);

    private GameObject panelRoot;
    private CanvasGroup panelGroup;
    private TextMeshProUGUI headerText;
    private RectTransform scrollContent;
    private bool isVisible = false;

    public void Show()
    {
        if (panelRoot == null) BuildPanel();
        PopulateAchievements();
        panelRoot.SetActive(true);
        isVisible = true;
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        isVisible = false;
    }

    public void Toggle()
    {
        if (isVisible) Hide(); else Show();
    }

    private void BuildPanel()
    {
        // Find canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Full-screen overlay
        panelRoot = new GameObject("AchievementsPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = panelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Dark background
        Image bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);
        panelGroup = panelRoot.AddComponent<CanvasGroup>();

        // Close on background click
        Button bgBtn = panelRoot.AddComponent<Button>();
        bgBtn.onClick.AddListener(Hide);
        ColorBlock cb = bgBtn.colors;
        cb.highlightedColor = new Color(0, 0, 0, 0.9f);
        cb.pressedColor = new Color(0, 0, 0, 0.95f);
        bgBtn.colors = cb;

        // Content panel (centered, with padding)
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(panelRoot.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.15f, 0.1f);
        contentRect.anchorMax = new Vector2(0.85f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = new Color(0.08f, 0.08f, 0.08f, 0.98f);

        VerticalLayoutGroup vlg = contentPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 5;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        // Header
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(contentPanel.transform, false);
        headerObj.AddComponent<RectTransform>();
        headerObj.AddComponent<LayoutElement>().preferredHeight = 60;
        headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.fontSize = 36;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = unlockedTextColor;
        headerText.alignment = TextAlignmentOptions.Center;

        // Scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(contentPanel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scrollObj.AddComponent<Image>().color = Color.clear;
        scrollObj.AddComponent<Mask>().showMaskGraphic = false;

        // Scroll content
        GameObject scrollContentObj = new GameObject("ScrollContent");
        scrollContentObj.transform.SetParent(scrollObj.transform, false);
        scrollContent = scrollContentObj.AddComponent<RectTransform>();
        scrollContent.anchorMin = new Vector2(0, 1);
        scrollContent.anchorMax = new Vector2(1, 1);
        scrollContent.pivot = new Vector2(0.5f, 1);
        scrollContent.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup scrollVlg = scrollContentObj.AddComponent<VerticalLayoutGroup>();
        scrollVlg.spacing = 6;
        scrollVlg.childControlWidth = true;
        scrollVlg.childControlHeight = false;
        scrollVlg.childForceExpandWidth = true;
        scrollVlg.padding = new RectOffset(5, 5, 5, 5);

        ContentSizeFitter fitter = scrollContentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = scrollContent;
    }

    private void PopulateAchievements()
    {
        // Clear old entries
        foreach (Transform child in scrollContent)
            Destroy(child.gameObject);

        if (AchievementManager.Instance == null) return;

        List<AchievementDef> allAch = AchievementManager.Instance.GetAllAchievements();
        int unlocked = AchievementManager.Instance.GetUnlockedCount();
        headerText.text = "ACHIEVEMENTS  " + unlocked + " / " + allAch.Count;

        foreach (AchievementDef ach in allAch)
        {
            bool isUnlocked = AchievementManager.Instance.IsUnlocked(ach.id);
            CreateAchievementRow(ach, isUnlocked);
        }
    }

    private void CreateAchievementRow(AchievementDef ach, bool unlocked)
    {
        GameObject row = new GameObject("Ach_" + ach.id);
        row.transform.SetParent(scrollContent, false);
        row.AddComponent<RectTransform>();
        row.AddComponent<LayoutElement>().preferredHeight = 55;

        Image rowBg = row.AddComponent<Image>();
        rowBg.color = unlocked ? unlockedBgColor : lockedBgColor;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(15, 15, 5, 5);
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Status icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(row.transform, false);
        iconObj.AddComponent<RectTransform>();
        iconObj.AddComponent<LayoutElement>().preferredWidth = 30;
        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = unlocked ? "V" : "X";
        iconText.fontSize = 24;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = unlocked ? new Color(0.3f, 1f, 0.3f) : new Color(0.5f, 0.2f, 0.2f);
        iconText.alignment = TextAlignmentOptions.Center;

        // Text container
        GameObject textCont = new GameObject("TextCont");
        textCont.transform.SetParent(row.transform, false);
        textCont.AddComponent<RectTransform>();
        textCont.AddComponent<LayoutElement>().preferredWidth = 500;

        VerticalLayoutGroup textVlg = textCont.AddComponent<VerticalLayoutGroup>();
        textVlg.childControlWidth = true;
        textVlg.childControlHeight = false;
        textVlg.childForceExpandWidth = true;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(textCont.transform, false);
        titleObj.AddComponent<RectTransform>();
        titleObj.AddComponent<LayoutElement>().preferredHeight = 25;
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = ach.title;
        title.fontSize = 20;
        title.fontStyle = FontStyles.Bold;
        title.color = unlocked ? unlockedTextColor : lockedTextColor;

        // Description
        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(textCont.transform, false);
        descObj.AddComponent<RectTransform>();
        descObj.AddComponent<LayoutElement>().preferredHeight = 20;
        TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
        desc.text = unlocked ? ach.description : "???";
        desc.fontSize = 15;
        desc.color = unlocked ? descColor : lockedTextColor;
    }
}
