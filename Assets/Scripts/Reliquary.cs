using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A place worth walking to, that you can see, and that you have to earn.
//
// ==== WHY NOT A BEAM ====
//
// The first version put a shaft of light over a chest. It works, and it is what
// every game does, and that is the problem: a coloured laser is a HUD element
// wearing a costume. It says "the designer put a reward here" rather than
// "somebody built something here", and once a map has a few the world reads as a
// menu. A reliquary is a STRUCTURE. Banners on tall poles are the landmark,
// because they are the one prop that reads across broken terrain — high,
// coloured, and a vertical line the eye instantly separates from trees.
//
// ==== WHY IT CANNOT BE GRABBED AND FLED FROM ====
//
// A reward you walk up to and collect is not a decision, it is a pickup, and a
// map of pickups is a chore list. Two locks, layered by grade so the small ones
// stay quick:
//
//   GUARDIANS. Dormant until you are close, then they wake. The chest is visibly
//   sealed while any of them stand, so the player reads "kill these first"
//   without being told.
//
//   THE CHANNEL. Opening takes time, and at a Barrow the time is long enough
//   that a wave arrives during it. The question stops being "can I reach it" and
//   becomes "can I hold this ground for nine seconds".
//
// The channel's rule is the important part and it is not the obvious one:
// DAMAGE DOES NOT INTERRUPT, WALKING AWAY DOES. In a game where something is
// clipping you almost constantly, cancel-on-damage does not mean "hard", it
// means "you may never open this during a fight" — and the fight is the whole
// point. Leaving, on the other hand, should cost the progress.
//
// And the loot lands at the END of the channel, never the start, or the player
// could open it and run — the exact thing this exists to prevent.
[DisallowMultipleComponent]
public class Reliquary : MonoBehaviour
{
    public enum Grade
    {
        Wayside,   // roadside find: no guard, a moment to open
        Shrine,    // one guardian, a short hold
        Barrow,    // a warband of them, a long hold, and a wave during it
    }

    [Header("Grade")]
    public Grade grade = Grade.Wayside;

    [Header("Payout")]
    [Tooltip("Scales supplies only. Armour odds live in ArmourLootTable so the economy has one owner.")]
    [Range(0.5f, 3f)] public float richness = 1f;

    [Header("Channel")]
    public float waysideChannel = 2f;
    public float shrineChannel = 4f;
    public float barrowChannel = 9f;
    [Tooltip("How far the player may drift before the channel starts draining.")]
    public float channelLeash = 4.5f;
    [Tooltip("Progress lost per second when out of range, as a fraction of the whole.")]
    public float drainRate = 0.5f;

    private LootChest _chest;
    private Light _lantern;
    private readonly List<EnemyAI> _guardians = new List<EnemyAI>(4);
    private readonly List<GameObject> _sealVfx = new List<GameObject>(4);
    private Transform _player;
    private float _progress;
    private bool _spent;
    private float _guardCheck;
    private bool _wavesSent;

    public bool Sealed => LivingGuardians() > 0;
    private float ChannelTime => grade switch
    {
        Grade.Barrow => barrowChannel,
        Grade.Shrine => shrineChannel,
        _ => waysideChannel,
    };
    private Color Accent => grade switch
    {
        Grade.Barrow => new Color(1.00f, 0.45f, 0.15f),
        Grade.Shrine => new Color(0.85f, 0.45f, 1.00f),
        _ => new Color(0.95f, 0.88f, 0.65f),
    };

    public void Bind(LootChest chest)
    {
        _chest = chest;
        if (_chest == null) return;
        _chest.Opened += OnOpened;
        // The reliquary owns the interaction. Leaving the chest's own press-to-
        // open live alongside the channel is how a player skips the fight.
        //
        // A flag rather than disabling the component: a disabled MonoBehaviour
        // cannot start a coroutine, so ForceOpen did nothing and the chest
        // opened empty.
        _chest.suppressOwnInteraction = true;
    }

    private void OnDestroy()
    {
        if (_chest != null) _chest.Opened -= OnOpened;
    }

    // ---- the site ------------------------------------------------------------

    public void Raise(ReliquarySet set)
    {
        if (set == null) return;

        bool barrow = grade == Grade.Barrow;
        int banners = barrow ? 4 : grade == Grade.Shrine ? 2 : 1;
        float ring = barrow ? 4.8f : grade == Grade.Shrine ? 3.4f : 2.4f;

        // Banners first and furthest out: they are the part that has to be seen
        // from across the valley, so nothing may occlude them.
        for (int i = 0; i < banners; i++)
        {
            var prefab = set.PickBanner();
            if (prefab == null) break;
            float a = (i / (float)banners) * Mathf.PI * 2f + Mathf.PI * 0.25f;
            Strip(Instantiate(prefab, Offset(a, ring), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), transform));
        }

        // Rune stones make it a shrine rather than a picnic. Odd count and
        // uneven spacing so it reads as raised by hand, not placed by a loop.
        int stones = barrow ? 7 : grade == Grade.Shrine ? 5 : 2;
        for (int i = 0; i < stones; i++)
        {
            var prefab = set.PickRuneStone();
            if (prefab == null) break;
            float a = (i / (float)stones) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            var go = Instantiate(prefab, Offset(a, ring * 0.62f), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            go.transform.localScale *= Random.Range(0.85f, 1.25f);
            Strip(go, keepColliders: true);   // stones are cover; let them block
        }

        if (barrow)
        {
            if (set.archPrefab != null)
                Strip(Instantiate(set.archPrefab, Offset(0f, ring * 1.25f), Quaternion.Euler(0f, 180f, 0f), transform), keepColliders: true);

            for (int i = 0; i < 6; i++)
            {
                var bone = set.PickRemains();
                if (bone == null) break;
                var go = Instantiate(bone, Offset(Random.Range(0f, Mathf.PI * 2f), Random.Range(1.4f, ring)),
                                     Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(0f, 360f), Random.Range(-14f, 14f)), transform);
                Strip(go);
            }
        }

        if (set.lanternPrefab != null && grade != Grade.Wayside)
        {
            var lamp = Instantiate(set.lanternPrefab, Offset(Mathf.PI, ring * 0.5f), Quaternion.identity, transform);
            Strip(lamp);
            var go = new GameObject("Lantern");
            go.transform.SetParent(transform, false);
            go.transform.position = lamp.transform.position + Vector3.up * 1.6f;
            _lantern = go.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.color = Accent;
            _lantern.range = barrow ? 12f : 8f;
            _lantern.intensity = barrow ? 3.2f : 2.1f;
            // A dozen of these each casting shadows would cost more than the
            // whole feature is worth.
            _lantern.shadows = LightShadows.None;
        }

        PostGuardians(set, ring);
    }

    // Dormant until approached. Built out of the ordinary enemy AI rather than a
    // bespoke state machine: startPassive already means "stand here until
    // something comes close", which is exactly a sleeping guard, and it means
    // they fight like every other enemy once woken instead of like a special case.
    private void PostGuardians(ReliquarySet set, float ring)
    {
        int count = grade switch { Grade.Barrow => 4, Grade.Shrine => 1, _ => 0 };
        if (count == 0 || set.guardianPrefabs == null || set.guardianPrefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            var prefab = set.guardianPrefabs[Random.Range(0, set.guardianPrefabs.Length)];
            if (prefab == null) continue;

            float a = (i / (float)count) * Mathf.PI * 2f + Mathf.PI * 0.5f;
            Vector3 p = Offset(a, ring * 0.78f);
            var go = Instantiate(prefab, p, Quaternion.identity, null);

            var ai = go.GetComponent<EnemyAI>();
            if (ai == null) continue;
            ai.startPassive = true;
            ai.roamWhilePassive = false;      // a guard stands; it does not mill about
            ai.faceAnchorWhenIdle = true;
            ai.anchorPoint = transform.position;   // facing the thing they guard
            ai.roamRadius = 0.2f;
            ai.aggroRange = 13f;
            // Guardians never give up and never leave. Their whole job is to be
            // the reason this chest is still shut, and one that wandered off
            // would leave a permanently sealed reliquary.
            ai.canDeAggro = false;
            ai.leashRange = 40f;
            ai.CapturePost();
            _guardians.Add(ai);

            _sealVfx.Add(MakeSealMote(i, count));
        }
    }

    // One mote per living guardian, orbiting the chest — the seal made visible.
    // Built in code rather than from a prefab because there is no reusable
    // corruption-anchor effect in the project (the totem's is a scene instance),
    // and a light plus a small emissive bead says "locked" perfectly well.
    private GameObject MakeSealMote(int index, int total)
    {
        var go = new GameObject($"Seal_{index}");
        go.transform.SetParent(transform, false);

        var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bead.transform.SetParent(go.transform, false);
        bead.transform.localScale = Vector3.one * 0.22f;
        Destroy(bead.GetComponent<Collider>());
        var r = bead.GetComponent<Renderer>();
        if (r != null)
        {
            var mat = new Material(r.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Accent);
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Accent * 3f); }
            r.material = mat;
        }

        var lightGo = new GameObject("SealGlow");
        lightGo.transform.SetParent(go.transform, false);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Accent;
        l.range = 3.5f;
        l.intensity = 1.6f;
        l.shadows = LightShadows.None;

        go.AddComponent<SealMoteOrbit>().Configure(transform, index / (float)Mathf.Max(1, total));
        return go;
    }

    // Keeps a mote circling the chest. A separate tiny component so the orbit
    // keeps running even while the reliquary's own Update has early-returned.
    private class SealMoteOrbit : MonoBehaviour
    {
        private Transform _centre;
        private float _phase;

        public void Configure(Transform centre, float phase01)
        {
            _centre = centre;
            _phase = phase01 * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (_centre == null) return;
            float a = _phase + Time.time * 0.9f;
            transform.position = _centre.position
                               + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.15f
                               + Vector3.up * (1.3f + Mathf.Sin(Time.time * 1.7f + _phase) * 0.12f);
        }
    }

    private Vector3 Offset(float angle, float radius)
    {
        Vector3 world = transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        world.y = GroundAt(world);
        return world;
    }

    private static void Strip(GameObject go, bool keepColliders = false)
    {
        if (go == null) return;
        if (!keepColliders)
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        else
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.isTrigger = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        VFXAutoFade.HideFromMinimap(go);
    }

    // ---- the channel ---------------------------------------------------------

    private void Update()
    {
        if (_spent || _chest == null) return;

        if (_player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            _player = pc.transform;
        }

        // Retire spent seal motes as their guardians fall, so the chest visibly
        // unlocks one kill at a time instead of all at once at the end.
        _guardCheck -= Time.deltaTime;
        if (_guardCheck <= 0f)
        {
            _guardCheck = 0.25f;
            int alive = LivingGuardians();
            for (int i = 0; i < _sealVfx.Count; i++)
                if (_sealVfx[i] != null && _sealVfx[i].activeSelf != i < alive)
                    _sealVfx[i].SetActive(i < alive);
        }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _chest.interactRange + channelLeash * 2f) { _progress = Mathf.Max(0f, _progress - Time.deltaTime * drainRate); return; }

        if (Sealed)
        {
            if (dist <= _chest.interactRange + 2f)
                ShowBar(LocalizationManager.Tr("RELIQUARY_SEALED", LivingGuardians()), 0f);
            _progress = 0f;
            return;
        }

        bool inReach = dist <= _chest.interactRange + channelLeash;
        bool holding = inReach && Input.GetKey(_chest.interactKey);

        if (holding)
        {
            if (grade == Grade.Barrow && !_wavesSent && _progress > 0.15f)
            {
                _wavesSent = true;
                // The whole point of a Barrow: the ground you have to hold is
                // contested from the moment you commit to holding it.
                var alerts = RegionAlertDirector.Instance;
                if (alerts != null) alerts.RaiseAlert(transform.position, transform);
            }
            _progress += Time.deltaTime / Mathf.Max(0.1f, ChannelTime);
        }
        else if (_progress > 0f)
        {
            // Drains rather than resets. Being knocked back for half a second
            // should cost something, not everything.
            _progress -= Time.deltaTime * drainRate;
        }

        _progress = Mathf.Clamp01(_progress);

        if (_progress > 0f || inReach)
        {
            string key = holding ? "RELIQUARY_OPENING"
                       : _progress > 0f ? "RELIQUARY_HOLD"
                       : "RELIQUARY_PROMPT";
            ShowBar(LocalizationManager.Tr(key), _progress);
        }

        if (_progress >= 1f)
        {
            _spent = true;
            _chest.ForceOpen();   // payout rides on LootChest.Opened
        }
    }

    // The boss HP bar, borrowed. The player already reads that bar as "there is
    // a fight in progress and here is how far through it you are" — which is
    // exactly what breaking a seal is — so reusing it costs them nothing to
    // learn. GlobalHUD drops the real boss bar out of the way if one is up.
    private void ShowBar(string label, float progress)
    {
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.ShowObjectiveBar(label, progress, Accent);
        _barShownAt = Time.unscaledTime;
    }

    private float _barShownAt = -1f;

    private void LateUpdate()
    {
        // Hide it a moment after the last Show. Every way out of a channel —
        // finishing, walking off, dying, the guardians being killed elsewhere —
        // would otherwise need its own teardown, and the one that got forgotten
        // would leave a bar stuck on screen for the rest of the run.
        if (_barShownAt < 0f) return;
        if (Time.unscaledTime - _barShownAt < 0.25f) return;
        _barShownAt = -1f;
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.HideObjectiveBar();
    }

    private int LivingGuardians()
    {
        int n = 0;
        for (int i = 0; i < _guardians.Count; i++)
            if (_guardians[i] != null && !_guardians[i].IsDead) n++;
        return n;
    }

    // ---- the payout ----------------------------------------------------------

    private void OnOpened()
    {
        bool gaveArmour = Random.value < ArmourLootTable.ArmourChance(grade) && GrantArmour();

        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            float scale = richness * grade switch { Grade.Barrow => 1.8f, Grade.Shrine => 1.3f, _ => 1f };
            rm.AddStashResources(
                Mathf.RoundToInt(Random.Range(35f, 70f) * scale),
                Mathf.RoundToInt(Random.Range(25f, 55f) * scale),
                Mathf.RoundToInt(Random.Range(15f, 35f) * scale));
            // No diamonds on top of a piece of armour — see ArmourLootTable.
            if (!gaveArmour) rm.AddDiamonds(Mathf.RoundToInt(Random.Range(12f, 26f) * scale));
            rm.UpdateUI();
        }

        if (_lantern != null) _lantern.enabled = false;
        ReliquaryDirector.NoteOpened(this);
    }

    private bool GrantArmour()
    {
        var prize = ArmourLootTable.Roll(grade);
        if (prize == null) return false;

        PlayerPrefs.SetInt("ArmorUnlocked_" + prize.armorID, 1);
        PlayerPrefs.Save();
        ArmourLootTable.NoteGranted();

        int tier = ArmourLootTable.TierOf(prize);
        RewardReveal.Show(prize.icon, prize.armorName,
            LocalizationManager.Tr("REVEAL_ARMOUR_SUB",
                                   LocalizationManager.Tr(ArmourLootTable.TierNameKey(tier)),
                                   prize.category.ToString(), prize.basePower),
            ArmourLootTable.TierColour(tier));
        return true;
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
