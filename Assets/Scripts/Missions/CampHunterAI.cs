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

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (carryItemVisual != null) carryItemVisual.SetActive(false);

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
                transform.position = hit.position;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            agent.stoppingDistance = 0.5f;
        }

        StartCoroutine(HunterRoutine());
    }

    private bool CheckIfWorkAvailable()
    {
        if (myBuilding != null && myBuilding.IsVisualsFull()) return false;
        return true;
    }

    private IEnumerator WanderAround()
    {
        // Знімаємо з ручника
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;

        while (true)
        {
            if (CheckIfWorkAvailable()) break;

            if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 8f;
                randomDirection += transform.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, 8f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
            yield return new WaitForSeconds(Random.Range(4f, 8f));
        }
    }

    private IEnumerator HunterRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            if (!CheckIfWorkAvailable())
            {
                if (carryItemVisual != null) carryItemVisual.SetActive(false);
                yield return StartCoroutine(WanderAround());
                continue;
            }

            if (carryItemVisual != null) carryItemVisual.SetActive(false);
            if (!agent.isOnNavMesh) yield break;

            agent.isStopped = false;
            if (lodgePoint != null) agent.SetDestination(lodgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            agent.isStopped = true;
            if (lodgePoint != null) transform.rotation = lodgePoint.rotation;

            if (anim != null) anim.SetTrigger("Work");
            yield return new WaitForSeconds(prepDuration);

            agent.isStopped = false;
            if (forestEdgePoint != null) agent.SetDestination(forestEdgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            if (forestEdgePoint == null || Vector3.Distance(transform.position, forestEdgePoint.position) > 3f)
            {
                Debug.LogWarning("[Hunter AI] Не зміг дійти до лісу. Починаю цикл заново.");
                continue;
            }

            agent.isStopped = true;
            yield return StartCoroutine(VFXTransitionRoutine(false));
            yield return new WaitForSeconds(huntDuration);

            if (carryItemVisual != null) carryItemVisual.SetActive(true);
            yield return StartCoroutine(VFXTransitionRoutine(true));

            agent.isStopped = false;
            if (lodgePoint != null) agent.SetDestination(lodgePoint.position);
            yield return StartCoroutine(WaitForDestination());

            agent.isStopped = true;
            if (lodgePoint != null) transform.rotation = lodgePoint.rotation;

            if (carryItemVisual != null) carryItemVisual.SetActive(false);
            yield return new WaitForSeconds(0.5f);

            if (myBuilding != null) myBuilding.ShowNextVisualResource();
            yield return new WaitForSeconds(1.5f);
        }
    }

    private IEnumerator VFXTransitionRoutine(bool show)
    {
        if (leavesVFX != null)
        {
            GameObject fx = Instantiate(leavesVFX, transform.position + Vector3.up, Quaternion.identity);
            Destroy(fx, 3f);
        }
        yield return new WaitForSeconds(0.3f);
        if (visualsParent != null) visualsParent.SetActive(show);
    }

    private IEnumerator WaitForDestination()
    {
        float timeout = 0f;
        while (timeout < 20f)
        {
            timeout += Time.deltaTime;
            if (agent != null && agent.isOnNavMesh && !agent.pathPending)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial) break;
                if (agent.remainingDistance <= agent.stoppingDistance + 0.1f) break;
            }
            yield return null;
        }
    }
}