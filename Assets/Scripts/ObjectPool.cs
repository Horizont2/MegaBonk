using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> prefabLookup = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Pre-warm the pool with a number of inactive instances.
    /// </summary>
    public void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            pools[prefab].Enqueue(obj);
            prefabLookup[obj] = prefab;
        }
    }

    /// <summary>
    /// Get an object from the pool (or create one if pool is empty).
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();

        GameObject obj;

        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();

            // Safety: if the pooled object was destroyed externally, create a new one
            if (obj == null)
                return CreateNew(prefab, position, rotation);

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = CreateNew(prefab, position, rotation);
        }

        return obj;
    }

    /// <summary>
    /// Return an object to the pool instead of destroying it.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);

        if (prefabLookup.TryGetValue(obj, out GameObject prefab))
        {
            pools[prefab].Enqueue(obj);
        }
        else
        {
            // Unknown object, just deactivate it
            obj.SetActive(false);
        }
    }

    private GameObject CreateNew(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(prefab, position, rotation);
        prefabLookup[obj] = prefab;
        return obj;
    }
}
