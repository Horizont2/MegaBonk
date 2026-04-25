using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public int weaponID;
    public string weaponName;
    public int price;

    [Header("Models")]
    public GameObject shopPrefab;   // Велика, деталізована модель для столу
    public GameObject inGamePrefab; // Модель, яка піде в руку лицарю (handslot.r)

    [Header("Stats")]
    public float damageBonus; // Наприклад: +10 до шкоди
    public float attackSpeed; // Наприклад: 1.2 (швидкість помаху)
    public float critChance;  // Наприклад: 0.15 (15% шанс криту)
}