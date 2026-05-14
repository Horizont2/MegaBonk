using UnityEngine;

public class OptimizedObject : MonoBehaviour
{
    [Tooltip("Залиш порожнім, якщо хочеш вимикати весь цей об'єкт. Або перетягни сюди візуальну модель, якщо логіка/колайдер має працювати завжди.")]
    public GameObject targetObject;

    private void Start()
    {
        if (targetObject == null) targetObject = this.gameObject;

        // Реєструємо себе в менеджері
        if (DistanceOptimizer.Instance != null)
        {
            DistanceOptimizer.Instance.RegisterObject(this);
        }
    }

    private void OnDestroy()
    {
        // Видаляємо себе, коли об'єкт знищується
        if (DistanceOptimizer.Instance != null)
        {
            DistanceOptimizer.Instance.UnregisterObject(this);
        }
    }
}