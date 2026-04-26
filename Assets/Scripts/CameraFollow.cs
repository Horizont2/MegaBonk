using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    // Замінили distance на maxDistance для зручності
    public float maxDistance = 8f;
    public float minDistance = 1.0f;

    [Header("Collision Settings (NEW)")]
    public LayerMask collisionLayers; // Шари, крізь які камера не може пройти
    public float smoothSpeed = 10f;   // Швидкість повернення камери

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    [Header("Shake Settings")]
    private float shakeTimer;
    private float currentShakeIntensity;

    private float currentX = 0f;
    private float currentY = 45f;
    private float currentDistance;

    private void Start()
    {
        transform.parent = null;
        Cursor.lockState = CursorLockMode.Locked;
        currentDistance = maxDistance;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Керування мишею
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // 2. Розрахунок бажаної позиції (ніби перешкод немає)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 lookAtPos = target.position + targetOffset;
        Vector3 direction = -(rotation * Vector3.forward);
        Vector3 desiredPosition = lookAtPos + direction * maxDistance;

        // 3. ПЕРЕВІРКА КОЛІЗІЙ (Щоб не заглядати в будинки)
        if (Physics.Linecast(lookAtPos, desiredPosition, out RaycastHit hit, collisionLayers))
        {
            // Якщо промінь вдарився - наближаємо камеру (множимо на 0.85, щоб вона не влипала в саму текстуру)
            currentDistance = Mathf.Clamp(hit.distance * 0.85f, minDistance, maxDistance);
        }
        else
        {
            // Якщо перешкод немає - плавно повертаємо камеру на максимальну відстань
            currentDistance = Mathf.Lerp(currentDistance, maxDistance, Time.deltaTime * smoothSpeed);
        }

        // Обчислюємо фінальну позицію з урахуванням колізій
        Vector3 finalPosition = lookAtPos + direction * currentDistance;

        // 4. ДИНАМІЧНА ТРЯСКА
        if (shakeTimer > 0)
        {
            finalPosition += Random.insideUnitSphere * currentShakeIntensity;
            shakeTimer -= Time.unscaledDeltaTime;
        }

        // 5. Застосування позиції
        transform.position = finalPosition;
        transform.LookAt(lookAtPos);

        // 6. ANTI-CLIPPING (Захист від провалювання під землю)
        if (Terrain.activeTerrain != null)
        {
            float terrainHeight = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
            float minCameraHeight = terrainHeight + 1.5f;

            if (transform.position.y < minCameraHeight)
            {
                Vector3 safePos = transform.position;
                safePos.y = minCameraHeight;
                transform.position = safePos;
            }
        }
    }

    public void TriggerShake(float duration, float intensity)
    {
        shakeTimer = duration;
        currentShakeIntensity = intensity;
    }

    public void StartShake()
    {
        TriggerShake(0.2f, 0.3f);
    }
}