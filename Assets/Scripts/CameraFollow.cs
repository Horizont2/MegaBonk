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

    [Header("Juice & AAA Feel")]
    public float positionSmoothTime = 0.08f; // Затримка камери (чим більше, тим "важча" камера)
    private Vector3 currentTargetPos;
    private Vector3 targetPosVelocity;

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f;
    public float maxYAngle = 80f;

    [Header("Cinematic Bridge")]
    public bool isCinematicMode = false;

    [Header("Shake Settings")]
    private float shakeTimer;
    private float currentShakeIntensity;
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

        if (target != null) currentTargetPos = target.position;
    }

    private void LateUpdate()
    {
        if (isCinematicMode || Time.timeScale == 0f || target == null) return;

        // МАГІЯ ААА-КАМЕРИ: Плавне слідування за позицією гравця (замість жорсткого)
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, target.position, ref targetPosVelocity, positionSmoothTime);

        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Використовуємо згладжену позицію замість target.position
        Vector3 lookAtPos = currentTargetPos + targetOffset;
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

        if (shakeTimer > 0)
        {
            finalPosition += Random.insideUnitSphere * currentShakeIntensity;
            if (directionalShakeForce > 0)
            {
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

        // Захист від провалювання під землю
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

    public void TriggerShake(float duration, float intensity) { /* Залишаємо як було */ }
    public void TriggerDirectionalShake(Vector3 direction, float force, float duration, float randomIntensity) { /* Залишаємо як було */ }
    public void StartShake() { TriggerShake(0.2f, 0.3f); }
    public void SyncRotation(float x, float y) { currentX = x; currentY = y; }
}