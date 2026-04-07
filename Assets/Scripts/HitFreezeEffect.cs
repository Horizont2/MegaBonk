using UnityEngine;
using System.Collections;

/// <summary>
/// Micro-pause on enemy kill for impactful feel. Singleton, auto-creates itself.
/// Attach to any GameObject in scene, or it will be created automatically.
/// </summary>
public class HitFreezeEffect : MonoBehaviour
{
    public static HitFreezeEffect Instance { get; private set; }

    [Header("Freeze Settings")]
    public float freezeDuration = 0.04f;
    public float freezeTimeScale = 0.05f;

    private Coroutine freezeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Freeze()
    {
        // Don't freeze if game is already paused (level-up menu, etc.)
        if (Time.timeScale == 0f) return;

        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        Time.timeScale = freezeTimeScale;
        yield return new WaitForSecondsRealtime(freezeDuration);

        // Only restore if nothing else paused the game during the freeze
        if (Time.timeScale <= freezeTimeScale)
            Time.timeScale = 1f;

        freezeCoroutine = null;
    }
}
