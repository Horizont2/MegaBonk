using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Megabonk/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Forest of Rusty Blades";
    public int levelID = 1;

    [Header("Environment Generation")]
    // Сюда ты будешь перетаскивать префабы деревьев или надгробий из KayKit
    public GameObject[] environmentPrefabs;

    // Как много разрушаемых бочек/ящиков будет на уровне (откуда падает дерево)
    [Range(0f, 1f)]
    public float destructibleDensity = 0.4f;

    [Header("Enemy & Difficulty Settings")]
    public float enemyHpMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;

    // Шанс появления элитных врагов в броне (с которых будет падать металл)
    [Range(0f, 1f)]
    public float armoredEnemySpawnChance = 0.15f;

    [Header("Level Missions")]
    // Список заданий, которые появятся слева на экране при старте уровня
    public MissionData[] levelMissions;
}