using UnityEngine;

public class CameraCulling : MonoBehaviour
{
    public float natureRenderDistance = 150f; // Відстань, на якій зникають дерева/кущі

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float[] distances = new float[32];

        // Встановлюємо дистанцію тільки для шару "Nature" (переконайся, що це шар номер 8 або заміни індекс)
        distances[LayerMask.NameToLayer("Nature")] = natureRenderDistance;

        cam.layerCullDistances = distances;
    }
}