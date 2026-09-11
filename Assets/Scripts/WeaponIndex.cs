using UnityEngine;

// Every weapon in the game, reachable from anywhere.
//
// WeaponData assets live in Assets/ShopItems and the only thing holding a list
// of them is ShopManager.weapons — a serialized array on a component that exists
// solely in the shop scene. So a region has no way to ask "what weapons are
// there, and which does the player not own yet", which is exactly the question
// an Armoury cache has to answer before it can pay out in something better than
// a number.
//
// Built by an editor tool into Resources, same as EnemyAnimationSet, so nothing
// needs wiring per scene and it cannot go stale silently — the builder reports
// what it found.
[CreateAssetMenu(fileName = "WeaponIndex", menuName = "Shop/Weapon Index")]
public class WeaponIndex : ScriptableObject
{
    public const string ResourceName = "WeaponIndex";

    public WeaponData[] weapons;

    private static WeaponIndex _cached;
    private static bool _searched;

    public static WeaponIndex Load()
    {
        if (_searched) return _cached;
        _searched = true;
        _cached = Resources.Load<WeaponIndex>(ResourceName);
        return _cached;
    }

    public static void ClearCache() { _cached = null; _searched = false; }

    // Weapons the player has not bought yet. Free starter gear is excluded —
    // "you found a weapon you already own" is not a discovery.
    public static System.Collections.Generic.List<WeaponData> Unowned()
    {
        var result = new System.Collections.Generic.List<WeaponData>();
        var index = Load();
        if (index == null || index.weapons == null) return result;

        foreach (var w in index.weapons)
        {
            if (w == null || w.price <= 0) continue;
            if (PlayerPrefs.GetInt("WeaponUnlocked_" + w.weaponID, 0) == 1) continue;
            result.Add(w);
        }
        return result;
    }
}
