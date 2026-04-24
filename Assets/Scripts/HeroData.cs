using UnityEngine;

// Цей рядок додає нове меню в Unity, щоб ми могли створювати героїв кліком мишки
[CreateAssetMenu(fileName = "New Hero", menuName = "Shop/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Basic Info")]
    public string heroName = "Unknown Hero";
    public int price = 150;

    [Tooltip("ID героя для збереження (має бути унікальним, наприклад: 0, 1, 2)")]
    public int heroID;

    [Header("Visuals")]
    [Tooltip("Префаб моделі тільки для вітрини (бажано без скриптів ворогів, просто з аніматором Idle)")]
    public GameObject shopModelPrefab;

    [Header("UI Stats (Від 0 до 1 для заповнення смужок)")]
    [Range(0f, 1f)] public float hpBarFill = 0.5f;
    [Range(0f, 1f)] public float speedBarFill = 0.5f;
    [Range(0f, 1f)] public float radiusBarFill = 0.5f;

    [Header("Real Gameplay Stats")]
    public float actualMaxHealth = 1000f;
    public float actualMoveSpeed = 5.5f;
    public float actualBombRadius = 9f;
}