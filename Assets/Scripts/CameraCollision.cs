using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform player;
    public float minDistance = 1.0f;
    public float maxDistance = 10.0f;
    public float smoothSpeed = 10.0f;
    public Vector3 dollyDir;
    public float distance;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;

    [Header("Cinematic Bridge")]
    public bool isCinematicMode = false; // Нове: дозволяє вимикати колізії для катсцен

    void Awake()
    {
        dollyDir = transform.localPosition.normalized;
        distance = transform.localPosition.magnitude;
    }

    void Update()
    {
        // Якщо увімкнено режим кіно — нічого не робимо, щоб не заважати Cinemachine
        if (isCinematicMode) return;

        if (transform.parent == null) return;

        Vector3 desiredCameraPos = transform.parent.TransformPoint(dollyDir * maxDistance);
        RaycastHit hit;

        if (Physics.Linecast(transform.parent.position, desiredCameraPos, out hit, collisionLayers))
        {
            distance = Mathf.Clamp((hit.distance * 0.8f), minDistance, maxDistance);
        }
        else
        {
            distance = maxDistance;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, dollyDir * distance, Time.deltaTime * smoothSpeed);
    }
}