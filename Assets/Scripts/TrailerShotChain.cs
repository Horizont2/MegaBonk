using System.Collections;
using UnityEngine;

// Plays the trailer's shots back to back.
//
// ==== WHY NOTHING IS EVER DEACTIVATED ====
//
// The obvious way to run one shot at a time is to switch each rig on for its turn
// and off again afterwards. That is what this did, and it is why the transition
// froze: shot 2's rig contains a 500-metre Terrain with hundreds of trees and a
// detail-grass layer, and ACTIVATING a terrain makes Unity rebuild its tree and
// detail render data synchronously. A hitch of that size, landing exactly on the
// cut, is indistinguishable from a hang — and it happened every single time,
// which is why cutting the unit count never helped.
//
// So the rigs are all switched on once, at the start, and stay on. What changes
// between shots is only which CAMERA renders, which AUDIO LISTENER hears, and
// which LIGHTS are lit. The sets sit 2 km apart, so the inactive one is outside
// the live camera's frustum and its far plane: it costs nothing to leave standing.
//
// ==== TWO THINGS IT DELIBERATELY DOES NOT DO ====
//
//   It does not wait a FIXED TIME per shot. Every shot reports when it is done. A
//   hard-coded delay is right exactly once — retune any beat and the hand-over
//   drifts, showing up as a black gap or a cut landing mid-flare rather than as
//   an obviously wrong number.
//
//   It does not fade on the seam. Shot 1 already ends in black and shot 2 opens
//   from black; a third fade would make a hole. Only a whoosh goes on the join,
//   which is what stops a cut between two quiet shots sounding like a dropout.
[DisallowMultipleComponent]
public class TrailerShotChain : MonoBehaviour
{
    [Tooltip("The shot rigs, in playing order. All of them stay ACTIVE for the whole trailer; only their cameras, listeners and lights are switched.")]
    public GameObject[] shotRigs;

    [Tooltip("Seconds of black held on the seam. Long enough to read as a cut, short enough not to read as a stall.")]
    public float seamHold = 0.25f;

    [Tooltip("Air movement across the join. A hard cut between two quiet shots sounds like a dropout without it.")]
    public string seamWhoosh = AudioID.Trailer_Whoosh;

    [Tooltip("Safety net. If a shot never reports finishing, the chain moves on rather than stopping the trailer dead.")]
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
        // Everything on, everything silent and dark. Doing this once, up front,
        // means no shot ever pays an activation cost in the middle of the trailer.
        foreach (var rig in shotRigs)
        {
            if (rig == null) continue;
            rig.SetActive(true);
            SetRigLive(rig, false);
        }

        // Let every Start() in the scene run, then give the shots a frame to
        // prepare themselves before the first one is asked to play.
        yield return null;

        foreach (var rig in shotRigs)
        {
            if (rig == null) continue;
            var legion = rig.GetComponentInChildren<TrailerLegionMarch>(true);
            if (legion != null) legion.Prepare();
        }

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

            // Hand the frame and the sound to this shot, and take them from the
            // one before it. No object is created or destroyed here — that is the
            // whole point.
            for (int j = 0; j < shotRigs.Length; j++)
                SetRigLive(shotRigs[j], j == i);

            TrailerSceneSanity.ClearTheField(rig.transform);

            yield return RunShot(rig);
        }

        Debug.Log("[TrailerChain] Sequence complete.");
        TrailerLogGuard.Disarm();
    }

    // Seen and heard, or not. Lights are included: two rigs standing at once
    // means two directional lights, and the one belonging to the shot that is not
    // playing would still be lighting the one that is.
    private static void SetRigLive(GameObject rig, bool live)
    {
        if (rig == null) return;

        foreach (var cam in rig.GetComponentsInChildren<Camera>(true)) cam.enabled = live;
        foreach (var lis in rig.GetComponentsInChildren<AudioListener>(true)) lis.enabled = live;
        foreach (var lgt in rig.GetComponentsInChildren<Light>(true)) lgt.enabled = live;
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
