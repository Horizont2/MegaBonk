using UnityEngine;

// Типы миссий. Позже мы сможем легко добавлять новые (например, Убить Босса)
public enum MissionType
{
    KillEnemies,
    CollectCrystals,
    SurviveSeconds,    // Продержаться определенное время
    BuildStructures    // Построить тактические барикады в бою
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Megabonk/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Details")]
    public string missionName = "Новая Миссия";

    [TextArea]
    public string missionDescription = "Убить 50 скелетов";

    public MissionType missionType;

    [Header("Target Goal")]
    public int targetAmount = 50; // Сколько нужно убить/собрать/продержаться

    [Header("Rewards (Hub Resources)")]
    public int woodReward = 20;    // Награда деревом для Хаба
    public int metalReward = 5;    // Награда металлом для Хаба
    public int diamondReward = 10; // Бриллианты для покупки оружия
}