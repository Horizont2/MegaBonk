using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    [Header("Settings")]
    public float xpAmount = 10f;
    public float magneticRadius = 4f; // « €коњ в≥дстан≥ кристал почне лет≥ти до гравц€
    public float flySpeed = 12f;

    private Transform player;
    private bool isFlying = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;

        // ѕерев≥р€Їмо в≥дстань до гравц€
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= magneticRadius)
        {
            isFlying = true;
        }

        // якщо кристал намагн≥тивс€, в≥н летить до гравц€
        if (isFlying)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, flySpeed * Time.deltaTime);

            // якщо торкнувс€ гравц€ - додаЇмо досв≥д ≥ знищуЇмо кристал
            if (dist < 0.5f)
            {
                player.GetComponent<PlayerController>().GainXP(xpAmount);
                Destroy(gameObject);
            }
        }
    }
}