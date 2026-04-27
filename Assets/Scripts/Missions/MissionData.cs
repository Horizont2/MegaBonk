using UnityEngine;

public enum MissionType { KillEnemies, CollectCrystals, Survive, BuildStructures }

[CreateAssetMenu(fileName = "NewMission", menuName = "Megabonk/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Details")]
    public string missionName = "New Mission";
    [TextArea] public string missionDescription = "Mission description...";
    public MissionType missionType;

    [Header("Target Goal")]
    public int targetAmount = 50;

    [Header("Rewards (Hub Resources)")]
    public int woodReward = 20;
    public int stoneReward = 10; // Замість металу
    public int foodReward = 5;   // Додано їжу
    public int diamondReward = 10;
}