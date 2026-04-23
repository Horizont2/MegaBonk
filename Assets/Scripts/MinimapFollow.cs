using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform player;
    public float cameraHeight = 30f;

    private void Start()
    {
        // Відкріплюємо камеру від гравця, якщо вона була всередині нього
        transform.parent = null;
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            // Камера просто висить над гравцем
            transform.position = new Vector3(player.position.x, player.position.y + cameraHeight, player.position.z);

            // Завжди дивиться вниз (на 90 градусів) і ніколи не крутиться!
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}