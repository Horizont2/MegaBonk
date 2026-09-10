using UnityEngine;

// Makes a scene safe for a cinematic to run in.
//
// The new trailer was being built into Trailer_Lvl_1, which already contained an
// ENTIRE earlier trailer: a master sequence director, nine undead-pursuit
// skeletons, a horse and rider, a foliage recolouring pass over 1406 renderers,
// rain, lightning, terrain seasons, and a second audio listener. All of it ran at
// the same time as the new shots.
//
// That single fact explains almost every symptom chased over the last few days:
// a horse galloping under every shot, thousands of console warnings a second from
// a component writing to a missing animator parameter, shot 1 never reporting
// that it had finished because another director was fighting it for the camera
// and the timescale, and the lag that looked like the new scene being too heavy.
//
// The real fix is to build the trailer in its own empty scene, and the tools do
// that now. This is the belt to that pair of braces: if a shot is ever played in
// a scene that has other things in it, it clears the field first rather than
// competing.
public static class TrailerSceneSanity
{
    // Everything from the earlier trailer. These are switched off, not destroyed:
    // the old shots may still be wanted, just not while another one is playing.
    private static readonly string[] LegacyDirectors =
    {
        "TrailerSequenceDirector", "TrailerRideEvent", "TrailerHorseRide",
        "TrailerUndeadPursuit", "TrailerCameraCutter", "TrailerCraneReveal",
        "TrailerSeasonRide", "TrailerTerrainSeasons", "TrailerRainFollow",
        "TrailerLightningBeat", "TrailerLightningStrike", "TrailerTimeRamp",
        "TrailerAmbience", "TrailerFovPush", "TrailerDoFFocus",
        "TrailerBreathVapor", "TrailerBattleDirector", "TrailerCastleReveal",
        "TrailerBirdFlush", "TrailerCutsceneAnim", "TrailerAnimatorHold",
        "TrailerFighter", "TrailerGroundClamp",
        // Gameplay systems that would spawn or drive things mid-shot.
        "EnemySpawner", "WorldEncounterDirector", "RegionAlertDirector",
        "EnemyEncounterGroup", "RegionTotem", "WorldGenerator",
    };

    public static void ClearTheField(Transform keepUnder)
    {
        int off = SilenceLegacy(keepUnder);
        int listeners = EnforceSingleListener(keepUnder);

        if (off > 0 || listeners > 0)
            Debug.Log($"[TrailerSanity] Disabled {off} competing director(s) and removed {listeners} extra audio listener(s). " +
                      $"For a clean run, build the trailer into its own scene — Tools ▸ Lore Trailer ▸ Setup FULL TRAILER does that.");
    }

    private static int SilenceLegacy(Transform keepUnder)
    {
        int n = 0;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mb == null || !mb.enabled) continue;
            if (keepUnder != null && mb.transform.IsChildOf(keepUnder)) continue;

            string name = mb.GetType().Name;
            for (int i = 0; i < LegacyDirectors.Length; i++)
            {
                if (name != LegacyDirectors[i]) continue;
                mb.enabled = false;
                n++;
                break;
            }
        }
        return n;
    }

    // Two listeners make Unity log a warning EVERY FRAME, which is hundreds of
    // console entries a minute on its own, and it picks between them arbitrarily
    // — so the shot can end up hearing the world from the wrong place.
    private static int EnforceSingleListener(Transform keepUnder)
    {
        var all = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (all.Length <= 1) return 0;

        AudioListener keep = null;
        foreach (var l in all)
        {
            if (l == null) continue;
            if (keepUnder != null && l.transform.IsChildOf(keepUnder)) { keep = l; break; }
        }
        if (keep == null) keep = all[0];

        int removed = 0;
        foreach (var l in all)
        {
            if (l == null || l == keep) continue;
            l.enabled = false;
            removed++;
        }
        return removed;
    }
}
