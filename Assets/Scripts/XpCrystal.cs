using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    public float xpAmount = 10f;

    [Header("Smart Magnet AI")]
    public float maxMagnetSpeed = 25f;
    public float acceleration = 12f;
    public float dropOffMultiplier = 1.8f;

    [Header("Spawn Pop Animation")]
    public float popRadius = 3f;
    public float popSpeed = 12f;
    private Vector3 popTarget;
    private bool isPopping = true;

    [Header("Hover Animation")]
    public float hoverSpeed = 3f;
    public float hoverHeight = 0.3f;
    public float rotationSpeed = 100f;

    private Transform player;
    private PlayerController playerController;
    private bool isMagnetized = false;
    private float currentFlySpeed = 0f;
    private float baseY;

    // --- ОПТИМІЗАЦІЯ ---
    private static int lastPlayFrame = -1; // Трекер для звуку
    private float pickupRadiusSqr;
    private float dropOffRadiusSqr;

    private void Awake() { /* Залишай тут свій код для ігнорування колізій, якщо він там був */ }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();

            // Рахуємо квадрати дистанцій один раз при старті, а не кожен кадр
            pickupRadiusSqr = playerController.pickupRadius * playerController.pickupRadius;
            dropOffRadiusSqr = (playerController.pickupRadius * dropOffMultiplier) * (playerController.pickupRadius * dropOffMultiplier);
        }

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.5f, popRadius);
        popTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (Terrain.activeTerrain != null)
            popTarget.y = Terrain.activeTerrain.SampleHeight(popTarget) + Terrain.activeTerrain.transform.position.y + 0.8f;
    }

    private void Update()
    {
        // 1. Анімація появи
        if (isPopping)
        {
            transform.position = Vector3.MoveTowards(transform.position, popTarget, popSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

            // Оптимізована перевірка дистанції
            if ((transform.position - popTarget).sqrMagnitude < 0.01f)
            {
                isPopping = false;
                baseY = transform.position.y;
            }
            return;
        }

        if (player == null || playerController == null) return;

        // 2. Логіка розумного магніту (ВИКОРИСТОВУЄМО SQR MAGNITUDE)
        float distSqr = (transform.position - player.position).sqrMagnitude;

        if (!isMagnetized && distSqr <= pickupRadiusSqr)
        {
            isMagnetized = true;
            currentFlySpeed = 0f;
        }
        else if (isMagnetized && distSqr > dropOffRadiusSqr)
        {
            isMagnetized = false;
            baseY = transform.position.y;
        }

        // 3. Політ до гравця
        if (isMagnetized)
        {
            currentFlySpeed = Mathf.Lerp(currentFlySpeed, maxMagnetSpeed, Time.deltaTime * acceleration);
            Vector3 targetPos = player.position + Vector3.up * 1f;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentFlySpeed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude < 0.25f) // Менше ніж 0.5 метра
            {
                // ФІКС АУДІО-ФРІЗУ: Дозволяємо звуку грати ТІЛЬКИ 1 раз за поточний кадр.
                if (Time.frameCount != lastPlayFrame)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectGem);
                    lastPlayFrame = Time.frameCount;
                }

                playerController.GainXP(xpAmount);
                Destroy(gameObject);
            }
            return;
        }

        // 4. Стандартне зависання
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = baseY + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}