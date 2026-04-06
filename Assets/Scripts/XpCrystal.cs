using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    public float xpAmount = 10f;
    public float magnetSpeed = 15f;

    [Header("Spawn Pop Animation")]
    public float popRadius = 3f;
    public float popSpeed = 12f;
    private Vector3 popTarget;
    private bool isPopping = true;

    private Transform player;
    private PlayerController playerController;
    private bool isMagnetized = false;
    private bool collidersStripped = false;

    private void Awake()
    {
        StripPhysics();
    }

    private void OnEnable()
    {
        StripPhysics();
        ResetState();
    }

    private void StripPhysics()
    {
        if (collidersStripped) return;

        // Remove physics so crystals don't interfere with gameplay
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

        collidersStripped = true;
    }

    private void ResetState()
    {
        isMagnetized = false;
        isPopping = true;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        // Pop animation: scatter in random direction from spawn point
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.5f, popRadius);
        popTarget = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (Terrain.activeTerrain != null)
        {
            popTarget.y = Terrain.activeTerrain.SampleHeight(popTarget) + Terrain.activeTerrain.transform.position.y + 0.8f;
        }
    }

    private void Update()
    {
        // 1. Pop animation (fly out from death point)
        if (isPopping)
        {
            transform.position = Vector3.MoveTowards(transform.position, popTarget, popSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, popTarget) < 0.1f)
            {
                isPopping = false;
            }
            return;
        }

        // 2. Magnet pickup
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

                if (ObjectPool.Instance != null)
                    ObjectPool.Instance.ReturnToPool(gameObject);
                else
                    Destroy(gameObject);
            }
        }
    }
}