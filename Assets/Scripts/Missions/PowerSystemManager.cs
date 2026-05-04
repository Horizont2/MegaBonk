using UnityEngine;

public class PowerSystemManager : MonoBehaviour
{
    public static PowerSystemManager Instance;

    [Header("Databases")]
    public HeroData[] allHeroes;
    public WeaponData[] allWeapons;

    [Header("Power Weights (Баланс)")]
    public float hpWeight = 0.5f;     // Скільки сили дає 1 ХП
    public float damageWeight = 5f;   // Скільки сили дає 1 шкоди
    public float forgeBonusPower = 20f; // Сила за кожен рівень Кузні

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int CalculatePlayerPower()
    {
        int power = 0;

        // 1. Герой
        int heroID = PlayerPrefs.GetInt("SelectedHeroID", 0);
        if (heroID < allHeroes.Length && allHeroes[heroID] != null)
        {
            power += Mathf.RoundToInt(allHeroes[heroID].actualMaxHealth * hpWeight);
        }

        // 2. Зброя
        int weaponID = PlayerPrefs.GetInt("SelectedWeaponID", 0);
        if (weaponID < allWeapons.Length && allWeapons[weaponID] != null)
        {
            WeaponData wep = allWeapons[weaponID];
            power += Mathf.RoundToInt(wep.damageBonus * damageWeight);
            power += Mathf.RoundToInt(wep.critChance * 100f);
        }

        // 3. Мета-прогресія
        int metaDmgLvl = PlayerPrefs.GetInt("UpgradeLevel_MetaDamage", 0);
        int metaHpLvl = PlayerPrefs.GetInt("UpgradeLevel_MetaHealth", 0);
        power += (metaDmgLvl * 15) + (metaHpLvl * 10);

        // 4. Кузня
        int forgeLevel = PlayerPrefs.GetInt("SaveBld_Forge", 0);
        power += Mathf.RoundToInt(forgeLevel * forgeBonusPower);

        return power;
    }
}