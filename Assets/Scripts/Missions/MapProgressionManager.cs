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
        if (conqueredRegion.currentState == RegionState.Conquered) return; // Вже захоплено

        // 1. Захоплюємо сам регіон
        conqueredRegion.currentState = RegionState.Conquered;
        Debug.Log($"[Map] Захоплено регіон: {conqueredRegion.regionName}!");

        // 2. Відкриваємо кордони сусідів (знімаємо замки)
        foreach (RegionData neighbor in conqueredRegion.neighboringRegions)
        {
            if (neighbor.currentState == RegionState.Locked)
            {
                neighbor.currentState = RegionState.Available;
                Debug.Log($"[Map] Відкрито новий шлях до: {neighbor.regionName}");
            }
        }

        // 3. Даємо команду всій мапі оновити візуал
        OnMapStateChanged?.Invoke();
    }

    // ТЕСТОВА КНОПКА (видаліть пізніше): 
    // Натисніть пробіл в грі, щоб зімітувати перемогу в регіоні 1
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ConquerRegionAndUnlockNeighbors(allRegionsInGame[0]);
        }
    }
}