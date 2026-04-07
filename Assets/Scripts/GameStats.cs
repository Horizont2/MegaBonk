using UnityEngine;

/// <summary>
/// Static container for per-run statistics. Reset at the start of each run.
/// </summary>
public static class GameStats
{
    public static int totalKills;
    public static int bossKills;
    public static float totalDamageDealt;
    public static float totalDamageTaken;
    public static int highestLevel;
    public static int crystalsCollected;

    public static void Reset()
    {
        totalKills = 0;
        bossKills = 0;
        totalDamageDealt = 0f;
        totalDamageTaken = 0f;
        highestLevel = 1;
        crystalsCollected = 0;
    }
}
