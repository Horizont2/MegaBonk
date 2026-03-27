using UnityEngine;

public class WeaponOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("How many degrees per second the weapon rotates.")]
    public float rotationSpeed = 180f;

    private void Update()
    {
        // Rotate the pivot around its Y axis
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}