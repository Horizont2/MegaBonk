using UnityEngine;

// Requires a Collider on the same object to detect mouse clicks
[RequireComponent(typeof(Collider))]
public class MenuCharacterSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinSpeed = 500f; // Multiplier for rotation speed

    // This method is called automatically by Unity while the mouse is dragged over the Collider
    private void OnMouseDrag()
    {
        // Get the horizontal movement of the mouse
        float horizontalInput = Input.GetAxis("Mouse X");

        // Rotate the character around its Y (vertical) axis
        // The negative sign ensures the character spins exactly in the direction of the drag
        transform.Rotate(Vector3.up, -horizontalInput * spinSpeed * Time.deltaTime, Space.World);
    }
}