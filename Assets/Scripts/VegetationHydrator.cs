using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lets every tree be painted onto the terrain AND still be chopped down.
//
// ==== THE PROBLEM THIS SOLVES ====
//
// Terrain trees are enormously cheaper than GameObjects — Unity batches them,
// LODs them and billboards them for free, and a forest of three thousand costs
// less than a hundred loose objects. But a TreeInstance is a row in an array,
// not an object: it cannot carry a ResourceNode, a particle system or a script
// of any kind. That is why terrain painting is switched off in this project and
// every tree in the world is a real GameObject paying full price.
//
// The way out is not to choose. Paint everything, and HYDRATE the handful the
// player is actually standing near: as they walk into range a terrain instance
// is hidden and a real prefab takes its exact place, with its collider, its
// ResourceNode and its effects; as they walk away the object is destroyed and
// the painted instance comes back. Thirty or forty objects exist at a time
// instead of three thousand, and from the player's side every tree in the world
// is choppable, because the one they walk up to always is.
//
// ==== WHY A GRID AND NOT A DISTANCE CHECK ====
//
// Testing every instance against the player each tick is three thousand
// distance checks a frame, which would cost more than the objects did. The
// instances never move, so they are bucketed into a grid once at startup and
// only the nine cells around the player are ever looked at.
//
// ==== WHAT IT WILL NOT DO ====
//
// A tree the player chopped stays chopped: its index is retired and the painted
// instance is never restored. Anything else — a tree that fell, a tree a
// cutscene removed — is outside what this knows about, so those must remain
// real objects, which they already are.
[DisallowMultipleComponent]
public class VegetationHydrator : MonoBehaviour
{
    public static VegetationHydrator Instance { get; private set; }

    [Header("Range")]
    [Tooltip("Metres within which a painted tree becomes a real, choppable object. Only needs to comfortably exceed the player's reach and the camera's near field — bigger costs objects for nothing.")]
    public float hydrateRadius = 46f;
    [Tooltip("Extra metres before a hydrated tree is put back. Without this gap a tree on the boundary flickers between the two forms every time the player breathes.")]
    public float dehydrateHysteresis = 10f;

    [Header("Budget")]
    [Tooltip("Most trees to swap in a single tick. Spreads a walk into a dense forest over several frames instead of one long hitch.")]
    public int maxSwapsPerTick = 25;
    [Tooltip("Seconds between checks. The player cannot cross the hysteresis gap in this time, so there is nothing to gain from running it every frame.")]
    public float tickInterval = 0.15f;

    [Header("Diagnostics")]
    public bool logSummary = true;

    private Terrain _terrain;
    private TerrainData _data;
    private Transform _player;

    // The painted world, as it was at startup. Kept locally because reading
    // terrainData.treeInstances copies the entire array every time.
    private TreeInstance[] _instances;
    private GameObject[] _protoPrefab;

    // Grid of instance indices, so only the cells near the player are examined.
    private readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>(512);
    private float _cell;

    private readonly Dictionary<int, GameObject> _live = new Dictionary<int, GameObject>(64);
    private readonly HashSet<int> _retired = new HashSet<int>();
    private readonly List<int> _scratch = new List<int>(256);
    private int _faults;
    private float _nextReport;

    public static void Install(Terrain terrain)
    {
        if (terrain == null || terrain.terrainData == null) return;
        if (Instance != null) return;
        var go = new GameObject("[VegetationHydrator]");
        var h = go.AddComponent<VegetationHydrator>();
        h._terrain = terrain;
        h._data = terrain.terrainData;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        if (_terrain == null) { _terrain = Terrain.activeTerrain; _data = _terrain != null ? _terrain.terrainData : null; }
        if (_data == null) { Destroy(gameObject); return; }

        _instances = _data.treeInstances;
        if (_instances == null || _instances.Length == 0)
        {
            // Nothing painted — either painting is off or generation put every
            // tree down as an object. Either way there is nothing to do, and
            // saying so beats sitting there ticking over an empty world.
            if (logSummary) Debug.Log("[Hydrator] No painted trees on the terrain — standing down.");
            Destroy(gameObject);
            return;
        }

        var protos = _data.treePrototypes;
        _protoPrefab = new GameObject[protos.Length];
        for (int i = 0; i < protos.Length; i++) _protoPrefab[i] = protos[i].prefab;

        BuildGrid();
        if (logSummary)
            Debug.Log($"[Hydrator] Watching {_instances.Length} painted trees across {_grid.Count} cells. " +
                      $"They become real objects within {hydrateRadius:0}m.");

        StartCoroutine(TickRoutine());
    }

    private void BuildGrid()
    {
        _cell = Mathf.Max(8f, hydrateRadius);
        Vector3 size = _data.size;
        Vector3 origin = _terrain.transform.position;

        for (int i = 0; i < _instances.Length; i++)
        {
            Vector3 w = origin + Vector3.Scale(_instances[i].position, size);
            long key = CellKey(w.x, w.z);
            if (!_grid.TryGetValue(key, out var list)) _grid[key] = list = new List<int>(16);
            list.Add(i);
        }
    }

    private long CellKey(float x, float z)
    {
        int cx = Mathf.FloorToInt(x / _cell);
        int cz = Mathf.FloorToInt(z / _cell);
        // Packed into one long so the dictionary hashes a primitive rather than
        // boxing a struct on every lookup.
        return ((long)cx << 32) ^ (uint)cz;
    }

    private IEnumerator TickRoutine()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player != null)
            {
                // A THROW MUST NOT END HYDRATION FOR THE RUN.
                //
                // An exception inside a coroutine stops that coroutine
                // permanently. Without this, one bad frame anywhere in Tick
                // means every tree for the rest of the session is scenery —
                // and it looks exactly like "it worked near spawn and then
                // stopped", because that is precisely what happens.
                try { Tick(); }
                catch (System.Exception e)
                {
                    _faults++;
                    if (_faults <= 3)
                        Debug.LogError($"[Hydrator] Tick threw (fault {_faults}); hydration continues.\n{e}");
                }
            }
            yield return wait;
        }
    }

    private void Tick()
    {
        Vector3 pos = _player.position;

        if (logSummary && Time.time >= _nextReport)
        {
            _nextReport = Time.time + 10f;
            Debug.Log($"[Hydrator] {_live.Count} trees real around the player, {_retired.Count} chopped. " +
                      "If this number is zero while you are standing in a wood, the grid lookup is missing them.");
        }
        float hydrateSqr = hydrateRadius * hydrateRadius;
        float dropSqr = (hydrateRadius + dehydrateHysteresis) * (hydrateRadius + dehydrateHysteresis);

        // PUT BACK anything the player has walked away from, first. Doing this
        // before hydrating keeps the object count from spiking while crossing a
        // dense patch, which is the moment a hitch would be most noticeable.
        _scratch.Clear();
        foreach (var kv in _live)
        {
            if (kv.Value == null) { _scratch.Add(kv.Key); continue; }   // chopped
            if ((kv.Value.transform.position - pos).sqrMagnitude > dropSqr) _scratch.Add(kv.Key);
        }
        foreach (int idx in _scratch) Dehydrate(idx);

        // BRING IN whatever is close, up to the budget.
        int budget = maxSwapsPerTick;
        Vector3 size = _data.size;
        Vector3 origin = _terrain.transform.position;

        for (int dx = -1; dx <= 1 && budget > 0; dx++)
        {
            for (int dz = -1; dz <= 1 && budget > 0; dz++)
            {
                long key = CellKey(pos.x + dx * _cell, pos.z + dz * _cell);
                if (!_grid.TryGetValue(key, out var list)) continue;

                for (int n = 0; n < list.Count && budget > 0; n++)
                {
                    int i = list[n];
                    if (_retired.Contains(i) || _live.ContainsKey(i)) continue;

                    Vector3 w = origin + Vector3.Scale(_instances[i].position, size);
                    if ((w - pos).sqrMagnitude > hydrateSqr) continue;

                    if (Hydrate(i, w)) budget--;
                }
            }
        }
    }

    // TURN THE NEAREST PAINTED TREE REAL, RIGHT NOW.
    //
    // The tick is a prediction — it guesses which trees the player is about to
    // care about from where they are standing. A prediction can be late: sprint
    // into a wood and for a fraction of a second the trees around you are still
    // painted, and a swing at one of them hits nothing. That is not a tuning
    // problem to be solved with a bigger budget, it is a race, and the way to
    // win a race is not to run it.
    //
    // So the player's swing says so directly. Painted trees collide through the
    // TerrainCollider, so a melee hit that lands on terrain and nothing else is
    // very likely a tree — this converts the nearest one and the follow-up swing
    // finds a real object. Costs nothing when there is no tree there.
    public static bool EnsureRealAt(Vector3 point, float radius = 3.5f)
    {
        return Instance != null && Instance.ConvertNearest(point, radius);
    }

    private bool ConvertNearest(Vector3 point, float radius)
    {
        if (_instances == null) return false;

        Vector3 size = _data.size;
        Vector3 origin = _terrain.transform.position;
        float bestSqr = radius * radius;
        int best = -1;
        Vector3 bestPos = Vector3.zero;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                long key = CellKey(point.x + dx * _cell, point.z + dz * _cell);
                if (!_grid.TryGetValue(key, out var list)) continue;

                for (int n = 0; n < list.Count; n++)
                {
                    int i = list[n];
                    if (_retired.Contains(i) || _live.ContainsKey(i)) continue;

                    Vector3 w = origin + Vector3.Scale(_instances[i].position, size);
                    float d = (new Vector2(w.x - point.x, w.z - point.z)).sqrMagnitude;
                    if (d < bestSqr) { bestSqr = d; best = i; bestPos = w; }
                }
            }
        }

        return best >= 0 && Hydrate(best, bestPos);
    }

    private bool Hydrate(int index, Vector3 world)
    {
        TreeInstance inst = _instances[index];
        if (inst.prototypeIndex < 0 || inst.prototypeIndex >= _protoPrefab.Length) return false;
        GameObject prefab = _protoPrefab[inst.prototypeIndex];
        if (prefab == null) return false;

        var go = Instantiate(prefab, world, Quaternion.Euler(0f, inst.rotation * Mathf.Rad2Deg, 0f), transform);
        // The painted instance's scale is a MULTIPLIER on the prototype, and the
        // prototype is the prefab — so the object has to keep its own authored
        // scale and take the instance's on top, or every hydrated tree pops to a
        // different size than the one that was standing there a frame ago.
        Vector3 authored = prefab.transform.localScale;
        go.transform.localScale = new Vector3(authored.x * inst.widthScale,
                                              authored.y * inst.heightScale,
                                              authored.z * inst.widthScale);

        // Hide the painted one. Zero scale rather than removing it from the
        // array: removal means rewriting and re-uploading every instance on the
        // terrain, which is exactly the cost this system exists to avoid.
        var hidden = inst;
        hidden.widthScale = 0f;
        hidden.heightScale = 0f;
        try { _data.SetTreeInstance(index, hidden); }
        catch (System.Exception e)
        {
            // If the terrain refuses the edit there would be two trees in one
            // spot, which is worse than one that cannot be chopped.
            Debug.LogWarning($"[Hydrator] Could not hide painted tree {index}, so it is left alone: {e.Message}");
            Destroy(go);
            _retired.Add(index);
            return false;
        }

        _live[index] = go;
        return true;
    }

    private void Dehydrate(int index)
    {
        if (!_live.TryGetValue(index, out var go)) return;
        _live.Remove(index);

        // The object being gone means the player chopped it. That tree does not
        // come back — restoring the painted instance would regrow a forest
        // behind the player as they walked through it.
        if (go == null) { _retired.Add(index); return; }

        Destroy(go);
        try { _data.SetTreeInstance(index, _instances[index]); }
        catch { _retired.Add(index); }
    }

    // Painted trees are baked into the TerrainData ASSET, which is shared and
    // persists between play sessions in the editor. Anything left hidden or
    // missing when the scene ends would still be hidden the next time it loads,
    // and after a few runs the forest would be full of invisible gaps nobody
    // could explain. Everything goes back exactly as it was found.
    private void OnDisable()
    {
        if (_data == null || _instances == null) return;
        foreach (var kv in _live)
            if (kv.Value != null) Destroy(kv.Value);
        _live.Clear();

        try { _data.SetTreeInstances(_instances, true); }
        catch (System.Exception e) { Debug.LogWarning($"[Hydrator] Could not restore the painted trees: {e.Message}"); }
    }
}
