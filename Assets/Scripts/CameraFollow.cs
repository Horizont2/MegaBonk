using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    public float distance = 8f;

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    [Header("Shake Settings")]
    private float shakeTimer;
    private float currentShakeIntensity;

    private float currentX = 0f;
    private float currentY = 45f;

    private void Start()
    {
        transform.parent = null;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Керування мишею
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // 2. Розрахунок позиції
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = target.position + targetOffset - (rotation * Vector3.forward * distance);

        // 3. ДИНАМІЧНА ТРЯСКА (Працює навіть під час Hit Stop)
        if (shakeTimer > 0)
        {
            // Додаємо випадковий зсув, помножений на поточну інтенсивність
            desiredPosition += Random.insideUnitSphere * currentShakeIntensity;

            // Використовуємо unscaledDeltaTime, щоб тряска не сповільнювалася разом із грою
            shakeTimer -= Time.unscaledDeltaTime;
        }

        // 4. Застосування позиції
        transform.position = desiredPosition;
        transform.LookAt(target.position + targetOffset);

        // --- ANTI-CLIPPING (Захист від провалювання під землю) ---
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

    // Новий метод для MegaBoom: дозволяє задавати різну силу тряски
    public void TriggerShake(float duration, float intensity)
    {
        shakeTimer = duration;
        currentShakeIntensity = intensity;
    }

    // Старий метод (для сумісності з отриманням шкоди)
    public void StartShake()
    {
        TriggerShake(0.2f, 0.3f);
    }
}