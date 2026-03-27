using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    [Header("Settings")]
    public float xpAmount = 10f;
    public float flySpeed = 12f;

    private Transform playerTransform;
    private PlayerController playerController;
    private bool isFlying = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (playerTransform == null || playerController == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (!isFlying && dist <= playerController.pickupRadius)
        {
            isFlying = true;
        }

        if (isFlying)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, flySpeed * Time.deltaTime);

            if (dist < 0.5f)
            {
                playerController.GainXP(xpAmount);
                Destroy(gameObject);
            }
        }
    }
}