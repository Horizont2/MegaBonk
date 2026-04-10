using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Animates menu elements on scene load: title drop, sequential button pop-in,
/// and a repeating shine sweep on the main button.
/// Assign all references in the Inspector.
/// </summary>
public class MenuAnimator : MonoBehaviour
{
    [Header("Title")]
    [Tooltip("The RectTransform of the title text (e.g. 'MEGABONK')")]
    public RectTransform titleRect;
    public CanvasGroup titleGroup;
    public float titleDropDuration = 0.7f;

    [Header("Subtitle")]
    public CanvasGroup subtitleGroup;

    [Header("Character Area")]
    [Tooltip("The character + hammer preview area")]
    public CanvasGroup characterGroup;
    public RectTransform characterRect;

    [Header("Buttons (in order of appearance)")]
    [Tooltip("Assign buttons in the order they should pop in: BONK, Continue, Achievements, Options, Quit")]
    public RectTransform[] buttonRects;
    public float buttonPopDelay = 0.1f;
    public float buttonPopDuration = 0.4f;

    [Header("Crystal Bar")]
    public CanvasGroup crystalBarGroup;
    public RectTransform crystalBarRect;

    [Header("Meta Upgrades Area")]
    public CanvasGroup upgradesGroup;
    public RectTransform upgradesRect;

    [Header("Shine Sweep (BONK button)")]
    [Tooltip("An Image child of the BONK button used as the shine overlay (white, low alpha, skewed)")]
    public RectTransform shineRect;
    public float shineSweepDuration = 0.6f;
    public float shineSweepInterval = 3.5f;

    [Header("Vignette / Background")]
    public CanvasGroup vignetteGroup;

    private void Start()
    {
        // Hide everything initially
        SetAlpha(titleGroup, 0f);
        SetAlpha(subtitleGroup, 0f);
        SetAlpha(characterGroup, 0f);
        SetAlpha(crystalBarGroup, 0f);
        SetAlpha(upgradesGroup, 0f);
        SetAlpha(vignetteGroup, 0f);

        if (titleRect != null) titleRect.localScale = Vector3.one * 1.5f;
        if (characterRect != null) characterRect.anchoredPosition += Vector2.down * 30f;

        foreach (RectTransform btn in buttonRects)
        {
            if (btn == null) continue;
            btn.localScale = Vector3.one * 0.6f;
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }

        if (crystalBarRect != null) crystalBarRect.anchoredPosition += Vector2.left * 40f;
        if (upgradesRect != null) upgradesRect.anchoredPosition += Vector2.down * 30f;

        StartCoroutine(PlayEntrySequence());
    }

    private IEnumerator PlayEntrySequence()
    {
        // 1. Vignette fade in
        yield return StartCoroutine(FadeGroup(vignetteGroup, 0f, 1f, 0.4f));

        // 2. Title drop (scale 1.5 -> 1.0 with overshoot + fade in)
        StartCoroutine(AnimateTitleDrop());
        yield return new WaitForSecondsRealtime(0.3f);

        // 3. Subtitle fade
        StartCoroutine(FadeGroup(subtitleGroup, 0f, 1f, 0.4f));
        yield return new WaitForSecondsRealtime(0.15f);

        // 4. Character area fade up
        StartCoroutine(FadeGroup(characterGroup, 0f, 1f, 0.5f));
        if (characterRect != null)
            StartCoroutine(SlideIn(characterRect, characterRect.anchoredPosition,
                characterRect.anchoredPosition + Vector2.up * 30f, 0.5f));
        yield return new WaitForSecondsRealtime(0.1f);

        // 5. Crystal bar slide in
        StartCoroutine(FadeGroup(crystalBarGroup, 0f, 1f, 0.35f));
        if (crystalBarRect != null)
            StartCoroutine(SlideIn(crystalBarRect, crystalBarRect.anchoredPosition,
                crystalBarRect.anchoredPosition + Vector2.right * 40f, 0.35f));
        yield return new WaitForSecondsRealtime(0.1f);

        // 6. Buttons pop in sequentially
        for (int i = 0; i < buttonRects.Length; i++)
        {
            if (buttonRects[i] == null) continue;
            StartCoroutine(PopInButton(buttonRects[i], buttonPopDuration));
            yield return new WaitForSecondsRealtime(buttonPopDelay);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // 7. Meta upgrades area
        StartCoroutine(FadeGroup(upgradesGroup, 0f, 1f, 0.5f));
        if (upgradesRect != null)
            StartCoroutine(SlideIn(upgradesRect, upgradesRect.anchoredPosition,
                upgradesRect.anchoredPosition + Vector2.up * 30f, 0.5f));

        // 8. Start repeating shine sweep on BONK button
        if (shineRect != null)
            StartCoroutine(ShineSweepLoop());
    }

    // ─── ANIMATION HELPERS ───

    private IEnumerator AnimateTitleDrop()
    {
        float t = 0f;
        Vector3 startScale = Vector3.one * 1.5f;
        while (t < titleDropDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / titleDropDuration);

            // Overshoot ease: goes past 1.0, then settles
            float overshoot = 1f + 0.15f * Mathf.Sin(p * Mathf.PI);
            float scale = Mathf.Lerp(1.5f, 1f, p) * (p < 0.8f ? overshoot : 1f);

            if (titleRect != null) titleRect.localScale = Vector3.one * scale;
            if (titleGroup != null) titleGroup.alpha = Mathf.Clamp01(p * 2.5f);
            yield return null;
        }
        if (titleRect != null) titleRect.localScale = Vector3.one;
        if (titleGroup != null) titleGroup.alpha = 1f;
    }

    private IEnumerator PopInButton(RectTransform btn, float duration)
    {
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        float t = 0f;
        Vector3 startScale = Vector3.one * 0.6f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);

            // Elastic ease out
            float elastic = 1f + Mathf.Pow(2f, -10f * p) * Mathf.Sin((p - 0.075f) * (2f * Mathf.PI) / 0.3f);
            btn.localScale = Vector3.one * elastic;

            if (cg != null) cg.alpha = Mathf.Clamp01(p * 3f);
            yield return null;
        }

        btn.localScale = Vector3.one;
        if (cg != null) cg.alpha = 1f;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator SlideIn(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease out cubic
            rect.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private IEnumerator ShineSweepLoop()
    {
        // The shineRect should be a child Image of the BONK button,
        // anchored to stretch full height, narrow width (~50% of button).
        // We animate its anchoredPosition.x from left to right.
        RectTransform parent = shineRect.parent as RectTransform;
        if (parent == null) yield break;

        float halfWidth = parent.rect.width * 0.5f;
        float shineHalf = shineRect.rect.width * 0.5f;
        float startX = -(halfWidth + shineHalf);
        float endX = halfWidth + shineHalf;

        while (true)
        {
            yield return new WaitForSecondsRealtime(shineSweepInterval);

            float t = 0f;
            while (t < shineSweepDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / shineSweepDuration);
                float x = Mathf.Lerp(startX, endX, p);
                shineRect.anchoredPosition = new Vector2(x, shineRect.anchoredPosition.y);
                yield return null;
            }
        }
    }

    // ─── UTILITY ───

    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }
}
