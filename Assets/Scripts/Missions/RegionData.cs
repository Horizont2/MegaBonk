using UnityEngine;
using System.Collections.Generic;

public enum RegionState { Locked, Available, Conquered }

[CreateAssetMenu(fileName = "NewRegion", menuName = "Map/Region Data")]
public class RegionData : ScriptableObject
{
    [Header("Lore & UI")]
    public int regionID;
    public string regionName = "New Territory";
    [TextArea(3, 5)] public string loreDescription;
    public Sprite regionIllustration;

    [Header("Map Logic")]
    public RegionState currentState = RegionState.Locked;
    public List<RegionData> neighboringRegions;
    [HideInInspector] public bool isNewlyUnlocked = false;

    [Header("Difficulty & Combat System")]
    public int recommendedPower = 150; // ÒÅÏÅĞ ÖÅ ªÄÈÍÀ ÇÌ²ÍÍÀ ÑÈËÈ ĞÅÃ²ÎÍÓ
    public float enemyHpMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;

    [Header("One-Time Rewards (Çà ïğîõîäæåííÿ)")]
    public int woodReward = 100;
    public int stoneReward = 50;
    public int foodReward = 20;
    public int diamondReward = 5;

    [Header("Passive Income Yield (Çà õâèëèíó)")]
    public int passiveWood = 0;
    public int passiveStone = 0;
    public int passiveFood = 0;
    public int passiveDiamonds = 0;
}