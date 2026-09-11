using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The "hold this position" readout.
//
// A channelled interaction is invisible without one: the player is standing
// still while a fight happens around them, and if nothing on screen says how
// long is left, standing still reads as being stuck rather than as doing
// something. The bar IS the mechanic — it is what turns "wait here" into a
// decision the player can time.
//
// Built in code and self-installing for the same reason RewardReveal is: it has
// to work from any scene with nothing wired. Deliberately small and low on the
// screen so it never covers the thing the player is watching, which during a
// channel is the enemies converging on them.
[DisallowMultipleComponent]
public class ChannelBar : MonoBehaviour
{
    public static ChannelBar Instance { get; private set; }

    private CanvasGroup _group;
    private Image _fill;
    private Image _track;
    private TextMeshProUGUI _label;
    private float _shownUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Instance != null) return;
        var go = new GameObject("[ChannelBar]");
        DontDestroyOnLoad(go);
        go.AddComponent<ChannelBar>();
    }

    // Call every frame while channelling; it hides itself a moment after the
    // calls stop, so no caller has to remember to switch it off on every exit
    // path — and there are several: finishing, walking away, dying.
    public static void Show(string label, float progress01, Color accent)
    {
        if (Instance == null) Install();
        if (Instance != null) Instance.Set(label, progress01, accent);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        _group.alpha = 0f;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3900;   // under RewardReveal, over the HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        var root = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 210f);
        root.sizeDelta = new Vector2(420f, 14f);

        _track = root.gameObject.AddComponent<Image>();
        _track.color = new Color(0f, 0f, 0f, 0.55f);
        _track.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
        fillGo.SetParent(root, false);
        fillGo.anchorMin = new Vector2(0f, 0f);
        fillGo.anchorMax = new Vector2(1f, 1f);
        fillGo.offsetMin = new Vector2(2f, 2f);
        fillGo.offsetMax = new Vector2(-2f, -2f);
        _fill = fillGo.gameObject.AddComponent<Image>();
        _fill.raycastTarget = false;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        // A plain white sprite so Filled has something to cut. Image with a null
        // sprite ignores fillAmount entirely, which would show a full bar from
        // the first frame — the exact wrong lie for a progress readout.
        _fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        var labelGo = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
        labelGo.SetParent(root, false);
        labelGo.anchorMin = new Vector2(0f, 1f);
        labelGo.anchorMax = new Vector2(1f, 1f);
        labelGo.pivot = new Vector2(0.5f, 0f);
        labelGo.anchoredPosition = new Vector2(0f, 8f);
        labelGo.sizeDelta = new Vector2(0f, 34f);
        _label = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontSize = 26f;
        _label.raycastTarget = false;
    }

    private void Set(string label, float progress01, Color accent)
    {
        _label.text = label;
        _label.color = accent;
        _fill.color = accent;
        _fill.fillAmount = Mathf.Clamp01(progress01);
        _group.alpha = 1f;
        _shownUntil = Time.unscaledTime + 0.15f;
    }

    private void Update()
    {
        if (_group.alpha <= 0f) return;
        if (Time.unscaledTime < _shownUntil) return;
        _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime * 5f);
    }
}
