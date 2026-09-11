using UnityEngine;

// Every animation an enemy can wear, sorted by the job it does rather than by
// which file it came from.
//
// It exists because clips live inside FBX assets, and an FBX sub-asset cannot be
// reached at runtime — AssetDatabase is editor-only. So an editor tool collects
// them once into this object, it is saved under Resources, and the game loads it
// with one call. Nothing has to be wired onto a prefab by hand, which matters
// because there are nine enemy prefabs and a wiring step that has to be repeated
// nine times is a wiring step that will be wrong on at least one of them.
//
// Arrays rather than single fields, everywhere. The whole point of this work is
// that two skeletons standing next to each other should not be the same skeleton,
// and that starts with there being more than one clip to choose from.
[CreateAssetMenu(fileName = "EnemyAnimationSet", menuName = "Enemies/Animation Set")]
public class EnemyAnimationSet : ScriptableObject
{
    public const string ResourceName = "EnemyAnimationSet";

    [Header("Standing about")]
    public AnimationClip[] idles;
    [Tooltip("Things to do while standing: poking a fire, picking something up. Used sparingly — an idle everyone is busy in reads as a workshop, not a war camp.")]
    public AnimationClip[] idleBusiness;

    [Header("Getting somewhere")]
    [Tooltip("Unhurried. Patrols, walking back to a post, anything that is not a chase.")]
    public AnimationClip[] walks;
    [Tooltip("Hunting the player.")]
    public AnimationClip[] runs;
    [Tooltip("Bow-carrying run, so an archer does not sprint with its arms swinging through the bow.")]
    public AnimationClip runHoldingBow;

    [Header("Violence")]
    public AnimationClip[] attacksUnarmed;
    public AnimationClip[] attacks1H;
    public AnimationClip[] attacks2H;
    public AnimationClip[] attacksDualWield;
    public AnimationClip[] spellcasts;
    public AnimationClip summon;
    public AnimationClip[] bow;

    [Header("Consequences")]
    public AnimationClip[] hits;
    public AnimationClip[] deaths;
    public AnimationClip blockHit;

    [Header("Skeleton-specific (KayKit Rig_Medium_Special)")]
    [Tooltip("Rising out of the ground. What the ground-ambush spawn has been faking by sliding the transform upward.")]
    public AnimationClip spawnGround;
    public AnimationClip awakenStanding;
    public AnimationClip awakenFloor;
    [Tooltip("The boss's roar, and an enrage.")]
    public AnimationClip[] taunts;
    public AnimationClip resurrect;

    [Header("Boss (Rig_Large)")]
    public AnimationClip[] bossAttacks;
    public AnimationClip bossSlam;

    // ---- loading ------------------------------------------------------------

    private static EnemyAnimationSet _cached;
    private static bool _searched;

    public static EnemyAnimationSet Load()
    {
        if (_searched) return _cached;
        _searched = true;
        _cached = Resources.Load<EnemyAnimationSet>(ResourceName);
        if (_cached == null)
        {
            Debug.LogWarning($"[EnemyAnim] No '{ResourceName}' in a Resources folder — enemies will all share the " +
                             "one animation they have now. Build it with Tools > Enemies > Build Animation Set.");
        }
        return _cached;
    }

    // Editor tooling rebuilds the asset in place; without this the game would keep
    // handing out the version it loaded before the rebuild.
    public static void ClearCache() { _cached = null; _searched = false; }

    public static AnimationClip Pick(AnimationClip[] pool, System.Random rng)
    {
        if (pool == null || pool.Length == 0) return null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var c = pool[rng.Next(pool.Length)];
            if (c != null) return c;
        }
        foreach (var c in pool) if (c != null) return c;
        return null;
    }
}
