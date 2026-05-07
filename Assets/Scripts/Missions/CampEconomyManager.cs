using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CampEconomyManager : MonoBehaviour
{
    [Header("Economy Settings")]
    public float resourceTickInterval = 60f; // Збирати ресурси кожні 60 секунд

    private void Start()
    {
        StartCoroutine(EconomyTickRoutine());
    }

    private IEnumerator EconomyTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(resourceTickInterval);
            CollectPassiveIncome();
        }
    }

    private void CollectPassiveIncome()
    {
        if (MapProgressionManager.Instance == null) return;

        int totalWood = 0, totalStone = 0, totalFood = 0, totalDiamonds = 0;

        // Рахуємо дохід тільки з захоплених територій
        foreach (RegionData region in MapProgressionManager.Instance.allRegionsInGame)
        {
            if (region.currentState == RegionState.Conquered)
            {
                // --- ФІКС: Зчитуємо рівень регіону і беремо дані з масиву upgradeLevels ---
                int currentLevel = PlayerPrefs.GetInt("RegionLevel_" + region.regionID, 1);

                // Захист від помилок (якщо масив в інспекторі раптом не заповнено)
                if (region.upgradeLevels != null && region.upgradeLevels.Length >= currentLevel)
                {
                    RegionLevelData levelData = region.upgradeLevels[currentLevel - 1];

                    totalWood += levelData.passiveWood;
                    totalStone += levelData.passiveStone;
                    totalFood += levelData.passiveFood;
                    totalDiamonds += levelData.passiveDiamonds;
                }
            }
        }

        // ТУТ БУДЕ ВАШ КОД ДОДАВАННЯ РЕСУРСІВ (наприклад, звернення до ResourceManager)
        // ResourceManager.Instance.AddWood(totalWood);

        Debug.Log($"[Economy] Зібрано пасивний дохід: Дерево +{totalWood}, Камінь +{totalStone}, Їжа +{totalFood}, Діаманти +{totalDiamonds}");
    }
}