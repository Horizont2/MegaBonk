using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapAmbienceManager : MonoBehaviour
{
    private RegionUI[] allRegionUIs;
    public float minInterval = 3f;
    public float maxInterval = 8f;

    private void Start()
    {
        // Знаходимо всі регіони на мапі
        allRegionUIs = FindObjectsOfType<RegionUI>();
        StartCoroutine(LightningTick());
    }

    private IEnumerator LightningTick()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            // Складаємо список тільки заблокованих регіонів
            List<RegionUI> lockedRegions = new List<RegionUI>();
            foreach (var r in allRegionUIs)
            {
                if (r.myRegionData.currentState == RegionState.Locked) lockedRegions.Add(r);
            }

            // Б'ємо блискавкою у випадковий заблокований регіон
            if (lockedRegions.Count > 0)
            {
                lockedRegions[Random.Range(0, lockedRegions.Count)].DoLightningFlash();
            }
        }
    }
}