using UnityEngine;
using System;
using System.Collections.Generic;

public class MapProgressionManager : MonoBehaviour
{
    public static MapProgressionManager Instance;

    [Header("All Regions Database")]
    public List<RegionData> allRegionsInGame;

    public static event Action OnMapStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ConquerRegionAndUnlockNeighbors(RegionData conqueredRegion)
    {
        if (conqueredRegion.currentState == RegionState.Conquered) return;

        conqueredRegion.currentState = RegionState.Conquered;

        int currentConquered = PlayerPrefs.GetInt("TotalConqueredRegions", 0);
        PlayerPrefs.SetInt("TotalConqueredRegions", currentConquered + 1);
        PlayerPrefs.Save();

        foreach (RegionData neighbor in conqueredRegion.neighboringRegions)
        {
            if (neighbor.currentState == RegionState.Locked)
            {
                neighbor.currentState = RegionState.Available;
                neighbor.isNewlyUnlocked = true;
            }
        }

        OnMapStateChanged?.Invoke();
    }

    public void RefreshMapState()
    {
        OnMapStateChanged?.Invoke();
    }
}