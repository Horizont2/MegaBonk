using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DistanceOptimizer : MonoBehaviour
{
    public static DistanceOptimizer Instance;

    [Header("Settings")]
    public Transform player;

    [Tooltip("Радіус видимості. Рекомендую збільшити до 100-120")]
    public float cullDistance = 100f;

    [Tooltip("Частота перевірки. 0.2 = швидке оновлення 5 разів на секунду")]
    public float checkInterval = 0.2f;

    [Tooltip("Кількість об'єктів за кадр. 1000 - це абсолютно нормально")]
    public int checksPerFrame = 1000;

    private List<OptimizedObject> managedObjects = new List<OptimizedObject>();
    private float sqrCullDistance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sqrCullDistance = cullDistance * cullDistance;
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        StartCoroutine(OptimizationRoutine());
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void RegisterObject(OptimizedObject obj)
    {
        // ВАЖЛИВО: Ми прибрали !managedObjects.Contains(obj)
        // Це прибрало 12+ мільйонів зайвих ітерацій при завантаженні сцени!
        managedObjects.Add(obj);

        FindPlayerIfNeeded();
        InitialCheck(obj);
    }

    public void UnregisterObject(OptimizedObject obj)
    {
        managedObjects.Remove(obj);
    }

    private void InitialCheck(OptimizedObject obj)
    {
        if (player == null || obj == null || obj.targetObject == null) return;

        float distSqr = (obj.transform.position - player.position).sqrMagnitude;
        bool shouldBeActive = distSqr <= sqrCullDistance;

        // Викликаємо SetActive ТІЛЬКИ якщо стан дійсно треба змінити
        if (obj.targetObject.activeSelf != shouldBeActive)
        {
            obj.targetObject.SetActive(shouldBeActive);
        }
    }

    private IEnumerator OptimizationRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            if (player == null || managedObjects.Count == 0)
            {
                yield return wait;
                continue;
            }

            Vector3 playerPos = player.position;
            int count = 0;

            for (int i = managedObjects.Count - 1; i >= 0; i--)
            {
                OptimizedObject obj = managedObjects[i];

                if (obj == null || obj.targetObject == null)
                {
                    managedObjects.RemoveAt(i);
                    continue;
                }

                float distSqr = (obj.transform.position - playerPos).sqrMagnitude;
                bool shouldBeActive = distSqr <= sqrCullDistance;

                if (obj.targetObject.activeSelf != shouldBeActive)
                {
                    obj.targetObject.SetActive(shouldBeActive);
                }

                count++;

                if (count >= checksPerFrame)
                {
                    count = 0;
                    yield return null;

                    if (player != null) playerPos = player.position;
                }
            }

            yield return wait;
        }
    }
}