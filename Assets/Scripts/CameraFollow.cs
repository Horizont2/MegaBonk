using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    public float maxDistance = 8f;
    public float minDistance = 1.0f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;
    public float smoothSpeed = 10f;

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    [Header("Cinematic Bridge")]
    public bool isCinematicMode = false;

    [Header("Shake Settings")]
    private float shakeTimer;
    private float currentShakeIntensity;

    // НОВЕ: Направлена тряска
    private Vector3 shakeDirection;
    private float directionalShakeForce;

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
        if (isCinematicMode || Time.timeScale == 0f) return;
        if (target == null) return;

        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 lookAtPos = target.position + targetOffset;
        Vector3 direction = -(rotation * Vector3.forward);
        Vector3 desiredPosition = lookAtPos + direction * maxDistance;

        if (Physics.Linecast(lookAtPos, desiredPosition, out RaycastHit hit, collisionLayers))
        {
            currentDistance = Mathf.Clamp(hit.distance * 0.85f, minDistance, maxDistance);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, maxDistance, Time.deltaTime * smoothSpeed);
        }

        Vector3 finalPosition = lookAtPos + direction * currentDistance;

        // --- ДИНАМІЧНА ТА НАПРАВЛЕНА ТРЯСКА ---
        if (shakeTimer > 0)
        {
            // Хаотична тряска
            finalPosition += Random.insideUnitSphere * currentShakeIntensity;

            // Направлений поштовх (віддача)
            if (directionalShakeForce > 0)
            {
                // Поштовх згасає разом з таймером
                float pushForce = directionalShakeForce * (shakeTimer / 0.2f);
                finalPosition += shakeDirection * pushForce;
            }

            shakeTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            directionalShakeForce = 0f;
        }

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

    public void TriggerShake(float duration, float intensity)
    {
        shakeTimer = duration;
        currentShakeIntensity = intensity;
        directionalShakeForce = 0f; // Скидаємо направлену тряску
    }

    // НОВИЙ МЕТОД: Направлена тряска для віддачі та отримання шкоди
    public void TriggerDirectionalShake(Vector3 direction, float force, float duration, float randomIntensity)
    {
        shakeTimer = duration;
        shakeDirection = direction.normalized;
        directionalShakeForce = force;
        currentShakeIntensity = randomIntensity;
    }

    public void StartShake()
    {
        TriggerShake(0.2f, 0.3f);
    }

    public void SyncRotation(float x, float y)
    {
        currentX = x;
        currentY = y;
    }
}