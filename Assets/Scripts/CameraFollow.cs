using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    public float maxDistance = 8f;
    public float minDistance = 1.0f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers; // Шари перешкод
    public float smoothSpeed = 10f;   // Швидкість повернення камери

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    [Header("Cinematic Bridge")]
    public bool isCinematicMode = false; // Нове: вмикається Директором

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
        // Якщо працює катсцена або пауза — скрипт не перехоплює камеру[cite: 7]
        if (isCinematicMode || Time.timeScale == 0f) return;

        if (target == null) return;

        // 1. Керування мишею[cite: 7]
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // 2. Розрахунок позиції[cite: 7]
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 lookAtPos = target.position + targetOffset;
        Vector3 direction = -(rotation * Vector3.forward);
        Vector3 desiredPosition = lookAtPos + direction * maxDistance;

        // 3. ПЕРЕВІРКА КОЛІЗІЙ (Твоя оригінальна логіка)[cite: 7]
        if (Physics.Linecast(lookAtPos, desiredPosition, out RaycastHit hit, collisionLayers))
        {
            currentDistance = Mathf.Clamp(hit.distance * 0.85f, minDistance, maxDistance);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, maxDistance, Time.deltaTime * smoothSpeed);
        }

        Vector3 finalPosition = lookAtPos + direction * currentDistance;

        // 4. ДИНАМІЧНА ТРЯСКА (Твоя оригінальна логіка)[cite: 7]
        if (shakeTimer > 0)
        {
            finalPosition += Random.insideUnitSphere * currentShakeIntensity;
            shakeTimer -= Time.unscaledDeltaTime;
        }

        // 5. Застосування позиції та ANTI-CLIPPING[cite: 7]
        transform.position = finalPosition;
        transform.LookAt(lookAtPos);

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

    // Твої оригінальні методи для тряски[cite: 7]
    public void TriggerShake(float duration, float intensity)
    {
        shakeTimer = duration;
        currentShakeIntensity = intensity;
    }

    public void StartShake()
    {
        TriggerShake(0.2f, 0.3f);
    }

    // Метод для безшовного повернення з катсцени
    public void SyncRotation(float x, float y)
    {
        currentX = x;
        currentY = y;
    }
}