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

    // --- СУПЕР ОПТИМІЗАЦІЯ ---
    private static float lastPlayTime = -1f; // Трекер звуку по реальному часу
    private float pickupRadiusSqr;
    private float dropOffRadiusSqr;

    // Кешуємо компоненти, щоб не шукати їх під час гри
    private Collider col;
    private Rigidbody rb;
    private Renderer[] renderers;

    private void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>(); // Знаходимо всі меші кристала
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();

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
        if (isPopping)
        {
            transform.position = Vector3.MoveTowards(transform.position, popTarget, popSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

            if ((transform.position - popTarget).sqrMagnitude < 0.01f)
            {
                isPopping = false;
                baseY = transform.position.y;
            }
            return;
        }

        if (player == null || playerController == null) return;

        float distSqr = (transform.position - player.position).sqrMagnitude;

        if (!isMagnetized && distSqr <= pickupRadiusSqr)
        {
            isMagnetized = true;
            currentFlySpeed = 0f;

            // ФІКС ФІЗИКИ: Вимикаємо колайдер і Rigidbody!
            // Тепер кристал "прозорий" для фізичного рушія і не створює лагів при вльоті в гравця.
            if (col != null) col.enabled = false;
            if (rb != null) rb.isKinematic = true;
        }
        else if (isMagnetized && distSqr > dropOffRadiusSqr)
        {
            isMagnetized = false;
            baseY = transform.position.y;

            // Якщо гравець втік, повертаємо колізію назад
            if (col != null) col.enabled = true;
            if (rb != null) rb.isKinematic = false;
        }

        if (isMagnetized)
        {
            currentFlySpeed = Mathf.Lerp(currentFlySpeed, maxMagnetSpeed, Time.deltaTime * acceleration);
            Vector3 targetPos = player.position + Vector3.up * 1f;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentFlySpeed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude < 0.25f) // Коли кристал торкнувся гравця
            {
                // ФІКС АУДІО: Захист 0.05 секунд реального часу, щоб звуки не "нашаровувались"
                if (Time.time - lastPlayTime > 0.05f)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectGem);
                    lastPlayTime = Time.time;
                }

                playerController.GainXP(xpAmount);

                // ФІКС GC (Garbage Collector): 
                // Замість миттєвого знищення, просто робимо об'єкт невидимим і вимикаємо цей скрипт.
                // А саме видалення з пам'яті (Destroy) відкладаємо на випадковий час.
                foreach (Renderer r in renderers) if (r != null) r.enabled = false;
                this.enabled = false;
                Destroy(gameObject, Random.Range(0.5f, 2f));
            }
            return;
        }

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = baseY + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}