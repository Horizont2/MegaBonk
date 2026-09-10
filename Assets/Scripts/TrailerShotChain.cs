using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays the trailer's shots back to back.
//
// Each shot is a self-contained rig with its own camera, lighting, fog and set,
// so the sequencer's whole job is to have exactly ONE of them switched on at a
// time and to hand over at the right moment.
//
// Two things it deliberately does not do:
//
//   It does not wait a FIXED TIME per shot. Every shot reports when it is done,
//   because a hard-coded delay is correct exactly once — the first time any beat
//   inside a shot is retuned, the hand-over drifts, and the symptom is a black
//   gap or a cut landing mid-flare rather than an obviously wrong number.
//
//   It does not fade between shots. Shot 1 already ends in black and shot 2
//   already opens from black, so the join is covered by the shots themselves.
//   Adding a fade here would double it and produce a hole in the middle of the
//   trailer. All that is added on the seam is a whoosh, which is what stops a cut
//   between two silences sounding like a mistake.
[DisallowMultipleComponent]
public class TrailerShotChain : MonoBehaviour
{
    [Tooltip("The shot rigs, in the order they play. Each is switched on for its turn and off again afterwards, so only one camera, listener and set is ever live.")]
    public GameObject[] shotRigs;

    [Tooltip("Seconds of black held on the seam. Small — long enough to read as a cut, short enough not to read as a stall.")]
    public float seamHold = 0.25f;

    [Tooltip("Air movement across the join. A hard cut between two quiet shots sounds like a dropout without it.")]
    public string seamWhoosh = AudioID.Trailer_Whoosh;

    [Tooltip("Safety net. If a shot never reports finishing — an exception inside it, a component switched off — the chain moves on anyway rather than stopping the trailer dead.")]
    public float maxSecondsPerShot = 40f;

    private void Start()
    {
        if (shotRigs == null || shotRigs.Length == 0)
        {
            Debug.LogWarning("[TrailerChain] No shot rigs assigned.");
            enabled = false;
            return;
        }
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // Everything off first. Two rigs live at once means two cameras tagged
        // MainCamera and two AudioListeners, and Unity picks between them
        // arbitrarily — which looks like the wrong shot playing rather than like
        // a setup mistake.
        foreach (var rig in shotRigs)
            if (rig != null) rig.SetActive(false);

        for (int i = 0; i < shotRigs.Length; i++)
        {
            GameObject rig = shotRigs[i];
            if (rig == null) continue;

            if (i > 0)
            {
                TrailerAudio.SilenceStaleBeds();
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(seamWhoosh))
                    AudioManager.Instance.PlaySFX(seamWhoosh);
                yield return new WaitForSecondsRealtime(seamHold);
            }

            SetRigLive(rig, true);
            // A frame for Start() on everything inside the rig to run before we
            // go looking for the director and ask it to play.
            yield return null;

            // Nothing else may be driving cameras, audio or animators while a shot
            // plays. In a clean trailer scene this finds nothing and costs a scan.
            TrailerSceneSanity.ClearTheField(rig.transform);

            // Wake the NEXT shot now, silent and blind, so it can assemble itself
            // while this one plays.
            //
            // Building it after the cut instead meant the audience sat through
            // several seconds of black while a few hundred prefabs were
            // instantiated. Hidden behind a fade is not the same as not being
            // there, and a black hole in the middle of a trailer is the most
            // expensive kind of dead air.
            if (i + 1 < shotRigs.Length) PrewarmNext(shotRigs[i + 1]);

            yield return RunShot(rig);

            rig.SetActive(false);
        }

        Debug.Log("[TrailerChain] Sequence complete.");
        TrailerLogGuard.Disarm();
    }

    // A rig that is ON but neither seen nor heard: its objects exist and its
    // scripts run, but its camera and listener stay off so the shot that is
    // actually playing keeps the frame and the audio to itself.
    private static void PrewarmNext(GameObject rig)
    {
        if (rig == null || rig.activeSelf) return;

        rig.SetActive(true);
        SetRigLive(rig, false);

        var legion = rig.GetComponentInChildren<TrailerLegionMarch>(true);
        if (legion != null) legion.Prepare();
    }

    private static void SetRigLive(GameObject rig, bool live)
    {
        if (rig == null) return;
        if (live && !rig.activeSelf) rig.SetActive(true);

        foreach (var cam in rig.GetComponentsInChildren<Camera>(true)) cam.enabled = live;
        foreach (var lis in rig.GetComponentsInChildren<AudioListener>(true)) lis.enabled = live;
    }

    private IEnumerator RunShot(GameObject rig)
    {
        var statue = rig.GetComponentInChildren<TrailerStatueShot>(true);
        var legion = rig.GetComponentInChildren<TrailerLegionMarch>(true);

        if (statue != null) statue.Play();
        else if (legion != null) legion.Play();
        else
        {
            Debug.LogWarning($"[TrailerChain] '{rig.name}' has no shot director on it — skipping.");
            yield break;
        }

        float deadline = Time.unscaledTime + maxSecondsPerShot;
        while (Time.unscaledTime < deadline)
        {
            if (statue != null && statue.IsFinished) yield break;
            if (legion != null && legion.IsFinished) yield break;
            yield return null;
        }

        Debug.LogWarning($"[TrailerChain] '{rig.name}' never reported finishing within {maxSecondsPerShot}s — moving on so the trailer does not stall here.");
    }
}
