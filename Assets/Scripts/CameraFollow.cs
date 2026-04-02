using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Щоб камера дивилася на спину/голову, а не в ноги
    public float distance = 8f; // Віддаленість камери

    [Header("Mouse Control")]
    public float mouseSensitivity = 3f;
    public float minYAngle = -20f; // Наскільки низько можна опустити камеру
    public float maxYAngle = 80f;  // Наскільки високо можна підняти

    [Header("Shake Settings")]
    public float shakeIntensity = 0.3f;
    public float shakeDuration = 0.2f;

    private float currentX = 0f;
    private float currentY = 45f;
    private float shakeTimer;

    private void Start()
    {
        transform.parent = null;
        // Ховаємо курсор миші для зручного керування (натисни ESC в Unity, щоб повернути його)
        Cursor.lockState = CursorLockMode.Locked; 
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Зчитуємо рух миші
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Обмежуємо кут нахилу, щоб камера не переверталася догори дригом
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // Вираховуємо нову позицію по колу
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = target.position + targetOffset - (rotation * Vector3.forward * distance);

        // Трясіння
        if (shakeTimer > 0)
        {
            desiredPosition += Random.insideUnitSphere * shakeIntensity;
            shakeTimer -= Time.deltaTime;
        }

        // Застосовуємо позицію і змушуємо камеру дивитися на гравця
        transform.position = desiredPosition;
        transform.LookAt(target.position + targetOffset);

        // --- ANTI-CLIPPING (Keep camera above ground) ---
        if (Terrain.activeTerrain != null)
        {
            // Find the height of the mountain exactly under the camera
            float terrainHeight = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;

            // The camera must always be at least 1.5 meters above the dirt
            float minCameraHeight = terrainHeight + 1.5f;

            if (transform.position.y < minCameraHeight)
            {
                Vector3 safePos = transform.position;
                safePos.y = minCameraHeight;
                transform.position = safePos;
            }
        }
    }

    public void StartShake()
    {
        shakeTimer = shakeDuration;
    }
}