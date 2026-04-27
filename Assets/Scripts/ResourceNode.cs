using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    public enum NodeType { Tree, Rock, Barrel }

    [Header("Type Settings")]
    public NodeType nodeType = NodeType.Tree;
    public float minHealth = 50f;  // НОВЕ: Мінімальне ХП
    public float maxHealth = 200f; // НОВЕ: Максимальне ХП

    [Header("Drops")]
    public GameObject dropPrefab;
    public int minDrops = 2;
    public int maxDrops = 5;

    [Header("Effects")]
    public ParticleSystem hitEffect;
    public GameObject stumpPrefab;

    private float currentHealth;
    private float actualMaxHealth; // Справжнє ХП цього конкретного об'єкта
    private Vector3 originalScale;
    private bool isDead = false;

    private void Start()
    {
        // При старті генеруємо випадкове ХП для цього об'єкта
        actualMaxHealth = Random.Range(minHealth, maxHealth);
        currentHealth = actualMaxHealth;

        originalScale = transform.localScale;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // Випускаємо пил/іскри при кожному ударі
        if (hitEffect != null) hitEffect.Play();

        StopAllCoroutines();

        if (currentHealth <= 0)
        {
            isDead = true;
            StartCoroutine(DeathRoutine());
        }
        else
        {
            // РОЗУМНА АНІМАЦІЯ ЗАЛЕЖНО ВІД ТИПУ
            if (nodeType == NodeType.Rock)
            {
                // Камінь фізично зменшується
                float healthPercent = currentHealth / maxHealth;
                Vector3 targetScale = originalScale * Mathf.Max(0.4f, healthPercent); // Зменшується максимум до 40%
                StartCoroutine(SquishRoutine(targetScale));
            }
            else
            {
                // Дерева і бочки хитаються від удару
                StartCoroutine(WobbleRoutine());
            }
        }
    }

    private IEnumerator SquishRoutine(Vector3 targetScale)
    {
        float t = 0;
        Vector3 squishScale = new Vector3(targetScale.x * 1.1f, targetScale.y * 0.9f, targetScale.z * 1.1f);
        while (t < 1) { t += Time.deltaTime * 15f; transform.localScale = Vector3.Lerp(transform.localScale, squishScale, t); yield return null; }
        t = 0;
        while (t < 1) { t += Time.deltaTime * 10f; transform.localScale = Vector3.Lerp(squishScale, targetScale, t); yield return null; }
        transform.localScale = targetScale;
    }

    private IEnumerator WobbleRoutine()
    {
        float t = 0;
        Vector3 squishScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 0.9f, originalScale.z * 1.1f);
        while (t < 1) { t += Time.deltaTime * 15f; transform.localScale = Vector3.Lerp(originalScale, squishScale, t); yield return null; }
        t = 0;
        while (t < 1) { t += Time.deltaTime * 10f; transform.localScale = Vector3.Lerp(squishScale, originalScale, t); yield return null; }
        transform.localScale = originalScale;
    }

    private IEnumerator DeathRoutine()
    {
        // 1. Викидаємо лут
        int dropCount = Random.Range(minDrops, maxDrops + 1);
        for (int i = 0; i < dropCount; i++)
        {
            if (dropPrefab != null) Instantiate(dropPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        }

        // 2. Анімація смерті
        if (nodeType == NodeType.Tree)
        {
            // РОЗУМНЕ ПАДІННЯ: Шукаємо найнижчу точку (корінь)
            Vector3 pivotPoint = transform.position;
            Collider col = GetComponentInChildren<Collider>();
            if (col != null) pivotPoint.y = col.bounds.min.y;

            float fallDuration = 0.5f; // Падає півсекунди
            float fallSpeed = 90f / fallDuration; // Швидкість (90 градусів)
            float t = 0;

            while (t < fallDuration)
            {
                t += Time.deltaTime;
                // Крутимо дерево рівно навколо його кореня!
                transform.RotateAround(pivotPoint, transform.right, fallSpeed * Time.deltaTime);
                yield return null;
            }

            if (hitEffect != null) hitEffect.Play();
            if (stumpPrefab != null) Instantiate(stumpPrefab, pivotPoint, transform.rotation);

            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }
        else
        {
            // КАМІНЬ АБО БОЧКА
            if (hitEffect != null) hitEffect.Play();

            if (GetComponentInChildren<MeshRenderer>() != null) GetComponentInChildren<MeshRenderer>().enabled = false;
            if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;

            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }
    }
}