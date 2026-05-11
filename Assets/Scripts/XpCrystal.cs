using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    public float xpAmount = 10f;

    [Header("Smart Magnet AI")]
    public float maxMagnetSpeed = 25f;
    public float acceleration = 12f;
    public float dropOffMultiplier = 1.8f; // Якщо гравець відійде на цю дистанцію, кристал перестане летіти за ним

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

    private void Awake() { /* Твій код для шарів та колізій залишається */ }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { player = p.transform; playerController = p.GetComponent<PlayerController>(); }

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
            if (Vector3.Distance(transform.position, popTarget) < 0.1f)
            {
                isPopping = false;
                baseY = transform.position.y;
            }
            return;
        }

        if (player == null || playerController == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ЛОГІКА РОЗУМНОГО МАГНІТУ
        if (!isMagnetized && distance <= playerController.pickupRadius)
        {
            isMagnetized = true;
            currentFlySpeed = 0f; // Починає летіти повільно
        }
        else if (isMagnetized && distance > playerController.pickupRadius * dropOffMultiplier)
        {
            isMagnetized = false; // Гравець втік! Кристал втрачає інтерес
            baseY = transform.position.y; // Запам'ятовуємо нову висоту для зависання
        }

        if (isMagnetized)
        {
            // Плавний розгін (Smooth Damping ефект)
            currentFlySpeed = Mathf.Lerp(currentFlySpeed, maxMagnetSpeed, Time.deltaTime * acceleration);
            Vector3 targetPos = player.position + Vector3.up * 1f;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentFlySpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectGem);
                playerController.GainXP(xpAmount);
                Destroy(gameObject);
            }
            return;
        }

        // Стандартне зависання, коли не магнітиться
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = baseY + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}