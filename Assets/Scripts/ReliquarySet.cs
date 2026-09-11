using UnityEngine;

// The prefabs a reliquary is built from, reachable at runtime.
//
// Same reason as WeaponIndex and EnemyAnimationSet: these live all over
// Assets/, AssetDatabase does not exist in a build, and a component created from
// nothing at runtime has no inspector for anyone to wire. An editor tool collects
// them here once and the game loads one asset.
[CreateAssetMenu(fileName = "ReliquarySet", menuName = "Exploration/Reliquary Set")]
public class ReliquarySet : ScriptableObject
{
    public const string ResourceName = "ReliquarySet";

    [Tooltip("Must carry a LootChest. This is the thing at the centre.")]
    public GameObject chestPrefab;

    [Tooltip("The landmark. Tall, coloured, and the one prop that reads across broken terrain.")]
    public GameObject[] banners;

    [Tooltip("Ring stones — what makes it legible as a shrine rather than as scenery.")]
    public GameObject[] runeStones;

    [Tooltip("Bones and skulls, legendary sites only.")]
    public GameObject[] remains;

    public GameObject archPrefab;
    public GameObject lanternPrefab;

    private static ReliquarySet _cached;
    private static bool _searched;

    public static ReliquarySet Load()
    {
        if (_searched) return _cached;
        _searched = true;
        _cached = Resources.Load<ReliquarySet>(ResourceName);
        if (_cached == null)
            Debug.LogWarning($"[Reliquary] No '{ResourceName}' in a Resources folder — no reliquaries will be placed. " +
                             "Build it with Tools > Exploration > Build Reliquary Set.");
        return _cached;
    }

    public static void ClearCache() { _cached = null; _searched = false; }

    public bool IsUsable => chestPrefab != null;

    public GameObject PickBanner() => Pick(banners);
    public GameObject PickRuneStone() => Pick(runeStones);
    public GameObject PickRemains() => Pick(remains);

    private static GameObject Pick(GameObject[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        for (int i = 0; i < 4; i++)
        {
            var g = pool[Random.Range(0, pool.Length)];
            if (g != null) return g;
        }
        foreach (var g in pool) if (g != null) return g;
        return null;
    }
}
