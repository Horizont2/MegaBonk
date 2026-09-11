using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The moment a find is worth stopping for.
//
// A weapon or a piece of armour appearing as a line of text in the corner is
// indistinguishable from picking up a coin, and the player learns to ignore it.
// Rare drops need a beat: the game pauses on the thing, holds it in the middle of
// the screen where nobody can miss it, and makes light come off it.
//
// Built entirely in code and self-installing, for the same reason the trailer's
// overlay is: it has to work in any scene, from any system, with no prefab wired
// anywhere. Everything runs on UNSCALED time so it still plays if something has
// frozen the game — and something usually has, because a good drop moment is
// exactly where a hit-stop or a level-up screen wants to be.
[DisallowMultipleComponent]
public class RewardReveal : MonoBehaviour
{
    public static RewardReveal Instance { get; private set; }

    [Header("Timing")]
    public float riseTime = 0.45f;
    public float holdTime = 1.9f;
    public float fadeTime = 0.5f;

    [Header("Look")]
    [Tooltip("Icon size at rest, in reference pixels (1920x1080).")]
    public float iconSize = 240f;
    [Tooltip("How far past its final size the icon overshoots on the way in. A little is impact; a lot is a bouncy castle.")]
    public float overshoot = 1.14f;
    public float rayCount = 12f;
    public float raySpinSpeed = 18f;

    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _iconRT;
    private Image _icon;
    private Image _burst;
    private RectTransform _burstRT;
    private RectTransform _raysRT;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _subtitle;
    private Coroutine _playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Instance != null) return;
        var go = new GameObject("[RewardReveal]");
        DontDestroyOnLoad(go);
        go.AddComponent<RewardReveal>();
    }

    // The one entry point. Anything that hands the player something rare calls
    // this; it knows nothing about caches, armour or weapons.
    public static void Show(Sprite icon, string title, string subtitle, Color accent)
    {
        if (Instance == null) Install();
        if (Instance != null) Instance.Play(icon, title, subtitle, accent);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        _group.alpha = 0f;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ---- construction --------------------------------------------------------

    private void Build()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the HUD but below the letterbox overlay the trailer uses, so a
        // drop during a cinematic does not punch through the bars.
        _canvas.sortingOrder = 4000;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;   // never eat a click; this is not a dialog
        _group.interactable = false;

        // Layer order matters: rays behind the soft burst, burst behind the icon.
        _raysRT = MakeChild("Rays", 0f).GetComponent<RectTransform>();
        var rays = _raysRT.gameObject.AddComponent<Image>();
        rays.sprite = BuildRaySprite();
        rays.raycastTarget = false;
        _raysRT.sizeDelta = new Vector2(iconSize * 4.2f, iconSize * 4.2f);

        _burstRT = MakeChild("Burst", 0f).GetComponent<RectTransform>();
        _burst = _burstRT.gameObject.AddComponent<Image>();
        _burst.sprite = BuildGlowSprite();
        _burst.raycastTarget = false;
        _burstRT.sizeDelta = new Vector2(iconSize * 2.6f, iconSize * 2.6f);

        _iconRT = MakeChild("Icon", 0f).GetComponent<RectTransform>();
        _icon = _iconRT.gameObject.AddComponent<Image>();
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        _iconRT.sizeDelta = new Vector2(iconSize, iconSize);

        _title = MakeText("Title", -iconSize * 0.78f, 54f, FontStyles.Bold);
        _subtitle = MakeText("Subtitle", -iconSize * 0.78f - 52f, 30f, FontStyles.Normal);
    }

    private GameObject MakeChild(string name, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        return go;
    }

    private TextMeshProUGUI MakeText(string name, float y, float size, FontStyles style)
    {
        var go = MakeChild(name, y);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1200f, size * 1.6f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        return t;
    }

    // A radial starburst, drawn once into a texture. Generated rather than
    // authored so there is no art dependency and nothing to wire.
    private static Sprite BuildRaySprite()
    {
        const int S = 256;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[S * S];
        Vector2 c = new Vector2(S * 0.5f, S * 0.5f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude / (S * 0.5f);
                float ang = Mathf.Atan2(d.y, d.x);
                // Alternating wedges, softened at the tips and hollow in the
                // middle so the icon is never sitting on a bright disc.
                float wedge = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 6f)), 8f);
                float radial = Mathf.Clamp01(1f - r) * Mathf.Clamp01((r - 0.22f) * 4f);
                px[y * S + x] = new Color(1f, 1f, 1f, wedge * radial * 0.85f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }

    // A soft round falloff — the same trick TrailerSoftSprite uses to stop
    // particles rendering as hard squares.
    private static Sprite BuildGlowSprite()
    {
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[S * S];
        Vector2 c = new Vector2(S * 0.5f, S * 0.5f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), c) / (S * 0.5f);
                float a = Mathf.Clamp01(1f - r);
                px[y * S + x] = new Color(1f, 1f, 1f, a * a * a * 0.7f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }

    // ---- the beat ------------------------------------------------------------

    private void Play(Sprite icon, string title, string subtitle, Color accent)
    {
        if (_playing != null) StopCoroutine(_playing);
        _playing = StartCoroutine(Routine(icon, title, subtitle, accent));
    }

    private IEnumerator Routine(Sprite icon, string title, string subtitle, Color accent)
    {
        _icon.sprite = icon;
        // No icon is not a reason to show nothing — the name still matters, and
        // a missing sprite would otherwise render as a white box.
        _icon.enabled = icon != null;
        _icon.color = Color.white;

        _title.text = title ?? "";
        _title.color = accent;
        _subtitle.text = subtitle ?? "";
        _subtitle.color = new Color(0.85f, 0.85f, 0.85f);

        Color glow = accent; glow.a = 1f;
        _burst.color = glow;
        _raysRT.GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.55f);

        if (AudioManager.Instance != null && AudioManager.Instance.HasEvent(AudioID.UI_QuestComplete))
            AudioManager.Instance.PlaySFX(AudioID.UI_QuestComplete);

        // RISE. The icon overshoots slightly and settles; the rays expand from
        // nothing. Unscaled throughout — see the class note.
        float t = 0f;
        while (t < riseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / riseTime);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            _group.alpha = eased;
            float s = Mathf.LerpUnclamped(0.55f, overshoot, eased);
            // Settle back from the overshoot over the last third of the rise.
            if (k > 0.66f) s = Mathf.Lerp(overshoot, 1f, (k - 0.66f) / 0.34f);
            _iconRT.localScale = Vector3.one * s;
            _burstRT.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, eased);
            _raysRT.localScale = Vector3.one * Mathf.LerpUnclamped(0.1f, 1f, eased);
            Spin();
            yield return null;
        }
        _iconRT.localScale = Vector3.one;

        // HOLD. The rays keep turning and the glow breathes, so the frame is
        // never static — a still image reads as the game having hung.
        t = 0f;
        while (t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            float breathe = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.045f;
            _burstRT.localScale = Vector3.one * breathe;
            _iconRT.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.015f);
            Spin();
            yield return null;
        }

        // FADE, drifting up a little so it leaves rather than switches off.
        t = 0f;
        Vector2 from = _iconRT.anchoredPosition;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            _group.alpha = 1f - k;
            _iconRT.anchoredPosition = from + Vector2.up * (k * 40f);
            Spin();
            yield return null;
        }

        _group.alpha = 0f;
        _iconRT.anchoredPosition = from;
        _playing = null;
    }

    private void Spin()
    {
        _raysRT.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * raySpinSpeed);
    }
}
