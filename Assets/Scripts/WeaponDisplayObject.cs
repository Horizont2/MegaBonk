using UnityEngine;

public class WeaponDisplayObject : MonoBehaviour
{
    [Header("Settings")]
    public float standAngleX = -90f;
    public float tiltAngleZ = 45f;
    public float tiltSpeed = 8f;
    public float rotationSpeed = 500f;

    [Header("Animation")]
    public float moveSpeed = 5f;
    public float idleBobAmount = 0.05f;
    public float idleBobSpeed = 2f;

    private Transform tablePoint;
    private Transform inspectPoint;

    private bool isInspecting = false;
    private bool isDragging = false;   // <--- ДОДАНО: Перевірка, чи тримаємо ми зброю

    private float currentYRot;
    private float currentTiltZ;
    private float currentStandX;
    private float startY;

    public void Setup(Transform tablePos, Transform inspectPos)
    {
        tablePoint = tablePos;
        inspectPoint = inspectPos;

        transform.position = tablePoint.position;
        currentYRot = tablePoint.eulerAngles.y;
        currentTiltZ = 0f;
        currentStandX = 0f;
        transform.rotation = Quaternion.Euler(0, currentYRot, 0);
        startY = transform.position.y;
    }

    public void SetInspect(bool state)
    {
        isInspecting = state;
        if (!isInspecting && tablePoint != null)
        {
            currentYRot = tablePoint.eulerAngles.y;
            isDragging = false; // Скидаємо обертання при виході
        }
    }

    void Update()
    {
        if (tablePoint == null || inspectPoint == null) return;

        float targetTilt = isInspecting ? tiltAngleZ : 0f;
        float targetStand = isInspecting ? standAngleX : 0f;

        currentTiltZ = Mathf.Lerp(currentTiltZ, targetTilt, Time.deltaTime * tiltSpeed);
        currentStandX = Mathf.Lerp(currentStandX, targetStand, Time.deltaTime * tiltSpeed);

        // ОБЕРТАННЯ: Працює тільки якщо ми в режимі огляду І затиснули палець НА ЗБРОЇ
        if (isInspecting && isDragging)
        {
            currentYRot -= Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        }

        transform.rotation = Quaternion.Euler(currentStandX, currentYRot, currentTiltZ);

        if (!isInspecting)
        {
            float newY = startY + Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
            Vector3 targetPos = new Vector3(tablePoint.position.x, newY, tablePoint.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, inspectPoint.position, Time.deltaTime * moveSpeed);
        }
    }

    // ЛОГІКА КЛІКІВ ПО САМІЙ ЗБРОЇ
    private void OnMouseDown()
    {
        if (!isInspecting)
        {
            ShopManager shop = FindFirstObjectByType<ShopManager>();
            if (shop != null) shop.StartInspect();
        }
        else
        {
            isDragging = true; // Починаємо обертати зброю
        }
    }

    private void OnMouseUp()
    {
        isDragging = false; // Відпустили зброю - припиняємо обертання
    }
}