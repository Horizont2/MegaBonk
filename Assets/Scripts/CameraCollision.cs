using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform player;          // Твій Player
    public float minDistance = 1.0f;  // Мінімальна відстань до гравця
    public float maxDistance = 10.0f; // Стандартна відстань камери
    public float smoothSpeed = 10.0f; // Швидкість наближення/віддалення
    public Vector3 dollyDir;          // Напрямок камери відносно гравця
    public float distance;            // Поточна відстань

    [Header("Collision Settings")]
    public LayerMask collisionLayers; // Шари, об які камера буде вдарятися

    void Awake()
    {
        dollyDir = transform.localPosition.normalized;
        distance = transform.localPosition.magnitude;
    }

    void Update()
    {
        // 🛡️ ЗАХИСТ: Якщо у камери тимчасово немає батьківського об'єкта 
        // (наприклад, під час завантаження сцени), нічого не робимо
        if (transform.parent == null) return;

        Vector3 desiredCameraPos = transform.parent.TransformPoint(dollyDir * maxDistance);
        RaycastHit hit;

        // Кидаємо промінь від батьківського об'єкта до бажаної позиції камери
        if (Physics.Linecast(transform.parent.position, desiredCameraPos, out hit, collisionLayers))
        {
            // Якщо промінь вдарився в будівлю/стіну - наближаємо камеру
            distance = Mathf.Clamp((hit.distance * 0.8f), minDistance, maxDistance);
        }
        else
        {
            // Якщо шлях вільний - повертаємо камеру назад
            distance = maxDistance;
        }

        // Плавно пересуваємо камеру
        transform.localPosition = Vector3.Lerp(transform.localPosition, dollyDir * distance, Time.deltaTime * smoothSpeed);
    }
}