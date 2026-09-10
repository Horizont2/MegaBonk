using UnityEngine;

// Kill any sound left running from a previous shot.
//
// The galloping horse was audible under every shot of the trailer. Its own
// controller stops the loop correctly when it is disabled — the problem is that a
// LOOPING sound is a handle held by the AudioManager, and parking a trailer rig
// only guarantees the component stops the loops IT started. Anything that began a
// bed and then had its object destroyed, or was started from a rig that was
// swapped out rather than disabled, keeps playing with nobody left holding the
// handle.
//
// So each shot begins by clearing the board. A cinematic should never inherit
// audio from whatever happened to be running before it, and the cost of being
// wrong in the other direction — a bed the shot wanted, cut a frame early — is
// nothing next to a horse galloping through a shot with no horse in it.
public static class TrailerAudio
{
    public static void SilenceStaleBeds()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.StopAllLoopedSFX();

        // Belt and braces: anything still driving a loop from a previous shot is
        // switched off, so it cannot start another one on its next Update.
        foreach (var h in Object.FindObjectsByType<HorseAudioController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (h != null) h.enabled = false;
    }
}
