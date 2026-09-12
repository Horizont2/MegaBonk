using UnityEditor;
using UnityEngine;

// A switch for the enemy animation-variation layer, and a way to see what it is
// doing.
//
// That layer rewrites which clip every enemy state plays. When enemy animation
// looks wrong it is the first thing worth ruling out, and needing a code change
// to rule it out is not acceptable — so it lives behind a menu toggle that takes
// effect on the next spawn.
public static class EnemyPersonalityToggle
{
    private const string Key = "EnemyPersonality";
    private const string LogKey = "EnemyPersonalityLog";

    [MenuItem("Tools/Enemies/Enemy Animation Variation", priority = 30)]
    private static void Toggle() => PlayerPrefs.SetInt(Key, On ? 0 : 1);

    [MenuItem("Tools/Enemies/Enemy Animation Variation", true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/Enemies/Enemy Animation Variation", On);
        return true;
    }

    [MenuItem("Tools/Enemies/Log Clip Mapping On Spawn", priority = 31)]
    private static void ToggleLog()
    {
        PlayerPrefs.SetInt(LogKey, Logging ? 0 : 1);
        Debug.Log($"[Personality] Mapping log {(Logging ? "ON" : "off")}. It prints, per enemy, which controller " +
                  "clip each role resolved to — the fastest way to see why a state is playing the wrong animation.");
    }

    [MenuItem("Tools/Enemies/Log Clip Mapping On Spawn", true)]
    private static bool ToggleLogValidate()
    {
        Menu.SetChecked("Tools/Enemies/Log Clip Mapping On Spawn", Logging);
        return true;
    }

    private static bool On => PlayerPrefs.GetInt(Key, 1) == 1;
    private static bool Logging => PlayerPrefs.GetInt(LogKey, 0) == 1;
}
