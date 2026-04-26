using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Shop Return Point")]
    public Transform shopDoorSpawnPoint; // —твори пустий об'Їкт б≥л€ дверей ≥ перет€гни сюди

    private void Start()
    {
        // ѕерев≥р€Їмо, чи повертаЇмос€ ми з магазину
        if (PlayerPrefs.GetInt("ReturningFromShop", 0) == 1)
        {
            // якщо так - телепортуЇмо гравц€ до дверей
            if (shopDoorSpawnPoint != null)
            {
                // ¬имикаЇмо CharacterController (€кщо в≥н Ї), бо в≥н блокуЇ телепортац≥ю
                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                transform.position = shopDoorSpawnPoint.position;
                transform.rotation = shopDoorSpawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }

            // «кидаЇмо м≥тку, щоб при наступному заход≥ в гру ми з'€вл€лис€ на звичайному м≥сц≥
            PlayerPrefs.SetInt("ReturningFromShop", 0);
            PlayerPrefs.Save();
        }
    }
}