using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CampWorkerAI : MonoBehaviour
{
    [Header("Link to Building")]
    public CampBuilding myBuilding;

    [Header("Locations")]
    public Transform dropPoint;
    public float searchRadius = 30f;

    [Header("Distances")]
    public float workDistance = 0.8f;
    public float dropDistance = 1.0f;

    [Header("Timings")]
    public float timeBetweenHits = 1.2f;
    public float dropDuration = 2f;

    [Header("Visuals & Animation")]
    public Animator anim;
    public GameObject carryItemVisual;

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
            agent.stoppingDistance = workDistance;
        }

        StartCoroutine(WorkerRoutine());
    }

    private bool CheckIfWorkAvailable()
    {
        if (myBuilding != null && myBuilding.IsVisualsFull()) return false;
        return FindNearestTree() != null;
    }

    private IEnumerator WanderAround()
    {
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

    private IEnumerator WorkerRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            if (!CheckIfWorkAvailable())
            {
                yield return StartCoroutine(WanderAround());
                continue;
            }

            CampTree targetTree = FindNearestTree();
            if (targetTree == null) { yield return new WaitForSeconds(2f); continue; }

            if (carryItemVisual != null) carryItemVisual.SetActive(false);
            if (!agent.isOnNavMesh) yield break;

            agent.isStopped = false;
            agent.stoppingDistance = workDistance;
            agent.SetDestination(targetTree.transform.position);

            float timeout = 0f;
            while (timeout < 15f)
            {
                timeout += Time.deltaTime;
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f) break;
                yield return null;
            }

            agent.isStopped = true;

            if (targetTree != null && !targetTree.isChopped)
            {
                while (targetTree != null && !targetTree.isChopped)
                {
                    Vector3 lookPos = targetTree.transform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);

                    if (anim != null) anim.SetTrigger("Work");

                    // ЗВУК: Робота NPC (рубка дерева)
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioID.NPC_Work);

                    yield return new WaitForSeconds(timeBetweenHits);

                    if (targetTree != null && !targetTree.isChopped) targetTree.TakeHit();
                }
                yield return new WaitForSeconds(1f);
            }

            if (myBuilding != null && myBuilding.IsVisualsFull())
            {
                if (carryItemVisual != null) carryItemVisual.SetActive(false);
                continue;
            }

            if (carryItemVisual != null) carryItemVisual.SetActive(true);
            agent.isStopped = false;
            agent.stoppingDistance = dropDistance;

            if (dropPoint != null) agent.SetDestination(dropPoint.position);

            timeout = 0f;
            while (dropPoint != null && timeout < 15f)
            {
                timeout += Time.deltaTime;
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f) break;
                yield return null;
            }

            agent.isStopped = true;
            if (dropPoint != null) transform.rotation = dropPoint.rotation;

            yield return new WaitForSeconds(dropDuration);

            if (carryItemVisual != null) carryItemVisual.SetActive(false);
            if (myBuilding != null) myBuilding.ShowNextVisualResource();
        }
    }

    private CampTree FindNearestTree()
    {
        CampTree[] allTrees = Object.FindObjectsByType<CampTree>(FindObjectsSortMode.None);
        CampTree nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (CampTree tree in allTrees)
        {
            if (tree.isChopped) continue;
            float dist = Vector3.Distance(transform.position, tree.transform.position);
            if (dist < minDistance && dist <= searchRadius)
            {
                minDistance = dist;
                nearest = tree;
            }
        }
        return nearest;
    }
}