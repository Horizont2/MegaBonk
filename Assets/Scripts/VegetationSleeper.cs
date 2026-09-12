using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Makes a tree cheap while nobody is near it, and whole again when they are.
//
// ==== WHY THIS EXISTS ALONGSIDE THE HYDRATOR ====
//
// VegetationHydrator solves the procedural case: thousands of trees PAINTED
// onto the terrain, of which a few become real objects near the player. That is
// the right answer when the generator decides where trees go.
//
// It is the wrong answer for a hand-built level. In Lvl_1 the trees were placed
// by a person, one at a time, exactly where they wanted them — there is nothing
// to paint and nothing to hydrate, because the objects ARE the level. So this
// works from the other end: leave every object exactly where it is, and strip
// the expensive parts off the ones nobody is standing near.
//
// ==== WHAT IS ACTUALLY EXPENSIVE, AND WHAT IS NOT ====
//
// Worth stating, because the obvious answer is wrong. A distant tree's script
// costs nothing — ResourceNode has no Update; it only reacts to being hit. The
// real costs are SHADOW CASTING, which makes every tree a second draw into the
// shadow map whether or not it is near anything, and COLLIDERS, which sit in
// the physics broadphase forever. Neither does anything useful for a tree
// forty metres away that the player cannot reach or interact with.
//
// So a sleeping tree keeps its mesh — it must, it is scenery — and gives up its
// shadow, its collider, its particle systems and its script. It is still there,
// still exactly where the designer put it, and still choppable the instant the
// player walks over.
//
// ==== WHY IT IS SAFE ====
//
// The wake radius is far beyond anything the player can reach or hit, so a tree
// is never asleep at a moment its collider or its ResourceNode could matter.
// Everything is restored on wake, and the component touches nothing it did not
// switch off itself — a tree whose shadows were already off keeps them off.
[DisallowMultipleComponent]
public class VegetationSleeper : MonoBehaviour
{
    public static VegetationSleeper Instance { get; private set; }

    [Header("Range")]
    [Tooltip("Metres within which a node is fully awake: collider, script, shadow, effects. Comfortably beyond the player's reach, so nothing is ever asleep at a moment it could matter.")]
    public float wakeRadius = 45f;
    [Tooltip("Extra metres before an awake node goes back to sleep. Without the gap, a tree on the boundary toggles every time the player shifts their weight.")]
    public float sleepHysteresis = 12f;

    [Header("What sleeping gives up")]
    [Tooltip("The big one. A distant tree draws a second time into the shadow map for nothing.")]
    public bool dropShadows = true;
    [Tooltip("Colliders on unreachable trees sit in the physics broadphase costing a little, forever.")]
    public bool dropColliders = true;
    [Tooltip("Leaf rustle, dust, fireflies — none of it is visible or audible from across the map.")]
    public bool dropEffects = true;

    [Header("Budget")]
    [Tooltip("Most nodes to change state in one tick, so walking into a dense wood spreads across frames instead of hitching once.")]
    public int maxChangesPerTick = 24;
    public float tickInterval = 0.35f;

    [Header("Diagnostics")]
    public bool logSummary = true;

    private class Node
    {
        public ResourceNode script;
        public Transform tf;
        public Renderer[] renderers;
        public UnityEngine.Rendering.ShadowCastingMode[] shadowWas;
        public Collider[] colliders;
        public bool[] colliderWas;
        public ParticleSystem[] effects;
        public bool asleep;
    }

    private readonly List<Node> _nodes = new List<Node>(512);
    private readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>(256);
    private float _cell;
    private Transform _player;

    // PER SCENE, NOT ONCE PER SESSION.
    //
    // A plain RuntimeInitializeOnLoadMethod fires in whichever scene starts
    // first and never again — the trap that has already cost this project the
    // exploration director and the shop walkthrough. Subscribing to sceneLoaded
    // from it is the version that actually runs everywhere.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode mode) => Install();

    private static void Install()
    {
        if (Instance != null) return;
        new GameObject("[VegetationSleeper]").AddComponent<VegetationSleeper>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        // A frame for the scene's own Start methods, and for a procedural world
        // to have finished putting its trees down. Scanning before that finds
        // an empty scene and sleeps forever.
        yield return null;
        float deadline = Time.realtimeSinceStartup + 90f;
        while (!WorldGenerator.IsGenerationDone && Time.realtimeSinceStartup < deadline
               && FindFirstObjectByType<WorldGenerator>() != null)
            yield return null;
        yield return null;

        Collect();
        if (_nodes.Count == 0)
        {
            if (logSummary) Debug.Log("[Sleeper] No resource nodes in this scene — standing down.");
            Destroy(gameObject);
            yield break;
        }

        BuildGrid();
        if (logSummary)
            Debug.Log($"[Sleeper] Managing {_nodes.Count} nodes. Beyond {wakeRadius:0}m they give up " +
                      $"{(dropShadows ? "shadows " : "")}{(dropColliders ? "colliders " : "")}" +
                      $"{(dropEffects ? "effects" : "")}.");

        // Everything starts asleep. The alternative — waking the world and then
        // putting it back — means the first second of every level pays the full
        // cost of a scene this exists to avoid paying for.
        for (int i = 0; i < _nodes.Count; i++) SetAsleep(_nodes[i], true);

        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player != null) Tick();
            yield return wait;
        }
    }

    private void Collect()
    {
        _nodes.Clear();
        foreach (var rn in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (rn == null) continue;
            var n = new Node
            {
                script = rn,
                tf = rn.transform,
                renderers = rn.GetComponentsInChildren<Renderer>(true),
                colliders = rn.GetComponentsInChildren<Collider>(true),
                effects = dropEffects ? rn.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0],
            };

            // Remember the AUTHORED state, so waking restores what the designer
            // set rather than what this component assumes. A tree deliberately
            // shipped without shadows stays without them.
            n.shadowWas = new UnityEngine.Rendering.ShadowCastingMode[n.renderers.Length];
            for (int i = 0; i < n.renderers.Length; i++)
                n.shadowWas[i] = n.renderers[i] != null ? n.renderers[i].shadowCastingMode
                                                        : UnityEngine.Rendering.ShadowCastingMode.Off;
            n.colliderWas = new bool[n.colliders.Length];
            for (int i = 0; i < n.colliders.Length; i++)
                n.colliderWas[i] = n.colliders[i] != null && n.colliders[i].enabled;

            _nodes.Add(n);
        }
    }

    private void BuildGrid()
    {
        _cell = Mathf.Max(12f, wakeRadius);
        _grid.Clear();
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].tf == null) continue;
            long key = CellKey(_nodes[i].tf.position.x, _nodes[i].tf.position.z);
            if (!_grid.TryGetValue(key, out var list)) _grid[key] = list = new List<int>(16);
            list.Add(i);
        }
    }

    private long CellKey(float x, float z)
    {
        int cx = Mathf.FloorToInt(x / _cell);
        int cz = Mathf.FloorToInt(z / _cell);
        return ((long)cx << 32) ^ (uint)cz;
    }

    private readonly HashSet<int> _nearby = new HashSet<int>();

    private void Tick()
    {
        Vector3 pos = _player.position;
        float wakeSqr = wakeRadius * wakeRadius;
        float sleepSqr = (wakeRadius + sleepHysteresis) * (wakeRadius + sleepHysteresis);
        int budget = maxChangesPerTick;

        // WAKE what is close. Only the nine cells around the player are looked
        // at — the nodes never move, so a grid built once answers this in a few
        // dozen checks instead of a distance test against every tree in the
        // level on every tick.
        _nearby.Clear();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                long key = CellKey(pos.x + dx * _cell, pos.z + dz * _cell);
                if (!_grid.TryGetValue(key, out var list)) continue;

                for (int n = 0; n < list.Count; n++)
                {
                    var node = _nodes[list[n]];
                    if (node.tf == null) continue;
                    if ((node.tf.position - pos).sqrMagnitude > wakeSqr) continue;

                    _nearby.Add(list[n]);
                    if (node.asleep && budget > 0) { SetAsleep(node, false); budget--; }
                }
            }
        }

        // PUT BACK TO SLEEP anything awake that the player has left behind. The
        // whole node list is walked here rather than the grid, because a node
        // the player has walked away from is by definition no longer in a cell
        // the grid query visits.
        for (int i = 0; i < _nodes.Count && budget > 0; i++)
        {
            var node = _nodes[i];
            if (node.asleep || node.tf == null) continue;
            if (_nearby.Contains(i)) continue;
            if ((node.tf.position - pos).sqrMagnitude < sleepSqr) continue;
            SetAsleep(node, true);
            budget--;
        }
    }

    private void SetAsleep(Node n, bool sleep)
    {
        n.asleep = sleep;

        // The MESH IS NEVER TOUCHED. A tree that vanishes when the player looks
        // away is not an optimisation, it is a bug — everything below is about
        // what the tree costs, never about whether it is there.
        if (dropShadows)
        {
            for (int i = 0; i < n.renderers.Length; i++)
            {
                if (n.renderers[i] == null) continue;
                n.renderers[i].shadowCastingMode = sleep
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : n.shadowWas[i];
            }
        }

        if (dropColliders)
        {
            for (int i = 0; i < n.colliders.Length; i++)
            {
                if (n.colliders[i] == null) continue;
                // Only ever re-enable what was enabled to begin with: a trigger
                // volume somebody deliberately switched off must stay off.
                n.colliders[i].enabled = sleep ? false : n.colliderWas[i];
            }
        }

        if (dropEffects)
        {
            for (int i = 0; i < n.effects.Length; i++)
            {
                if (n.effects[i] == null) continue;
                if (sleep) n.effects[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // Not restarted on wake: these are HIT effects, played on
                // contact. Starting them because the player walked past would
                // make every tree in the wood puff dust at them.
            }
        }

        if (n.script != null) n.script.enabled = !sleep;
    }
}
