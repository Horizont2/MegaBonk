using UnityEngine;

public class MenuCameraParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("How far the camera can move from its center")]
    public float maxOffset = 0.5f;
    [Tooltip("How smooth the movement feels")]
    public float smoothSpeed = 5f;

    private Vector3 startPos;
    private Vector2 screenCenter;

    private void Start()
    {
        // Remember the original position of the camera
        startPos = transform.position;

        // Calculate the exact center of the screen in pixels
        screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    private void Update()
    {
        // 1. Get mouse position relative to the center of the screen
        // This gives us a value from -1 to 1 on both X and Y axes
        float mouseX = (Input.mousePosition.x - screenCenter.x) / screenCenter.x;
        float mouseY = (Input.mousePosition.y - screenCenter.y) / screenCenter.y;

        // 2. Calculate the target position based on mouse offset
        Vector3 targetPos = startPos + new Vector3(mouseX * maxOffset, mouseY * maxOffset, 0f);

        // 3. Smoothly move the camera towards the target position
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}