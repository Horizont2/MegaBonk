using UnityEngine;
using System.Collections;

[RequireComponent(typeof(WindZone))]
public class DynamicWind : MonoBehaviour
{
    private WindZone windZone;

    [Header("Wind Settings")]
    public float minWindWaitTime = 15f; // Мінімальний час між змінами вітру
    public float maxWindWaitTime = 35f; // Максимальний час

    void Start()
    {
        windZone = GetComponent<WindZone>();
        StartCoroutine(WindRoutine());
    }

    IEnumerator WindRoutine()
    {
        while (true)
        {
            // Генеруємо нову ціль для вітру
            float targetMain = Random.Range(0.1f, 1.2f); // Сила вітру
            float targetTurbulence = Random.Range(0.1f, 0.8f); // Хаотичність

            // Випадковий напрямок (повертаємо сам об'єкт)
            Quaternion targetRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            float t = 0;
            float transitionDuration = 6f; // Вітер плавно змінюється цілих 6 секунд!

            float startMain = windZone.windMain;
            float startTurbulence = windZone.windTurbulence;
            Quaternion startRotation = transform.rotation;

            // Плавна інтерполяція (зміна не б'є по очах)
            while (t < 1)
            {
                t += Time.deltaTime / transitionDuration;
                windZone.windMain = Mathf.Lerp(startMain, targetMain, Mathf.SmoothStep(0, 1, t));
                windZone.windTurbulence = Mathf.Lerp(startTurbulence, targetTurbulence, Mathf.SmoothStep(0, 1, t));
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            // Чекаємо перед наступною зміною
            yield return new WaitForSeconds(Random.Range(minWindWaitTime, maxWindWaitTime));
        }
    }
}