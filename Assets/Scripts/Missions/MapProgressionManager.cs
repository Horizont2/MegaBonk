using UnityEngine;
using System;
using System.Collections.Generic;

public class MapProgressionManager : MonoBehaviour
{
    public static MapProgressionManager Instance;

    [Header("All Regions Database")]
    public List<RegionData> allRegionsInGame;

    // Івент, який буде казати всім RegionUI на мапі "Оновіть свої кольори!"
    public static event Action OnMapStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Цю функцію ви будете викликати, коли гравець успішно повертається з рейду
    public void ConquerRegionAndUnlockNeighbors(RegionData conqueredRegion)
    {
        if (conqueredRegion.currentState == RegionState.Conquered) return;

        conqueredRegion.currentState = RegionState.Conquered;

        // --- НОВЕ: Збільшуємо лічильник завойованих регіонів для діалогів Еліаса ---
        int currentConquered = PlayerPrefs.GetInt("TotalConqueredRegions", 0);
        PlayerPrefs.SetInt("TotalConqueredRegions", currentConquered + 1);
        PlayerPrefs.Save();
        // -------------------------------------------------------------------------

        foreach (RegionData neighbor in conqueredRegion.neighboringRegions)
        {
            if (neighbor.currentState == RegionState.Locked)
            {
                neighbor.currentState = RegionState.Available;
                neighbor.isNewlyUnlocked = true; // Кажемо мапі, що це НОВА територія
            }
        }

        // ВИПРАВЛЕННЯ: Викликаємо івент, щоб попередити всі UI елементи про зміну стану на мапі
        OnMapStateChanged?.Invoke();
    }
}