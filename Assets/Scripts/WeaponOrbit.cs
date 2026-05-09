using UnityEngine;

public class WeaponOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform pivot;
    public float baseRotationSpeed = 120f;  // Швидкість, коли стоїмо
    public float runRotationMultiplier = 2f; // Прискорення під час бігу
    public float orbitDistance = 2.5f;
    public float orbitHeight = 1f;

    [Header("AAA Smoothness")]
    public float followSpeed = 15f;   // Плавність наздоганяння гравця
    public float bobbingAmount = 0.4f; // Амплітуда левітації
    public float bobbingSpeed = 3f;    // Швидкість левітації

    private float currentAngle;
    private Vector3 targetPosition;

    private void Start()
    {
        if (pivot == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) pivot = playerObj.transform;
        }

        if (pivot != null)
        {
            transform.parent = null; // Відв'язуємо, щоб молот літав сам
            Vector3 startPos = transform.position;
            startPos.y = pivot.position.y + orbitHeight;
            transform.position = startPos;
        }
    }

    private void LateUpdate()
    {
        if (pivot == null) return;

        // 1. Рахуємо швидкість: повільно, якщо стоїмо, швидко — якщо біжимо
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float currentSpeed = (h != 0 || v != 0) ? baseRotationSpeed * runRotationMultiplier : baseRotationSpeed;

        // 2. Оновлюємо кут
        currentAngle += currentSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        // 3. Рахуємо ідеальну позицію кола
        float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * orbitDistance;
        float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * orbitDistance;

        // 4. Додаємо ефект левітації (Sine wave)
        float currentBobbing = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;

        // 5. Розраховуємо цільову позицію
        targetPosition = pivot.position + new Vector3(x, orbitHeight + currentBobbing, z);

        // 6. ПЛАВНО переміщуємо молот до цілі (Lerp) замість жорсткої телепортації
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // 7. Повертаємо молот у напрямку його руху
        Vector3 orbitDirection = (targetPosition - pivot.position).normalized;
        // Додаємо 90 градусів, щоб молот летів "боком" або "носом" уперед
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(-orbitDirection.z, 0, orbitDirection.x));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
    }
}