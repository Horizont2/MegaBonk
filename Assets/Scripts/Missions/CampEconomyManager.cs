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
                totalWood += region.passiveWood;
                totalStone += region.passiveStone;
                totalFood += region.passiveFood;
                totalDiamonds += region.passiveDiamonds;
            }
        }

        // ТУТ БУДЕ ВАШ КОД ДОДАВАННЯ РЕСУРСІВ (наприклад, звернення до ResourceManager)
        // ResourceManager.Instance.AddWood(totalWood);

        Debug.Log($"[Economy] Зібрано пасивний дохід: Дерево +{totalWood}, Камінь +{totalStone}, Їжа +{totalFood}, Діаманти +{totalDiamonds}");
    }
}