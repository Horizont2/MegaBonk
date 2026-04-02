using UnityEngine;

public class XpCrystal : MonoBehaviour
{
    [Header("Settings")]
    public float xpAmount = 10f;
    public float magnetSpeed = 15f;

    private Transform player;
    private PlayerController playerController;
    private bool isMagnetized = false;

    private void Start()
    {
        // «находимо гравц€ один раз при по€в≥ кристала
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (player == null || playerController == null) return;

        // 1. ѕерев≥р€Їмо, чи гравець зайшов у рад≥ус збору (ћагн≥т)
        if (!isMagnetized && Vector3.Distance(transform.position, player.position) <= playerController.pickupRadius)
        {
            isMagnetized = true;
        }

        // 2. якщо магн≥т активний, кристал летить до гравц€
        if (isMagnetized)
        {
            // Ћетимо до центру гравц€ (трохи вище його н≥г)
            Vector3 targetPos = player.position + Vector3.up * 1f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);

            // 3. «бираЇмо досв≥д, коли кристал майже торкнувс€ гравц€
            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                playerController.GainXP(xpAmount);
                Destroy(gameObject);
            }
        }
    }
}