using UnityEngine;
using System;
using System.Collections.Generic;

public class MapProgressionManager : MonoBehaviour
{
    public static MapProgressionManager Instance;

    [Header("All Regions Database")]
    public List<RegionData> allRegionsInGame;

    public static event Action OnMapStateChanged;

    // ����̲��ֲ�: ��������� ������ ��� PlayerPrefs, ��� �� ���������� ����� � ���'�� (Garbage)
    private Dictionary<int, string> regionStateKeys = new Dictionary<int, string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // �������� �� ����� ���� ���
        if (allRegionsInGame != null)
        {
            foreach (var region in allRegionsInGame)
            {
                if (region != null)
                    regionStateKeys[region.regionID] = "RegionState_" + region.regionID;
            }
        }

        SyncMapStatesWithSaves();
    }

    // Reads every region's saved state back onto its asset.
    //
    // This whole method used to be one bad entry away from doing nothing: a null
    // in allRegionsInGame threw on region.currentState, a region whose key was
    // never registered threw on the dictionary indexer, and a null in
    // neighboringRegions threw on neighbor.currentState. Any of those aborted the
    // loop, so every region AFTER the bad one silently kept its asset default —
    // and a capture late in the list, like the Sunken Outpost, never showed as
    // taken however many times it was won.
    public void SyncMapStatesWithSaves()
    {
        bool needsSave = false;
        if (allRegionsInGame == null) return;

        foreach (var region in allRegionsInGame)
        {
            if (region == null) continue;
            int defaultState = (int)region.currentState;
            int savedState = PlayerPrefs.GetInt(KeyFor(region), defaultState);
            region.currentState = (RegionState)savedState;
        }

        foreach (var region in allRegionsInGame)
        {
            if (region == null || region.currentState != RegionState.Conquered) continue;
            if (region.neighboringRegions == null) continue;

            foreach (RegionData neighbor in region.neighboringRegions)
            {
                if (neighbor == null || neighbor.currentState != RegionState.Locked) continue;

                neighbor.currentState = RegionState.Available;
                neighbor.isNewlyUnlocked = true;
                PlayerPrefs.SetInt(KeyFor(neighbor), (int)RegionState.Available);
                needsSave = true;
            }
        }

        if (needsSave) PlayerPrefs.Save();
    }

    // Never index the dictionary directly: a region missing from it — a duplicate
    // ID overwriting another, an entry added after Awake — took the whole sync
    // down with a KeyNotFoundException.
    private string KeyFor(RegionData region)
    {
        if (region == null) return "RegionState_-1";
        if (regionStateKeys.TryGetValue(region.regionID, out string key)) return key;
        key = "RegionState_" + region.regionID;
        regionStateKeys[region.regionID] = key;
        return key;
    }

    public void ConquerRegionAndUnlockNeighbors(RegionData conqueredRegion)
    {
        if (conqueredRegion.currentState == RegionState.Conquered) return;

        conqueredRegion.currentState = RegionState.Conquered;
        PlayerPrefs.SetInt(KeyFor(conqueredRegion), (int)RegionState.Conquered);

        int currentConquered = PlayerPrefs.GetInt("TotalConqueredRegions", 0);
        PlayerPrefs.SetInt("TotalConqueredRegions", currentConquered + 1);
        PlayerPrefs.Save();

        SyncMapStatesWithSaves();
        OnMapStateChanged?.Invoke();

        // First-time hint the very first region falls — teach the
        // neighbour-unlock system so the player realises the map opens
        // outward as they conquer.
        if (currentConquered == 0 && TutorialHints.Instance != null)
        {
            TutorialHints.Instance.ShowIfNew("FirstRegionConquered",
                "Region cleared! Its neighbours are now Available. Chain conquests outward — the map opens as you go.", 6f);
        }

        // Achievement milestones on the conqueror ladder.
        int newCount = currentConquered + 1;
        Analytics.Event("region_conquered", "region_id", conqueredRegion.regionID, "total", newCount);
        RunSession.AddRegion();
        if (newCount == 1)  AchievementSystem.Unlock("FIRST_BLOOD");
        if (newCount == 12) AchievementSystem.Unlock("HALFWAY");
        if (conqueredRegion.regionID == 22) AchievementSystem.Unlock("CITY_SIEGE");
        if (conqueredRegion.regionID == 24) AchievementSystem.Unlock("THRONE_TAKEN");
        if (newCount >= 24) AchievementSystem.Unlock("FULL_MAP");
    }

    public void RefreshMapState()
    {
        OnMapStateChanged?.Invoke();
    }
}