using UnityEngine;

[CreateAssetMenu(fileName = "New Hero", menuName = "Shop/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("Basic Info")]
    public string heroName = "Unknown Hero";
    public int price = 150;
    public int heroID;

    [Header("Power System (NEW)")]
    public int basePower = 50; // Базова сила героя для Мапи Регіонів

    [Header("Visuals")]
    public GameObject shopModelPrefab;

    [Header("UI Stats")]
    [Range(0f, 1f)] public float hpBarFill = 0.5f;
    [Range(0f, 1f)] public float speedBarFill = 0.5f;
    [Range(0f, 1f)] public float radiusBarFill = 0.5f;

    [Header("Real Gameplay Stats")]
    public float actualMaxHealth = 1000f;
    public float actualMoveSpeed = 5.5f;
    public float actualBombRadius = 9f;
}