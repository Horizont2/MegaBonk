using UnityEngine;
using System.Collections;

public class LootChest : MonoBehaviour
{
    [Header("References")]
    public Animator chestAnimator;

    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Shake Settings")]
    public float shakeDuration = 0.6f;
    public float shakeAmount = 0.15f;

    [Header("Loot Settings")]
    public GameObject[] possibleLoot;
    public int minLootItems = 3;
    public int maxLootItems = 6;

    [Tooltip("Затримка перед вильотом луту (збільш це значення, щоб ресурси вилітали пізніше)")]
    public float delayForLoot = 1.5f;

    [Header("Destruction")]
    public float destroyDelay = 10f; // Через скільки секунд після відкриття скриня зникне

    private bool isInteracted = false;
    private Transform player;
    private Vector3 originalPos;

    private void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;

        originalPos = transform.position;

        if (chestAnimator == null) chestAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isInteracted || player == null) return;

        if (Vector3.Distance(transform.position, player.position) <= interactRange)
        {
            if (Input.GetKeyDown(interactKey))
            {
                StartCoroutine(OpenSequence());
            }
        }
    }

    private IEnumerator OpenSequence()
    {
        isInteracted = true;

        // 1. ТРЯСКА
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        // 2. АНІМАЦІЯ ВІДКРИТТЯ
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
        }

        // 3. ПАУЗА ПЕРЕД ВИЛЬОТОМ (Ми її збільшили)
        // Поки йде ця пауза, програється анімація відкриття кришки
        yield return new WaitForSeconds(delayForLoot);

        // 4. ВИЛІТ РЕСУРСІВ
        SpawnLoot();

        // 5. САМОЗНИЩЕННЯ
        // Скриня просто видалиться зі сцени через 10 секунд
        Destroy(gameObject, destroyDelay);
    }

    private void SpawnLoot()
    {
        int count = Random.Range(minLootItems, maxLootItems + 1);
        for (int i = 0; i < count; i++)
        {
            if (possibleLoot.Length > 0)
            {
                GameObject loot = possibleLoot[Random.Range(0, possibleLoot.Length)];
                Instantiate(loot, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}