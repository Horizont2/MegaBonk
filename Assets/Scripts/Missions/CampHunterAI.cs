using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CampHunterAI : MonoBehaviour
{
    [Header("Link to Building")]
    public CampBuilding myBuilding;

    [Header("Locations")]
    public Transform lodgePoint;
    public Transform forestEdgePoint;

    [Header("Timings")]
    public float prepDuration = 5f;
    public float huntDuration = 15f;

    [Header("Visuals & Animation")]
    public GameObject visualsParent;
    public Animator anim;
    public GameObject carryItemVisual;

    [Header("Effects")]
    public GameObject leavesVFX;

    private NavMeshAgent agent;
    private Vector3 originalVisualsScale = Vector3.one; // Запам'ятовуємо розмір модельки

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (carryItemVisual != null) carryItemVisual.SetActive(false);

        // Зберігаємо оригінальний масштаб (наприклад, 0.8)
        if (visualsParent != null) originalVisualsScale = visualsParent.transform.localScale;

        StartCoroutine(InitAndStartRoutine());
    }

    private void Update()
    {
        if (anim != null && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private IEnumerator InitAndStartRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (transform.position.y < -2f)
        {
            if (agent != null) agent.enabled = false;

            yield return new WaitForSeconds(2.5f);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            agent.stoppingDistance = 0.5f;
        }

        StartCoroutine(HunterRoutine());
    }

    private IEnumerator HunterRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            // --- 1. ПІДГОТОВКА ВДОМА ---
            if (carryItemVisual != null) carryItemVisual.SetActive(false);

            if (!agent.isOnNavMesh) yield break;

            agent.isStopped = false;
            if (lodgePoint != null) agent.SetDestination(lodgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            agent.isStopped = true;
            if (lodgePoint != null) transform.rotation = lodgePoint.rotation;

            if (anim != null) anim.SetTrigger("Work");
            yield return new WaitForSeconds(prepDuration);

            // --- 2. ЙДЕ В ЛІС ---
            agent.isStopped = false;
            if (forestEdgePoint != null) agent.SetDestination(forestEdgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            if (forestEdgePoint == null || Vector3.Distance(transform.position, forestEdgePoint.position) > 3f)
            {
                Debug.LogWarning("[Hunter AI] Не зміг дійти до лісу. Починаю цикл заново.");
                continue;
            }

            // --- 3. ЗНИКАЄ (ПЛАВНО) ---
            agent.isStopped = true;

            if (leavesVFX != null)
            {
                GameObject fx = Instantiate(leavesVFX, transform.position + Vector3.up, Quaternion.identity);
                Destroy(fx, 3f);
            }

            // Викликаємо плавне зменшення замість різкого SetActive(false)
            yield return StartCoroutine(FadeVisualsRoutine(false));

            yield return new WaitForSeconds(huntDuration);

            // --- 4. ПОВЕРТАЄТЬСЯ (ПЛАВНО) ---
            if (carryItemVisual != null) carryItemVisual.SetActive(true);

            if (leavesVFX != null)
            {
                GameObject fx = Instantiate(leavesVFX, transform.position + Vector3.up, Quaternion.identity);
                Destroy(fx, 3f);
            }

            // Викликаємо плавне збільшення назад
            yield return StartCoroutine(FadeVisualsRoutine(true));

            // --- 5. НЕСЕ М'ЯСО ДОДОМУ ---
            agent.isStopped = false;
            if (lodgePoint != null) agent.SetDestination(lodgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            agent.isStopped = true;
            if (lodgePoint != null) transform.rotation = lodgePoint.rotation;

            yield return new WaitForSeconds(2f);

            if (myBuilding != null) myBuilding.ShowNextVisualResource();
        }
    }

    // --- НОВА КОРУТИНА ПЛАВНОГО ЗНИКНЕННЯ/ПОЯВИ ---
    private IEnumerator FadeVisualsRoutine(bool show)
    {
        if (visualsParent == null) yield break;

        float duration = 0.4f; // Тривалість анімації зникнення (0.4 секунди)
        float elapsed = 0f;

        Vector3 startScale = show ? Vector3.zero : originalVisualsScale;
        Vector3 targetScale = show ? originalVisualsScale : Vector3.zero;

        // Якщо маємо з'явитися, спочатку вмикаємо об'єкт
        if (show) visualsParent.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Плавно змінюємо розмір
            visualsParent.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }

        visualsParent.transform.localScale = targetScale;

        // Якщо мали зникнути, повністю вимикаємо об'єкт у кінці
        if (!show) visualsParent.SetActive(false);
    }

    private IEnumerator WaitForDestination()
    {
        float timeout = 0f;
        while (timeout < 20f)
        {
            timeout += Time.deltaTime;

            if (agent != null && agent.isOnNavMesh && !agent.pathPending)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    break;
                }

                if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    break;
                }
            }
            yield return null;
        }
    }
}