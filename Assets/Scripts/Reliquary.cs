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

    // ==== WHY THIS COMPONENT ASSEMBLES ITSELF ====
    //
    // The first version could only be built by a director that hunted for a clear
    // patch of terrain and assembled the whole site out of loose prefabs at
    // runtime. That put every part of the feature behind one search that could
    // fail — and did, silently, for several rounds: if no site passed the
    // clearance test, nothing existed and nothing said why.
    //
    // Now the site is a PREFAB. Drop Reliquary_Shrine into a location built by
    // hand, add that location to the generator's POI list, and the chest brings
    // its own guardians, seal and channel with it. Nothing has to find anywhere.
    // The director still exists and still scatters them across open ground, but it
    // is now one way to place a prefab rather than the only path that works.
    [Header("Self-assembly")]
    [Tooltip("Post guardians around the chest on Start. Leave on: the guardians ARE the lock, and a site without them is a free pickup.")]
    public bool spawnGuardians = true;
    [Tooltip("How far out the guardians stand. Widen it for a big hand-built location so they are not standing in the walls.")]
    public float guardRadius = 4f;
    [Tooltip("Override the guardian count for this site. -1 keeps the count the grade implies (0 / 1 / 4).")]
    public int guardianCountOverride = -1;
    [Tooltip("Also scatter banners, stones and bones around the chest. OFF for a prefab dropped into a hand-built location — the location already has its own dressing — and ON for a site the director places on bare ground.")]
    public bool buildDecor = false;

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

    // Everything the prefab needs to become a live site, without a director.
    private void Start()
    {
        if (_chest == null) Bind(GetComponentInChildren<LootChest>(true));
        if (_chest == null)
        {
            Debug.LogWarning($"[Reliquary] '{name}' has no LootChest under it — there is nothing here to open. " +
                             "Use the prefabs from Tools > Exploration > Build Reliquary Prefabs.", this);
            enabled = false;
            return;
        }

        var set = ReliquarySet.Load();
        if (buildDecor && set != null) Raise(set);
        if (spawnGuardians && _guardians.Count == 0) PostGuardians(set, guardRadius);
    }

    public void Bind(LootChest chest)
    {
        if (_chest == chest) return;
        _chest = chest;
        if (_chest == null) return;
        _chest.Opened += OnOpened;
        // The reward lands when the lid is UP, not when the player commits — see
        // LootChest.LidOpened.
        _chest.LidOpened += OnLidOpened;
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
        if (_chest == null) return;
        _chest.Opened -= OnOpened;
        _chest.LidOpened -= OnLidOpened;
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
            // Tall on purpose: the banners ARE the landmark, so they are the one
            // prop allowed to be bigger than a person.
            Strip(PlaceProp(prefab, Offset(a, ring), -a * Mathf.Rad2Deg, 3.4f, transform));
        }

        // Rune stones make it a shrine rather than a picnic. Odd count and
        // uneven spacing so it reads as raised by hand, not placed by a loop.
        int stones = barrow ? 7 : grade == Grade.Shrine ? 5 : 2;
        for (int i = 0; i < stones; i++)
        {
            var prefab = set.PickRuneStone();
            if (prefab == null) break;
            float a = (i / (float)stones) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            var go = PlaceProp(prefab, Offset(a, ring * 0.62f), Random.Range(0f, 360f),
                               Random.Range(1.3f, 1.9f), transform);
            Strip(go, keepColliders: true);   // stones are cover; let them block
        }

        if (barrow)
        {
            if (set.archPrefab != null)
                Strip(PlaceProp(set.archPrefab, Offset(0f, ring * 1.25f), 180f, 4.2f, transform), keepColliders: true);

            for (int i = 0; i < 6; i++)
            {
                var bone = set.PickRemains();
                if (bone == null) break;
                // Yaw only. A skull given a random pitch and roll floats at an
                // angle instead of lying where somebody dropped it.
                var go = PlaceProp(bone, Offset(Random.Range(0f, Mathf.PI * 2f), Random.Range(1.4f, ring)),
                                   Random.Range(0f, 360f), Random.Range(0.28f, 0.45f), transform);
                Strip(go);
            }
        }

        if (set.lanternPrefab != null && grade != Grade.Wayside)
        {
            var lamp = PlaceProp(set.lanternPrefab, Offset(Mathf.PI, ring * 0.5f),
                                 Random.Range(0f, 360f), 2.1f, transform);
            Strip(lamp);
            var go = new GameObject("Lantern");
            go.transform.SetParent(transform, false);
            go.transform.position = (lamp != null ? lamp.transform.position : Offset(Mathf.PI, ring * 0.5f))
                                  + Vector3.up * 1.6f;
            _lantern = go.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.color = Accent;
            _lantern.range = barrow ? 12f : 8f;
            _lantern.intensity = barrow ? 3.2f : 2.1f;
            // A dozen of these each casting shadows would cost more than the
            // whole feature is worth.
            _lantern.shadows = LightShadows.None;
        }

        // Guardians are NOT posted here. Decor and guards are separate decisions:
        // a prefab dropped into a hand-built ruin wants the guards and none of the
        // dressing, and Start owns that call.
        guardRadius = ring;
    }

    // Dormant until approached. Built out of the ordinary enemy AI rather than a
    // bespoke state machine: startPassive already means "stand here until
    // something comes close", which is exactly a sleeping guard, and it means
    // they fight like every other enemy once woken instead of like a special case.
    private void PostGuardians(ReliquarySet set, float ring)
    {
        int count = guardianCountOverride >= 0
                  ? guardianCountOverride
                  : grade switch { Grade.Barrow => 4, Grade.Shrine => 1, _ => 0 };
        if (count == 0) return;
        if (set == null || set.guardianPrefabs == null || set.guardianPrefabs.Length == 0)
        {
            // A sealed chest with nothing to unseal it is a chest nobody can ever
            // open, so this is loud rather than a shrug.
            Debug.LogWarning($"[Reliquary] '{name}' wants {count} guardians but the ReliquarySet has no guardian " +
                             "prefabs. Run Tools > Exploration > Build Reliquary Set, or the seal can never break.", this);
            return;
        }

        if (ring <= 0.1f) ring = 4f;

        for (int i = 0; i < count; i++)
        {
            var prefab = set.guardianPrefabs[Random.Range(0, set.guardianPrefabs.Length)];
            if (prefab == null) continue;

            float a = (i / (float)count) * Mathf.PI * 2f + Mathf.PI * 0.5f;
            Vector3 p = Offset(a, ring);
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

    // Place a prop at a sane real-world size, upright, sitting on the ground.
    //
    // THIS IS WHY THE SITES LOOKED LIKE A PILE OF ASSETS. Every prefab in the set
    // has localScale 1, but they come from five different packs and their MESHES
    // are authored in wildly different units — the chest is several metres tall
    // in its own space and the "lantern" is a street lamp. Instantiating them as
    // they are gives a shrine where the chest dwarfs the player and the banners
    // are the size of buildings.
    //
    // So nothing is placed at its authored scale. Each prop is measured and
    // scaled to the size it should be IN THIS WORLD, then grounded by its own
    // bounds rather than by its pivot — pivots across packs sit at the base, the
    // centre or nowhere in particular, which is the other half of why things
    // floated and sank. Rotation is yaw only: a banner given a random pitch lies
    // down, and a lying banner is not a landmark.
    public static GameObject PlaceProp(GameObject prefab, Vector3 groundPos, float yawDegrees,
                                       float targetHeight, Transform parent)
    {
        if (prefab == null) return null;

        var go = Instantiate(prefab, groundPos, Quaternion.Euler(0f, yawDegrees, 0f), parent);

        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return go;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        if (b.size.y > 0.001f && targetHeight > 0.01f)
        {
            float k = targetHeight / b.size.y;
            go.transform.localScale *= k;

            // Bounds move with the scale, so they have to be re-read before the
            // grounding step or the object is placed using its old footprint.
            rends = go.GetComponentsInChildren<Renderer>();
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }

        // Sit the BOTTOM of the mesh on the ground, whatever the pivot says.
        float lift = groundPos.y - b.min.y;
        go.transform.position += Vector3.up * lift;
        return go;
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

    // The player committed. Nothing is paid here — see OnLidOpened.
    private void OnOpened()
    {
        if (_lantern != null) _lantern.enabled = false;
        ReliquaryDirector.NoteOpened(this);
    }

    // The lid is up. Everything the reward consists of happens now, in one beat.
    private void OnLidOpened()
    {
        bool gaveArmour = Random.value < ArmourLootTable.ArmourChance(grade) && GrantArmour();

        // ==== HOW MUCH, AND WHY IT IS NOT ALWAYS THE SAME ====
        //
        // The first pass paid a flat 35-70 wood, 25-55 stone and 15-35 food, and
        // that was wrong twice over. It was too much — the backpack holds 100 / 50
        // / 30, so ONE roadside chest filled it and every chest after that in the
        // run was worth nothing. And it was the same every time, so after two
        // chests the player knew exactly what the third contained and opening it
        // stopped being a question.
        //
        // So the payout is a FRACTION OF WHAT YOU CAN CARRY, rolled against a
        // fortune table. A wayside find is normally a handful; occasionally it is
        // a real haul. A barrow is normally a real haul; rarely it is more than
        // you can carry home, which is a good problem and a memorable one.
        int fortune = RollFortune();
        float share = GradeShare() * FortuneScale(fortune)
                    // Distance still pays, but modestly — this used to more than
                    // double the payout on a far site, which is how a single
                    // chest ended a run's need to gather anything.
                    * Mathf.Lerp(1f, 1.35f, Mathf.InverseLerp(1f, 2.1f, richness));

        var caps = ResourceManager.Instance;
        int capWood = caps != null ? caps.GetRunMax("Wood") : 100;
        int capStone = caps != null ? caps.GetRunMax("Stone") : 50;
        int capFood = caps != null ? caps.GetRunMax("Food") : 30;

        // Not every chest holds everything. A crate of salted meat, an ore cache,
        // a woodpile — a chest with a CHARACTER is worth remembering, and three
        // even piles every time is worth nothing.
        bool anyWood = false, anyStone = false, anyFood = false;
        int kinds = grade == Grade.Barrow ? Random.Range(2, 4)
                  : grade == Grade.Shrine ? Random.Range(1, 4)
                  : Random.Range(1, 3);
        // Drawn with replacement, so asking for two kinds sometimes yields one —
        // which is the point: the spread itself varies, not only the amount.
        for (int i = 0; i < kinds; i++)
            switch (Random.Range(0, 3))
            {
                case 0: anyWood = true; break;
                case 1: anyStone = true; break;
                default: anyFood = true; break;
            }

        // Concentrated when there are fewer kinds, so a single-resource chest is
        // a proper pile rather than a third of one.
        int present = (anyWood ? 1 : 0) + (anyStone ? 1 : 0) + (anyFood ? 1 : 0);
        float focus = present <= 1 ? 1.6f : present == 2 ? 1.25f : 1f;

        int Amount(bool included, int cap) =>
            included ? Mathf.Max(1, Mathf.RoundToInt(cap * share * focus * Random.Range(0.85f, 1.15f))) : 0;

        int wood = Amount(anyWood, capWood);
        int stone = Amount(anyStone, capStone);
        int food = Amount(anyFood, capFood);

        // The XP and crystals the chest itself scatters ride the same roll, so a
        // rich chest is rich in every way at once instead of the two payouts
        // disagreeing about how good the find was. Set before SpawnLoot, which
        // runs on the very next line of LootChest's open sequence.
        if (_chest != null)
        {
            float k = FortuneScale(fortune);
            _chest.minLootItems = Mathf.Max(1, Mathf.RoundToInt(_chest.minLootItems * k));
            _chest.maxLootItems = Mathf.Max(_chest.minLootItems, Mathf.RoundToInt(_chest.maxLootItems * k));
        }

        // SUPPLIES COME OUT AS OBJECTS, not as a number that changes.
        //
        // Crediting the backpack directly is the cheap version and it reads as
        // nothing happening: the lid opens on an empty box while a counter ticks
        // somewhere at the edge of the screen. Throwing physical pickups out of
        // the chest costs a handful of prefabs and turns the payout into the thing
        // the player actually came for — a pile on the ground they walk through.
        //
        // Each pickup carries a share of the total, so the backpack ends up with
        // the same amount either way; the difference is entirely in the watching.
        // If a drop prefab is missing the amount is credited instead of being lost.
        var set = ReliquarySet.Load();
        Vector3 mouth = _chest != null ? _chest.LootOrigin : transform.position + Vector3.up;
        int creditWood = Scatter(set != null ? set.woodDrop : null, ResourceDrop.ResourceType.Wood, wood, mouth);
        int creditStone = Scatter(set != null ? set.stoneDrop : null, ResourceDrop.ResourceType.Stone, stone, mouth);
        int creditFood = Scatter(set != null ? set.foodDrop : null, ResourceDrop.ResourceType.Food, food, mouth);

        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            if (creditWood + creditStone + creditFood > 0)
                rm.AddRunResources(creditWood, creditStone, creditFood);
            // No diamonds on top of a piece of armour — see ArmourLootTable.
            if (!gaveArmour)
                rm.AddDiamonds(Mathf.Max(1, Mathf.RoundToInt(Random.Range(8f, 18f) * GradeCoin() * FortuneScale(fortune))));
            rm.UpdateUI();
        }
    }

    // 0 meagre, 1 fair, 2 rich, 3 hoard. Weighted by grade: a wayside find is
    // usually a handful and a barrow is usually worth the fight, but neither is
    // guaranteed, and that uncertainty is the only reason opening one is a moment.
    private int RollFortune()
    {
        int[] weights = grade switch
        {
            Grade.Barrow => new[] { 10, 35, 40, 15 },
            Grade.Shrine => new[] { 30, 42, 22, 6 },
            _            => new[] { 55, 33, 10, 2 },
        };
        int total = 0;
        foreach (int w in weights) total += w;
        int roll = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0) return i;
        }
        return 0;
    }

    private static float FortuneScale(int fortune) => fortune switch
    {
        3 => 2.6f,   // hoard — rare enough to be talked about
        2 => 1.6f,
        1 => 1.0f,
        _ => 0.6f,
    };

    // Fraction of the BACKPACK a fair find is worth. Everything is expressed
    // against carrying capacity rather than in absolute numbers so a change to the
    // backpack size cannot silently make chests trivial or overwhelming.
    private float GradeShare() => grade switch
    {
        Grade.Barrow => 0.38f,
        Grade.Shrine => 0.24f,
        _            => 0.14f,
    };

    private float GradeCoin() => grade switch
    {
        Grade.Barrow => 2.0f,
        Grade.Shrine => 1.4f,
        _            => 1.0f,
    };

    // Throws `total` worth of one resource out of the chest as pickups, and hands
    // back whatever could not be thrown so the caller can credit it directly.
    private static int Scatter(GameObject prefab, ResourceDrop.ResourceType type, int total, Vector3 mouth)
    {
        if (total <= 0) return 0;
        if (prefab == null) return total;

        // Enough to look like a haul, few enough not to carpet the ground.
        int pieces = Mathf.Clamp(Mathf.CeilToInt(total / 12f), 3, 8);
        int per = Mathf.Max(1, total / pieces);
        int left = total;

        for (int i = 0; i < pieces && left > 0; i++)
        {
            int give = (i == pieces - 1) ? left : Mathf.Min(per, left);
            left -= give;

            var go = Instantiate(prefab, mouth + Random.insideUnitSphere * 0.15f, Random.rotation);
            var drop = go.GetComponent<ResourceDrop>();
            if (drop == null) drop = go.AddComponent<ResourceDrop>();
            drop.resourceType = type;
            drop.amount = give;
            // Up and out, so the burst arcs over the open lid rather than
            // squirting sideways through the chest walls.
            drop.popForce = Random.Range(4.5f, 7.5f);
        }
        return left;
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
