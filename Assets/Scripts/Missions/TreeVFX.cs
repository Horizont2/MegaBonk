using UnityEngine;

public class TreeVFX : MonoBehaviour
{
    [Header("VFX Prefabs (Spawned on action)")]
    public GameObject hitVFXPrefab;   // Тріски при ударі
    public GameObject fallDustPrefab; // Хмара пилу при падінні

    public void PlayHitEffect()
    {
        if (hitVFXPrefab != null)
        {
            // Вираховуємо висоту удару: беремо найнижчу точку колайдера і піднімаємо на 1 метр
            Collider col = GetComponent<Collider>();
            Vector3 spawnPos = transform.position + Vector3.up * 1f;
            if (col != null)
            {
                spawnPos = new Vector3(col.bounds.center.x, col.bounds.min.y + 1f, col.bounds.center.z);
            }

            GameObject fx = Instantiate(hitVFXPrefab, spawnPos, Quaternion.identity);

            // Примусово запускаємо ефект, навіть якщо галочка Play On Awake вимкнена
            ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();

            Destroy(fx, 2f);
        }
    }

    public void PlayFallEffect(Vector3 rootPosition)
    {
        if (fallDustPrefab != null)
        {
            GameObject fx = Instantiate(fallDustPrefab, rootPosition + Vector3.up * 0.2f, Quaternion.identity);

            ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();

            Destroy(fx, 4f);
        }
    }
}