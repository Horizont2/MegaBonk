using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ResourceDrop : MonoBehaviour
{
    public enum ResourceType { Wood, Stone, Food, Diamond }

    [Header("Drop Settings")]
    public ResourceType resourceType;
    public int amount = 1;
    public float popForce = 6f;

    [Header("Idle Animation")]
    public float spinSpeed = 120f;

    [Header("Magnet Settings")]
    public float magnetSpeed = 20f;
    [Tooltip("A drop this old is collected on contact regardless of the pickup radius. The failsafe against one wedging somewhere it can never be reached from.")]
    public float giveUpAfter = 25f;
    private bool isMagnetizing = false;

    private Transform player;
    private PlayerController playerController;
    private Rigidbody rb;
    private readonly List<Collider> myColliders = new List<Collider>(4);
    private float bornAt;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        bornAt = Time.time;

        // EVERY collider, including children. The old code took
        // GetComponent<Collider>() — the root one only — so a drop whose collider
        // sits on a child model kept a live, solid collider for its whole life.
        // That is the one that jams against the player's capsule and hangs there:
        // physics will not let it through, the magnet moves it by transform so it
        // cannot push past either, and it never reaches the collect distance.
        GetComponentsInChildren(true, myColliders);

        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 2f, Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDir * popForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * popForce * 2f, ForceMode.Impulse);

        AcquirePlayer();
        Invoke(nameof(StartSpinning), 1.5f);
    }

    // The player may not exist yet when a drop is created — chests and nodes can
    // spawn during a load or a cinematic. Start used to look once and give up,
    // and a drop that missed simply hung forever: Update returns immediately
    // without a player, so it never span, never magnetised and never paid out.
    private void AcquirePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        player = p.transform;
        playerController = p.GetComponent<PlayerController>();

        // Never collide with the player at all. A pickup is not an obstacle, and
        // letting one rest against the capsule is how it ends up parked in mid-air
        // on the player's shoulder.
        foreach (var mine in myColliders)
        {
            if (mine == null) continue;
            foreach (var theirs in p.GetComponentsInChildren<Collider>(true))
            {
                if (theirs == null) continue;
                Physics.IgnoreCollision(mine, theirs, true);
            }
        }
    }

    private void StartSpinning()
    {
        if (!isMagnetizing)
        {
            rb.isKinematic = true;
            foreach (var c in myColliders) if (c != null) c.isTrigger = true;
        }
    }

    private void Update()
    {
        if (player == null || playerController == null)
        {
            // Keep looking rather than giving up for good.
            if (Time.frameCount % 30 == 0) AcquirePlayer();
            return;
        }

        if (rb.isKinematic && !isMagnetizing)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // The failsafe: a drop that has sat around long enough is collected from
        // anywhere within arm's reach, whatever the pickup radius says. A
        // resource the player is standing inside and cannot pick up is worse than
        // one that is slightly too easy to pick up.
        bool stale = Time.time - bornAt > giveUpAfter;

        if (!isMagnetizing && (dist <= playerController.pickupRadius || (stale && dist <= 3f)))
        {
            isMagnetizing = true;
            rb.isKinematic = true;
            foreach (var c in myColliders) if (c != null) c.enabled = false;
        }

        if (isMagnetizing)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, magnetSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, player.position + Vector3.up) < 0.5f)
            {
                Collect();
            }
        }
    }

    // Belt and braces: if the drop somehow overlaps the player as a trigger
    // without the magnet having fired, take it anyway.
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || other == null) return;
        if (!other.CompareTag("Player")) return;
        if (playerController == null) playerController = other.GetComponentInParent<PlayerController>();
        if (playerController != null) Collect();
    }

    private bool isCollected = false;

    private void Collect()
    {
        // Guard against double-collection. Destroy() is deferred to end-of-frame,
        // so without this a drop that lingered a frame (physics settle, low FPS,
        // a second magnet tick) could add its resources more than once — the
        // "one node fills the whole backpack" bug.
        if (isCollected) return;
        isCollected = true;

        if (ResourceManager.Instance != null)
        {
            // ResourceManager.AddRunResources now fires its own toast
            // through GlobalHUD, so we no longer call ShowPickupPopup
            // here — that produced two stacking toasts per pickup.
            if (resourceType == ResourceType.Wood)
            {
                ResourceManager.Instance.AddRunResources(amount, 0, 0);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectItem);
            }
            else if (resourceType == ResourceType.Stone)
            {
                ResourceManager.Instance.AddRunResources(0, amount, 0);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectItem);
            }
            else if (resourceType == ResourceType.Food)
            {
                ResourceManager.Instance.AddRunResources(0, 0, amount);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.Camp_CollectItem);
            }
            else if (resourceType == ResourceType.Diamond && playerController != null)
            {
                playerController.GainDiamond(amount);
            }
        }

        Destroy(gameObject);
    }
}
