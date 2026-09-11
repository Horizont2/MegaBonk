using System.Collections.Generic;
using UnityEngine;

// What the camera finds on its way through the woods.
//
// ==== THE RULE THIS IS BUILT ON ====
//
// Nothing here is staged for the lens. Everything is placed as if it had been
// there before the camera arrived and would still be there after it left: the
// camp is off to one side, the patrol crosses the path without acknowledging it,
// the bones lie where somebody dropped. The camera passes them.
//
// That is the whole difference between a world and a set. A shot that turns to
// present each prop in turn tells the audience a camera operator chose them; a
// shot that drifts past things it does not react to tells them the place is full
// of things and they happened to see four of them.
//
// ==== WHAT EACH PIECE IS FOR ====
//
//   THE CAMP is the disturbing one. Skeletons standing around a lit fire, facing
//   it, not moving. Dead things keeping warm is a far worse thought than dead
//   things attacking, because attacking is what they are supposed to do and this
//   is not. It also puts the one warm light source in an otherwise cold frame.
//
//   THE PATROL crosses the camera's path at middle distance, in file, going
//   somewhere. Purposeful is scarier than hostile: hostile means they have seen
//   you, purposeful means there is somewhere they need to be and a reason for it.
//
//   THE SENTRY does nothing at all until the shot's last beat, when it turns.
//
//   THE REMAINS — skulls, bones, a broken cart — are the only exposition in the
//   shot. They say people came through here, which is what makes skeletons in the
//   same frame read as a consequence rather than as monsters.
//
// Prefabs are taken from the region's own WorldEncounterDirector wherever
// possible, so the shot is dressed with exactly what the game uses rather than a
// parallel set of references that can drift out of step with it.
public class TrailerForestDressing : MonoBehaviour
{
    [Header("Camp (skeletons standing at a fire)")]
    public float campAlongPath = 30f;
    public float campSideOffset = 13f;
    [Range(2, 8)] public int campGuards = 4;
    public float campRingRadius = 2.6f;

    [Header("Patrol (crossing the path, in file)")]
    public float patrolAlongPath = 46f;
    [Range(2, 6)] public int patrolCount = 4;
    public float patrolSpacing = 2.4f;
    public float patrolSpeed = 1.1f;
    [Tooltip("How far to the side the file starts. Negative crosses the other way.")]
    public float patrolStartSide = -16f;

    [Header("Sentry (the shot's reversal)")]
    public float sentryAlongPath = 80f;
    public float sentrySideOffset = 2.5f;

    [Header("Remains")]
    public int boneClusters = 5;
    public float remainsSpread = 22f;

    public TrailerPuppet Sentry { get; private set; }

    private readonly List<TrailerPuppet> puppets = new List<TrailerPuppet>(16);
    private readonly List<GameObject> props = new List<GameObject>(16);

    // `forward` must be the camera's direction of travel and `anchor` the point it
    // passes through — everything is positioned along that line so the dressing
    // lands in frame regardless of where in the world the shot ended up.
    public void Build(Vector3 anchor, Vector3 forward, GameObject[] enemyPrefabs,
                      GameObject campfirePrefab, GameObject[] remainsPrefabs)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, forward);

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[Dressing] No enemy prefabs — the woods will be empty. " +
                             "They are read off the scene's WorldEncounterDirector; check it has enemyPrefabs assigned.");
        }
        else
        {
            BuildCamp(anchor + forward * campAlongPath + side * campSideOffset, enemyPrefabs, campfirePrefab);
            BuildPatrol(anchor + forward * patrolAlongPath, side, enemyPrefabs);
            BuildSentry(anchor + forward * sentryAlongPath + side * sentrySideOffset, forward, enemyPrefabs);
        }

        BuildRemains(anchor + forward * (campAlongPath * 0.6f), forward, side, remainsPrefabs);
    }

    private void BuildCamp(Vector3 centre, GameObject[] enemyPrefabs, GameObject campfirePrefab)
    {
        centre.y = GroundAt(centre);

        if (campfirePrefab != null)
        {
            var fire = Instantiate(campfirePrefab, centre, campfirePrefab.transform.rotation, transform);
            props.Add(fire);
        }

        // An even ring, facing inward. Even because a huddle reads as a mob and a
        // ring reads as a gathering, and the gathering is the unsettling one.
        for (int i = 0; i < campGuards; i++)
        {
            float a = (i / (float)campGuards) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
            Vector3 p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * campRingRadius;
            var puppet = TrailerPuppet.Spawn(Pick(enemyPrefabs), p, Quaternion.identity, transform);
            if (puppet == null) continue;
            puppet.FaceTowards(centre);
            puppet.Stand();
            puppets.Add(puppet);
        }
    }

    private void BuildPatrol(Vector3 crossingPoint, Vector3 side, GameObject[] enemyPrefabs)
    {
        // The file starts well off to one side and walks across, so by the time the
        // camera reaches the crossing they are already in motion — a patrol that
        // starts walking as the camera arrives is a patrol that was waiting for it.
        Vector3 start = crossingPoint + side * patrolStartSide;
        Vector3 walkDir = (side * -Mathf.Sign(patrolStartSide)).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, walkDir).normalized;

        for (int i = 0; i < patrolCount; i++)
        {
            // Each one trails the last, so the file is strung out along its own
            // line of travel rather than abreast.
            Vector3 p = start - walkDir * (i * patrolSpacing);
            // Small sideways wobble: a perfectly straight file is a parade.
            p += lateral * Random.Range(-0.5f, 0.5f);

            var puppet = TrailerPuppet.Spawn(Pick(enemyPrefabs), p, Quaternion.identity, transform);
            if (puppet == null) continue;
            puppet.WalkAlong(walkDir, patrolSpeed * Random.Range(0.92f, 1.08f));
            puppets.Add(puppet);
        }
    }

    private void BuildSentry(Vector3 pos, Vector3 forward, GameObject[] enemyPrefabs)
    {
        var puppet = TrailerPuppet.Spawn(Pick(enemyPrefabs), pos, Quaternion.identity, transform);
        if (puppet == null) return;

        // Facing AWAY down the path. It has its back to the camera for the whole
        // shot, which is what makes the turn at the end land.
        puppet.FaceTowards(pos + forward * 10f);
        puppet.Stand();
        puppets.Add(puppet);
        Sentry = puppet;
    }

    private void BuildRemains(Vector3 centre, Vector3 forward, Vector3 side, GameObject[] remainsPrefabs)
    {
        if (remainsPrefabs == null || remainsPrefabs.Length == 0) return;

        for (int i = 0; i < boneClusters; i++)
        {
            GameObject prefab = Pick(remainsPrefabs);
            if (prefab == null) continue;

            Vector3 p = centre
                      + forward * Random.Range(-remainsSpread, remainsSpread)
                      + side * Random.Range(-remainsSpread * 0.5f, remainsSpread * 0.5f);
            p.y = GroundAt(p);

            var go = Instantiate(prefab, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            // Half-sunk and tilted. Remains lying perfectly flat and upright on the
            // grass look placed; these are supposed to look forgotten.
            go.transform.Rotate(Random.Range(-12f, 12f), 0f, Random.Range(-12f, 12f), Space.Self);
            go.transform.position += Vector3.down * Random.Range(0.02f, 0.14f);
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            props.Add(go);
        }
    }

    public void TearDown()
    {
        for (int i = 0; i < puppets.Count; i++)
            if (puppets[i] != null) Destroy(puppets[i].gameObject);
        for (int i = 0; i < props.Count; i++)
            if (props[i] != null) Destroy(props[i]);
        puppets.Clear();
        props.Clear();
        Sentry = null;
    }

    private static GameObject Pick(GameObject[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }

    private static float GroundAt(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 120f, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        Terrain[] all = Terrain.activeTerrains;
        if (all != null)
        {
            foreach (var t in all)
            {
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position;
                Vector3 s = t.terrainData.size;
                if (pos.x >= o.x && pos.x <= o.x + s.x && pos.z >= o.z && pos.z <= o.z + s.z)
                    return t.SampleHeight(pos) + o.y;
            }
        }
        return pos.y;
    }
}
