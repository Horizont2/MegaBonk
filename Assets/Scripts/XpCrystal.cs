using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    public float xpAmount = 10f;
    public float magnetSpeed = 15f;

    [Header("Spawn Pop Animation")]
    public float popRadius = 3f; // Наскільки далеко розлітається лут від ворога
    public float popSpeed = 12f; // Швидкість розльоту
    private Vector3 popTarget;
    private bool isPopping = true; // Чи летить кристал зараз

    private Transform player;
    private PlayerController playerController;
    private bool isMagnetized = false;

    private void Awake()
    {
        // --- ЗАХИСТ ВІД БАГІВ ФІЗИКИ (Синдром Ліфта) ---
        gameObject.layer = 9;
        foreach (Transform t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 9;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
            Destroy(col);
        }

        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs) Destroy(rb);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        // --- РОЗРАХУНОК ТОЧКИ ВІДСКОКУ ---
        // Вибираємо випадковий напрямок навколо місця спавну
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.5f, popRadius);
        popTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Шукаємо ідеальну висоту землі для цієї нової точки
        if (Terrain.activeTerrain != null)
        {
            popTarget.y = Terrain.activeTerrain.SampleHeight(popTarget) + Terrain.activeTerrain.transform.position.y + 0.8f;
        }
    }

    private void Update()
    {
        // 1. АНІМАЦІЯ РОЗЛЬОТУ (Відбувається до увімкнення магніту)
        if (isPopping)
        {
            // Рухаємо кристал до цільової точки відскоку
            transform.position = Vector3.MoveTowards(transform.position, popTarget, popSpeed * Time.deltaTime);

            // Якщо кристал долетів до точки - вимикаємо політ
            if (Vector3.Distance(transform.position, popTarget) < 0.1f)
            {
                isPopping = false;
            }
            return; // Блокуємо код нижче, поки лут не приземлиться
        }

        // 2. СИСТЕМА МАГНІТУ
        if (player == null || playerController == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isMagnetized && distance <= playerController.pickupRadius)
        {
            isMagnetized = true;
        }

        if (isMagnetized)
        {
            Vector3 targetPos = player.position + Vector3.up * 1f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                playerController.GainXP(xpAmount);
                Destroy(gameObject);
            }
        }
    }
}