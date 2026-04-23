using UnityEngine;

public class DiamondPickup : MonoBehaviour
{
    public int diamondAmount = 1;
    public float magnetSpeed = 15f;

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
    private float baseY;

    private void Awake()
    {
        gameObject.layer = 9; // LootPhysics øàð
        int minimapLayer = LayerMask.NameToLayer("MinimapOnly");

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer != minimapLayer) t.gameObject.layer = 9;
        }

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

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.5f, popRadius);
        popTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (Terrain.activeTerrain != null)
        {
            popTarget.y = Terrain.activeTerrain.SampleHeight(popTarget) + Terrain.activeTerrain.transform.position.y + 0.8f;
        }
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
                // ÂÈÊËÈÊÀªÌÎ ÍÎÂÓ ÔÓÍÊÖ²Þ!
                playerController.GainDiamond(diamondAmount);
                Destroy(gameObject);
            }
            return;
        }

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = baseY + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}