using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Animated death statistics screen. Creates its own UI programmatically.
/// Attach to an empty GameObject in the scene. It will build the full overlay at runtime.
/// </summary>
public class DeathStatsScreen : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeShow = 0.5f;
    public float bgFadeDuration = 0.6f;
    public float statRevealInterval = 0.25f;
    public float statCountDuration = 0.6f;
    public float titleDropDuration = 0.5f;

    [Header("Colors")]
    public Color bgColor = new Color(0f, 0f, 0f, 0.85f);
    public Color titleColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color labelColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color valueColor = Color.white;
    public Color buttonColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    // Built at runtime
    private Canvas canvas;
    private CanvasGroup rootGroup;
    private Image bgImage;
    private RectTransform contentRoot;

    // Stat rows for animation
    private TextMeshProUGUI titleText;
    private StatRow[] statRows;
    private Button continueButton;
    private TextMeshProUGUI continueText;

    private struct StatRow
    {
        public RectTransform root;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public CanvasGroup group;
        public float targetValue;
        public bool isTime;
        public bool isFloat;
    }

    /// <summary>
    /// Call this from GameManager to show the death screen with current stats.
    /// </summary>
    public void Show(float survivalTime, int kills, float damageDealt, float damageTaken,
                     int level, int crystals)
    {
        BuildUI();
        PopulateStats(survivalTime, kills, damageDealt, damageTaken, level, crystals);
        StartCoroutine(AnimateIn());
    }

    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DeathStatsCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Root with CanvasGroup for master fade
        GameObject rootObj = new GameObject("Root");
        rootObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootGroup = rootObj.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        // Dark background
        bgImage = rootObj.AddComponent<Image>();
        bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);

        // Content container (centered column)
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(rootObj.transform, false);
        contentRoot = contentObj.AddComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = new Vector2(600, 600);
        contentRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        titleText = CreateText(contentRoot, "YOU DIED", 52, titleColor, FontStyles.Bold);
        titleText.GetComponent<LayoutElement>().preferredHeight = 80;
        titleText.rectTransform.localScale = Vector3.one * 2f;

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(contentRoot, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().preferredHeight = 20;
    }

    private void PopulateStats(float survivalTime, int kills, float damageDealt,
                                float damageTaken, int level, int crystals)
    {
        statRows = new StatRow[6];
        statRows[0] = CreateStatRow("SURVIVAL TIME", survivalTime, isTime: true);
        statRows[1] = CreateStatRow("ENEMIES KILLED", kills);
        statRows[2] = CreateStatRow("DAMAGE DEALT", damageDealt, isFloat: true);
        statRows[3] = CreateStatRow("DAMAGE TAKEN", damageTaken, isFloat: true);
        statRows[4] = CreateStatRow("LEVEL REACHED", level);
        statRows[5] = CreateStatRow("CRYSTALS", crystals);

        // Continue button
        GameObject btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(contentRoot, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnObj.AddComponent<LayoutElement>().preferredHeight = 60;

        // Spacer before button
        GameObject spacer2 = new GameObject("Spacer2");
        spacer2.transform.SetParent(contentRoot, false);
        spacer2.AddComponent<RectTransform>();
        spacer2.AddComponent<LayoutElement>().preferredHeight = 10;
        spacer2.transform.SetSiblingIndex(btnObj.transform.GetSiblingIndex());

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = buttonColor;

        // Round corners via sprite (fallback: solid color)
        continueButton = btnObj.AddComponent<Button>();
        continueButton.targetGraphic = btnBg;

        ColorBlock cb = continueButton.colors;
        cb.highlightedColor = new Color(1f, 0.45f, 0.45f, 1f);
        cb.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f);
        continueButton.colors = cb;
        continueButton.onClick.AddListener(OnContinueClicked);

        continueText = CreateText(btnRect, "CONTINUE", 28, Color.white, FontStyles.Bold);
        continueText.alignment = TextAlignmentOptions.Center;
        continueText.rectTransform.anchorMin = Vector2.zero;
        continueText.rectTransform.anchorMax = Vector2.one;
        continueText.rectTransform.offsetMin = Vector2.zero;
        continueText.rectTransform.offsetMax = Vector2.zero;

        // Start hidden
        CanvasGroup btnGroup = btnObj.AddComponent<CanvasGroup>();
        btnGroup.alpha = 0f;
    }

    private StatRow CreateStatRow(string label, float value, bool isTime = false, bool isFloat = false)
    {
        GameObject rowObj = new GameObject("Row_" + label);
        rowObj.transform.SetParent(contentRoot, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowObj.AddComponent<LayoutElement>().preferredHeight = 45;

        CanvasGroup cg = rowObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Horizontal layout
        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;

        TextMeshProUGUI labelTmp = CreateText(rowRect, label, 26, labelColor, FontStyles.Normal);
        labelTmp.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI valueTmp = CreateText(rowRect, "0", 30, valueColor, FontStyles.Bold);
        valueTmp.alignment = TextAlignmentOptions.Right;

        return new StatRow
        {
            root = rowRect,
            label = labelTmp,
            value = valueTmp,
            group = cg,
            targetValue = value,
            isTime = isTime,
            isFloat = isFloat
        };
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string text, int size, Color color, FontStyles style)
    {
        GameObject textObj = new GameObject("Text_" + text);
        textObj.transform.SetParent(parent, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        textObj.AddComponent<LayoutElement>();

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;

        return tmp;
    }

    // ─── ANIMATION ───

    private IEnumerator AnimateIn()
    {
        yield return new WaitForSecondsRealtime(delayBeforeShow);

        rootGroup.alpha = 1f;

        // Fade in background
        float t = 0f;
        while (t < bgFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / bgFadeDuration);
            bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, bgColor.a * a);
            yield return null;
        }

        // Title drop animation (scale from 2x to 1x with overshoot)
        yield return StartCoroutine(AnimateTitleDrop());

        // Reveal each stat row one by one with count-up
        for (int i = 0; i < statRows.Length; i++)
        {
            StartCoroutine(RevealStatRow(statRows[i]));
            yield return new WaitForSecondsRealtime(statRevealInterval);
        }

        // Wait for last row to finish counting
        yield return new WaitForSecondsRealtime(statCountDuration);

        // Show continue button with fade + scale pop
        CanvasGroup btnGroup = continueButton.GetComponent<CanvasGroup>();
        continueButton.transform.localScale = Vector3.one * 0.5f;

        t = 0f;
        float popDur = 0.35f;
        while (t < popDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDur);
            // Elastic ease out
            float elastic = 1f + (Mathf.Pow(2f, -10f * p) * Mathf.Sin((p - 0.075f) * (2f * Mathf.PI) / 0.3f));
            btnGroup.alpha = Mathf.Clamp01(t / (popDur * 0.5f));
            continueButton.transform.localScale = Vector3.one * elastic;
            yield return null;
        }
        btnGroup.alpha = 1f;
        continueButton.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateTitleDrop()
    {
        float t = 0f;
        Vector3 startScale = Vector3.one * 2f;
        Color startColor = new Color(titleColor.r, titleColor.g, titleColor.b, 0f);

        while (t < titleDropDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / titleDropDuration);

            // Overshoot curve: goes to ~1.15 then settles to 1.0
            float overshoot = 1f + 0.15f * Mathf.Sin(p * Mathf.PI);
            float scale = Mathf.Lerp(2f, 1f, p) * (p < 0.8f ? overshoot : 1f);

            titleText.rectTransform.localScale = Vector3.one * scale;
            titleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, Mathf.Clamp01(p * 2f));
            yield return null;
        }

        titleText.rectTransform.localScale = Vector3.one;
        titleText.color = titleColor;
    }

    private IEnumerator RevealStatRow(StatRow row)
    {
        // Slide in from left + fade
        Vector2 startPos = row.root.anchoredPosition + Vector2.left * 60f;
        Vector2 endPos = row.root.anchoredPosition;

        float t = 0f;
        float fadeDur = 0.3f;
        while (t < fadeDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease out cubic

            row.group.alpha = ease;
            row.root.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            yield return null;
        }
        row.group.alpha = 1f;
        row.root.anchoredPosition = endPos;

        // Count up animation
        t = 0f;
        while (t < statCountDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / statCountDuration);
            float ease = 1f - Mathf.Pow(1f - p, 2f); // ease out quad

            float current = row.targetValue * ease;
            row.value.text = FormatValue(current, row.isTime, row.isFloat);
            yield return null;
        }
        row.value.text = FormatValue(row.targetValue, row.isTime, row.isFloat);
    }

    private string FormatValue(float value, bool isTime, bool isFloat)
    {
        if (isTime)
        {
            int minutes = Mathf.FloorToInt(value / 60f);
            int seconds = Mathf.FloorToInt(value % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        if (isFloat)
            return Mathf.FloorToInt(value).ToString("N0");

        return Mathf.FloorToInt(value).ToString("N0");
    }

    private void OnContinueClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
