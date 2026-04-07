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

    [Header("Idle Animation")]
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 90f;
    public float glowPulseSpeed = 3f;
    public float glowPulseIntensity = 0.3f;

    private Transform player;
    private PlayerController playerController;
    private bool isMagnetized = false;
    private bool collidersStripped = false;
    private Vector3 restPosition;
    private float bobTimer;
    private MeshRenderer crystalRenderer;
    private Color crystalBaseColor;

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
        bobTimer = Random.Range(0f, Mathf.PI * 2f); // Randomize phase so crystals don't bob in sync

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        crystalRenderer = GetComponentInChildren<MeshRenderer>();
        if (crystalRenderer != null) crystalBaseColor = crystalRenderer.material.color;

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
                restPosition = transform.position;
            }
            return;
        }

        // 2. Idle animation: bob up/down + rotate + glow pulse
        if (!isMagnetized)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
            transform.position = restPosition + Vector3.up * bobOffset;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Subtle brightness pulse
            if (crystalRenderer != null)
            {
                float pulse = 1f + Mathf.Sin(bobTimer * glowPulseSpeed) * glowPulseIntensity;
                crystalRenderer.material.color = crystalBaseColor * pulse;
            }
        }

        // 3. Magnet pickup
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

            // Scale up slightly as it approaches player for a satisfying pickup feel
            float dist = Vector3.Distance(transform.position, targetPos);
            float scaleMult = Mathf.Lerp(1.3f, 1f, dist / playerController.pickupRadius);
            transform.localScale = Vector3.one * scaleMult;

            if (dist < 0.5f)
            {
                transform.localScale = Vector3.one;
                playerController.GainXP(xpAmount);

                if (ObjectPool.Instance != null)
                    ObjectPool.Instance.ReturnToPool(gameObject);
                else
                    Destroy(gameObject);
            }
        }
    }
}