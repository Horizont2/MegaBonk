using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player;
    public float cameraHeight = 50f;
    public bool rotateWithPlayer = true; // Чи має мапа крутитися

    private void LateUpdate()
    {
        if (player == null) return;

        // Слідуємо за позицією
        transform.position = new Vector3(player.position.x, player.position.y + cameraHeight, player.position.z);

        if (rotateWithPlayer)
        {
            // Камера мінімапи крутиться так само, як і гравець/камера навколо осі Y
            transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}