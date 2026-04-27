using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ResourceDrop : MonoBehaviour
{
    public enum ResourceType { Wood, Stone, Food, Diamond }

    [Header("Drop Settings")]
    public ResourceType resourceType;
    public int amount = 1;
    public float popForce = 6f; // Сила вибуху вгору

    [Header("Idle Animation")]
    public float spinSpeed = 120f; // Як швидко крутиться на землі

    [Header("Magnet Settings")]
    public float magnetSpeed = 20f;
    private bool isMagnetizing = false;

    private Transform player;
    private PlayerController playerController;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. Потужний стрибок вгору і в боки
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 2f, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDir * popForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * popForce * 2f, ForceMode.Impulse);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        // 2. Через 1.5с предмети падають на землю, і ми вмикаємо їм красиве обертання
        Invoke(nameof(StartSpinning), 1.5f);
    }

    private void StartSpinning()
    {
        if (!isMagnetizing)
        {
            rb.isKinematic = true; // Вимикаємо фізику, щоб не заважала
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (player == null || playerController == null) return;

        // --- АНІМАЦІЯ ОБЕРТАННЯ ---
        // Якщо фізика вимкнена (предмет впав) і він ще не магнітиться
        if (rb.isKinematic && !isMagnetizing)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // Починаємо магнітитись до гравця
        if (!isMagnetizing && dist <= playerController.pickupRadius)
        {
            isMagnetizing = true;
            rb.isKinematic = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        if (isMagnetizing)
        {
            // Летимо в груди гравцю (Vector3.up)
            transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, magnetSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, player.position + Vector3.up) < 0.5f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        if (ResourceManager.Instance != null)
        {
            // ЗАМІНЕНО AddResources на AddRunResources
            if (resourceType == ResourceType.Wood) ResourceManager.Instance.AddRunResources(amount, 0, 0);
            else if (resourceType == ResourceType.Stone) ResourceManager.Instance.AddRunResources(0, amount, 0);
            else if (resourceType == ResourceType.Food) ResourceManager.Instance.AddRunResources(0, 0, amount);
            else if (resourceType == ResourceType.Diamond) playerController.GainDiamond(amount);
        }

        Destroy(gameObject);
    }
}