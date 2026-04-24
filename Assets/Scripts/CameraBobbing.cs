using UnityEngine;

public class CameraBobbing : MonoBehaviour
{
    public float speed = 0.5f;
    public float amount = 0.1f;
    private Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amount;
        transform.position = new Vector3(transform.position.x, newY, startPos.z);
    }
}