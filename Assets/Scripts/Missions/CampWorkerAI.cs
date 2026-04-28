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

    [Header("Distances (NEW)")]
    [Tooltip("Як близько підходити до дерева")]
    public float workDistance = 0.8f;
    [Tooltip("Як близько підходити до купи ресурсів")]
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

        StartCoroutine(WorkerRoutine());
    }

    private void Update()
    {
        if (anim != null && agent != null)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private IEnumerator WorkerRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            // --- 1. ШУКАЄМО НАЙБЛИЖЧЕ ДЕРЕВО ---
            CampTree targetTree = FindNearestTree();

            if (targetTree == null)
            {
                yield return new WaitForSeconds(2f);
                continue;
            }

            if (carryItemVisual != null) carryItemVisual.SetActive(false);
            agent.isStopped = false;
            agent.stoppingDistance = workDistance; // Встановлюємо дистанцію до дерева
            agent.SetDestination(targetTree.transform.position);

            float timeout = 0f;
            while (timeout < 15f)
            {
                timeout += Time.deltaTime;
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    break;
                }
                yield return null;
            }

            // --- 2. РУБАЄ ДЕРЕВО ---
            agent.isStopped = true;

            if (targetTree != null && !targetTree.isChopped)
            {
                while (targetTree != null && !targetTree.isChopped)
                {
                    // Жорстко фіксуємо поворот на дерево перед кожним ударом
                    Vector3 lookPos = targetTree.transform.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);

                    if (anim != null) anim.SetTrigger("Work");

                    yield return new WaitForSeconds(timeBetweenHits);

                    if (targetTree != null && !targetTree.isChopped)
                    {
                        targetTree.TakeHit();
                    }
                }

                yield return new WaitForSeconds(1f);
            }

            // --- 3. НЕСЕ НА СКЛАД ---
            if (carryItemVisual != null) carryItemVisual.SetActive(true);
            agent.isStopped = false;
            agent.stoppingDistance = dropDistance; // Встановлюємо дистанцію до складу

            if (dropPoint != null) agent.SetDestination(dropPoint.position);

            timeout = 0f;
            while (dropPoint != null && timeout < 15f)
            {
                timeout += Time.deltaTime;
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    break;
                }
                yield return null;
            }

            // --- 4. СКИДАЄ КОЛОДУ ---
            agent.isStopped = true;
            if (dropPoint != null) transform.rotation = dropPoint.rotation;

            yield return new WaitForSeconds(dropDuration);

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