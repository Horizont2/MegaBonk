using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    [Header("Grenade Settings")]
    public GameObject grenadePrefab;
    public Transform throwPoint;

    [Header("Throw Force Settings")]
    public float minForce = 5f;       // Мінімальна сила (якщо просто клікнути)
    public float maxForce = 30f;      // Максимальна сила кидка
    public float chargeRate = 15f;    // Як швидко накопичується сила
    public float upwardAngle = 0.5f;  // Кут кидка вгору

    [Header("Trajectory Line")]
    public LineRenderer lineRenderer;
    public int linePoints = 30;
    public float timeBetweenPoints = 0.1f;

    private Camera mainCam;
    private float currentForce;
    private bool isCharging = false;

    private void Start()
    {
        mainCam = Camera.main;
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        // Початок заряджання (ЛКМ - кнопка 0)
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentForce = minForce;
        }

        // Процес заряджання (тримаємо ЛКМ)
        if (Input.GetMouseButton(0) && isCharging)
        {
            currentForce += chargeRate * Time.deltaTime;
            if (currentForce > maxForce) currentForce = maxForce; // Обмежуємо максимум

            DrawTrajectory();
        }

        // Кидок (відпускаємо ЛКМ)
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ThrowGrenade();
            isCharging = false;
            currentForce = minForce;
            lineRenderer.positionCount = 0; // Ховаємо лінію
        }
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, throwPoint.position);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 direction = (hitPoint - throwPoint.position).normalized;
            direction.y = 0; // Тільки горизонтальний напрямок
            return direction.normalized;
        }
        return transform.forward;
    }

    private Vector3 GetThrowVelocity()
    {
        Vector3 aimDir = GetAimDirection();
        // Додаємо кут вгору, щоб граната летіла дугою
        Vector3 throwDir = (aimDir + Vector3.up * upwardAngle).normalized;
        return throwDir * currentForce;
    }

    private void DrawTrajectory()
    {
        lineRenderer.positionCount = linePoints;
        Vector3 startPosition = throwPoint.position;
        Vector3 startVelocity = GetThrowVelocity();

        for (int i = 0; i < linePoints; i++)
        {
            float t = i * timeBetweenPoints;
            Vector3 point = startPosition + startVelocity * t + Physics.gravity * 0.5f * t * t;

            lineRenderer.SetPosition(i, point);

            if (point.y < 0f && i > 5)
            {
                lineRenderer.positionCount = i + 1;
                break;
            }
        }
    }

    private void ThrowGrenade()
    {
        if (grenadePrefab == null || throwPoint == null) return;

        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = GetThrowVelocity();
        }
    }
}