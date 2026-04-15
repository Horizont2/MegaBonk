using UnityEngine;

public class CampfireInteract : MonoBehaviour
{
    [Header("Heal Settings")]
    public float healPerSecond = 10f;
    public float healRadius = 5f;

    [Header("Visual Effects")]
    public ParticleSystem healEffect;

    private PlayerController player;

    private void Start()
    {
        // Ѕагатт€ знаходить гравц€ 1 раз при старт≥ гри (н≥€ких тригер≥в)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
        }

        if (healEffect != null) healEffect.Stop();
    }

    private void Update()
    {
        // якщо гравц€ немаЇ - н≥чого не робимо
        if (player == null) return;

        // ћј“≈ћј“» ј: ћ≥р€Їмо точну в≥дстань в≥д багатт€ до гравц€
        float distance = Vector3.Distance(transform.position, player.transform.position);

        // якщо гравець достатньо близько (рад≥ус)
        if (distance <= healRadius)
        {
            // 1. Ћ≥куЇмо
            player.Heal(healPerSecond * Time.deltaTime);

            // 2. ѕрив'€зуЇмо в≥зуальний ефект до гравц€
            if (healEffect != null)
            {
                // ≈фект завжди летить п≥д ноги гравцю
                healEffect.transform.position = player.transform.position + Vector3.up * 0.2f;

                if (!healEffect.isPlaying)
                {
                    healEffect.Play();
                }
            }
        }
        else
        {
            // √равець в≥д≥йшов в≥д багатт€ - вимикаЇмо ефект
            if (healEffect != null && healEffect.isPlaying)
            {
                healEffect.Stop();
            }
        }
    }

    // ћалюЇмо зелену сферу в редактор≥ Unity, щоб ти м≥г налаштувати healRadius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}