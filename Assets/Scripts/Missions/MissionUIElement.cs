using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MissionUIElement : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    public Image backgroundImage; // �� ��� ��'��� Bg, ���� �� ������ ������

    [Header("Settings")]
    [Tooltip("RETIRED — every objective now animates in. Kept only so existing prefabs and scenes do not lose a serialized field; setting it does nothing.")]
    public bool animateAppearance = false;
    public float animationDuration = 0.5f;

    private CanvasGroup canvasGroup;
    public bool isCompleted = false;

    private string baseDescription = "";
    // Cache the background image's authored colour so we can restore it if
    // the CompleteMission flash coroutine gets StopAllCoroutines'd mid-flash
    // (which happened when Setup was called during the 0.5s white flash —
    // the flash never lerped back and the tile stayed pure white).
    private Color originalBgColor = Color.white;
    private bool originalBgCached = false;

    private bool _setupCalled = false;

    // ==== THE POLISH LAYER, BUILT AT RUNTIME ====
    //
    // Everything below is generated rather than added to the prefab. The plate
    // is already wired into two scenes and both quest chains, and a change that
    // requires re-wiring is a change that half the prefabs quietly miss — the
    // objective would then look different depending on which scene you were in.
    // Generating it means every plate gets the same treatment automatically and
    // an old prefab cannot be left behind.
    [Header("Polish")]
    [Tooltip("Accent stripe down the left edge. It is what turns a text box into something the eye reads as an objective.")]
    public bool showAccentBar = true;
    [Tooltip("Reveal the objective text a character at a time. Motion in the corner of the screen is what actually gets a new objective noticed — a line that simply appears does not.")]
    public bool typewriter = true;
    public float typeSpeed = 55f;

    private Image _accent;
    private Image _progressFill;
    private RectTransform _progressRT;
    private TextMeshProUGUI _tick;
    private Coroutine _typing;
    private Coroutine _idle;

    private static readonly Color AccentActive = new Color(1f, 0.82f, 0.35f);
    private static readonly Color AccentDone = new Color(0.35f, 0.95f, 0.45f);

    // Builds the accent stripe, the progress fill and the completion tick, once.
    // All parented to the same visual root the appear-slide already moves, so
    // they travel with the plate instead of being left behind by it.
    private void EnsureFurniture()
    {
        if (backgroundImage == null || _accent != null) return;
        RectTransform host = backgroundImage.rectTransform;

        if (showAccentBar)
        {
            var go = new GameObject("AccentBar", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(host, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(0f, 4f);
            rt.offsetMax = new Vector2(5f, -4f);
            _accent = go.AddComponent<Image>();
            _accent.color = AccentActive;
            _accent.raycastTarget = false;
        }

        // A thin fill along the bottom edge. Only shown for stepped objectives —
        // "collect 3 of 5" is a bar, "go and talk to Elias" is not, and a bar
        // that is always empty teaches the player to ignore bars.
        var pgo = new GameObject("ProgressFill", typeof(RectTransform));
        _progressRT = pgo.GetComponent<RectTransform>();
        _progressRT.SetParent(host, false);
        _progressRT.anchorMin = new Vector2(0f, 0f);
        _progressRT.anchorMax = new Vector2(1f, 0f);
        _progressRT.pivot = new Vector2(0f, 0f);
        _progressRT.offsetMin = new Vector2(5f, 0f);
        _progressRT.offsetMax = new Vector2(0f, 3f);
        _progressFill = pgo.AddComponent<Image>();
        _progressFill.color = new Color(1f, 0.82f, 0.35f, 0.9f);
        _progressFill.raycastTarget = false;
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillAmount = 0f;
        pgo.SetActive(false);

        var tgo = new GameObject("Tick", typeof(RectTransform));
        var trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(host, false);
        trt.anchorMin = trt.anchorMax = new Vector2(1f, 0.5f);
        trt.pivot = new Vector2(1f, 0.5f);
        trt.anchoredPosition = new Vector2(-14f, 0f);
        trt.sizeDelta = new Vector2(52f, 52f);
        _tick = tgo.AddComponent<TextMeshProUGUI>();
        _tick.text = "\u2713";
        _tick.fontSize = 46f;
        _tick.alignment = TextAlignmentOptions.Center;
        _tick.color = AccentDone;
        _tick.raycastTarget = false;
        MarkNoAutoLocalize(_tick);
        tgo.SetActive(false);
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (backgroundImage != null)
        {
            originalBgColor = backgroundImage.color;
            originalBgCached = true;
        }

        // Start HIDDEN and blank the prefab's placeholder text. The tile is only
        // meaningful once Setup() gives it a real mission — otherwise (e.g. in the
        // camp after a scene transition where no mission is assigned) it showed
        // the raw "New Text" placeholder in the top-left. It reveals itself in
        // Setup. Also stop AutoLocalize from re-keying the placeholder.
        if (titleText != null) { titleText.text = ""; MarkNoAutoLocalize(titleText); }
        if (descriptionText != null) { descriptionText.text = ""; MarkNoAutoLocalize(descriptionText); }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        // If the object gets re-enabled without a mission (scene transitions
        // re-activate HUD panels), keep it hidden until Setup runs.
        if (!_setupCalled && canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private static void MarkNoAutoLocalize(TextMeshProUGUI t)
    {
        if (t != null && t.GetComponent<NoAutoLocalize>() == null)
            t.gameObject.AddComponent<NoAutoLocalize>();
    }

    public void Setup(string title, string description, int current, int target)
    {
        isCompleted = false;
        _setupCalled = true;
        StopAllCoroutines();
        _typing = null;
        _idle = null;
        EnsureFurniture();

        if (_accent != null) { _accent.color = AccentActive; _accent.rectTransform.localScale = Vector3.one; }
        if (_tick != null) _tick.gameObject.SetActive(false);

        // Restore the background if a previous CompleteMission flash was
        // interrupted before its own lerp finished, otherwise the tile shows
        // the raw white flash sprite instead of the mission background.
        if (originalBgCached && backgroundImage != null)
            backgroundImage.color = originalBgColor;

        if (titleText != null) titleText.text = title;

        baseDescription = description;
        UpdateProgress(current, target);

        // EVERY new objective slides in, not only the first.
        //
        // animateAppearance used to switch itself off after one use, so the
        // opening step arrived with a flourish and every step after it silently
        // swapped its text — which is the one moment the player most needs to
        // notice, and the one that got the least attention.
        StartCoroutine(AppearRoutine());
        if (typewriter && descriptionText != null)
            _typing = StartCoroutine(TypeRoutine(baseDescription));
        _idle = StartCoroutine(IdleRoutine());
    }

    // A slow shimmer down the accent bar. Small enough not to nag, moving
    // enough that the plate does not read as a dead label once it has sat in
    // the corner for a minute.
    private IEnumerator IdleRoutine()
    {
        while (_accent != null && !isCompleted)
        {
            float k = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 1.6f));
            _accent.color = AccentActive * k;
            yield return null;
        }
    }

    // Reveals the objective a character at a time. Unscaled: objectives are
    // handed out during dialogue and menus, where the game clock is often
    // stopped, and a line that never finishes typing is worse than none.
    private IEnumerator TypeRoutine(string full)
    {
        if (string.IsNullOrEmpty(full)) yield break;
        descriptionText.text = "";
        float shown = 0f;
        while (shown < full.Length)
        {
            shown += Time.unscaledDeltaTime * typeSpeed;
            int n = Mathf.Clamp(Mathf.FloorToInt(shown), 0, full.Length);
            // Rich-text safe: TMP's maxVisibleCharacters counts glyphs, not the
            // markup around them, so a tag is never cut in half.
            descriptionText.text = full;
            descriptionText.maxVisibleCharacters = n;
            yield return null;
        }
        descriptionText.maxVisibleCharacters = int.MaxValue;
        _typing = null;
    }

    private IEnumerator AppearRoutine()
    {
        // ��ò� ���: �� �� ������ �������� ��'��� (���� ����� Layout Group).
        // �� ������ ����� �������� ������� (Bg) � ������ ��!
        if (backgroundImage == null) yield break;

        Transform visualTransform = backgroundImage.transform;

        // ������� �������� ������� Bg (�������� �� 0,0,0)
        Vector3 targetPos = visualTransform.localPosition;

        // ³������� �� �� 300 ������ ���� ��� ������
        Vector3 startPos = targetPos + new Vector3(-300f, 0, 0);

        float t = 0;
        while (t < 1)
        {
            // Unscaled: objectives are handed out mid-dialogue and mid-menu,
            // where the game clock is often stopped, and a plate frozen halfway
            // through sliding in is worse than one that simply appeared.
            t += Time.unscaledDeltaTime / Mathf.Max(0.05f, animationDuration);
            float curve = Mathf.SmoothStep(0, 1, t);

            // ������ ���������� �������� ��'���
            if (canvasGroup != null) canvasGroup.alpha = curve;

            // ������ �������� �������
            visualTransform.localPosition = Vector3.Lerp(startPos, targetPos, curve);
            yield return null;
        }
        visualTransform.localPosition = targetPos;
    }

    public void UpdateProgress(int current, int target)
    {
        if (isCompleted) return;

        if (descriptionText != null && _typing == null)
        {
            if (target > 1)
                descriptionText.text = $"{baseDescription} (<color=#FFD700>{current}</color>/{target})";
            else
                descriptionText.text = baseDescription;
        }

        // The bar only exists for objectives that actually count something. A
        // bar that is permanently empty because the objective is "go and talk
        // to someone" teaches the player that bars mean nothing.
        if (_progressRT != null)
        {
            bool stepped = target > 1;
            if (_progressRT.gameObject.activeSelf != stepped) _progressRT.gameObject.SetActive(stepped);
            if (stepped && _progressFill != null)
                _progressFill.fillAmount = Mathf.Clamp01(current / (float)target);
        }
    }

    public void CompleteMission()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (titleText != null) titleText.text = $"<s>{titleText.text}</s>";
        if (descriptionText != null) descriptionText.text = $"{baseDescription} <color=#00FF00>({LocalizationManager.Tr("MISSION_DONE_TAG")})</color>";

        StartCoroutine(CompleteAnimationRoutine());
    }

    public void SetCompletedStateInstant()
    {
        isCompleted = true;
        _setupCalled = true;
        if (titleText != null) titleText.text = $"<s>{titleText.text}</s>";
        if (descriptionText != null) descriptionText.text = $"{baseDescription} <color=#00FF00>({LocalizationManager.Tr("MISSION_DONE_TAG")})</color>";

        EnsureFurniture();
        if (_accent != null) _accent.color = AccentDone;
        if (_tick != null) { _tick.gameObject.SetActive(true); _tick.rectTransform.localScale = Vector3.one; }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    // The completion beat. Four things at once, because a single white flash
    // reads as a rendering hiccup rather than as an achievement: the accent
    // turns green, a tick pops in, the whole plate gives a small nudge, and the
    // background sweeps back from a bright flash. Together they are legible in
    // peripheral vision, which is where this plate lives.
    private IEnumerator CompleteAnimationRoutine()
    {
        EnsureFurniture();
        if (_progressRT != null && _progressFill != null && _progressRT.gameObject.activeSelf)
            _progressFill.fillAmount = 1f;
        if (_accent != null) _accent.color = AccentDone;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(AudioID.UI_QuestComplete);

        Transform visual = backgroundImage != null ? backgroundImage.transform : transform;
        Vector3 restPos = visual.localPosition;
        Color originalColor = backgroundImage != null ? backgroundImage.color : Color.white;
        if (backgroundImage != null) backgroundImage.color = Color.white;

        if (_tick != null)
        {
            _tick.gameObject.SetActive(true);
            _tick.rectTransform.localScale = Vector3.zero;
        }

        float t = 0f;
        const float beat = 0.5f;
        while (t < beat)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / beat);

            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(Color.white, originalColor, k);

            // The tick overshoots and settles — the same easing the reward
            // reveal uses, so completions across the game feel like one game.
            if (_tick != null)
            {
                float pop = k < 0.45f
                    ? Mathf.Lerp(0f, 1.25f, k / 0.45f)
                    : Mathf.Lerp(1.25f, 1f, (k - 0.45f) / 0.55f);
                _tick.rectTransform.localScale = Vector3.one * pop;
            }

            // A nudge to the right and back: the plate reacts physically to
            // being ticked off instead of only changing colour.
            visual.localPosition = restPos + new Vector3(Mathf.Sin(k * Mathf.PI) * 12f, 0f, 0f);
            yield return null;
        }

        visual.localPosition = restPos;
        if (backgroundImage != null) backgroundImage.color = originalColor;
        if (_tick != null) _tick.rectTransform.localScale = Vector3.one;
    }
}