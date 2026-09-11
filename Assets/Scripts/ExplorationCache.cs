using UnityEngine;

// A chest worth crossing the map for.
//
// ==== WHY EXPLORATION DID NOT WORK ====
//
// The chest system was never the problem: LootChest has a shake, a sound, an
// animated lid and a timed loot burst, and Camp_POI — which carries one — is
// already scattered across the region. The problem is that NOTHING TELLS THE
// PLAYER A CHEST EXISTS. Enemies get minimap dots. Diamonds get dots. The caged
// ally gets a marker. Chests get nothing, so on a map this size finding one is
// pure accident, which for most players means never.
//
// And exploration competes with combat for the player's time. Killing pays out
// continuously and visibly; if wandering pays nothing VISIBLE IN ADVANCE, nobody
// wanders. But a map covered in markers is not exploration either, it is a
// checklist. So: A HINT AT RANGE, A REWARD ON ARRIVAL.
//
// ==== THE THREE PARTS ====
//
//   THE TELL. A shaft of light standing over the cache, tall enough to clear the
//   canopy and read from across a valley. Its COLOUR says what is inside, which
//   turns the detour into a decision about need rather than a coin toss about
//   worth — a player short on stone and a player short on damage should walk
//   toward different lights.
//
//   THE MARKER. A compass pip, but only once you are close enough that you have
//   arguably already found it. Far away you get the light and your own judgement.
//
//   THE PRICE. Opening a cache is loud. It raises a region alert, so the choice
//   is not "is this worth walking to" — it is "do I want a fight here, now,
//   before I have cleared the area". A reward you simply pick up is not a
//   decision, and exploration made of non-decisions is just walking.
[DisallowMultipleComponent]
[RequireComponent(typeof(LootChest))]
public class ExplorationCache : MonoBehaviour
{
    public enum Kind
    {
        Supplies,   // camp economy: wood, stone, food
        Power,      // the run itself: xp and crystals
        Armoury,    // diamonds, and a weapon the player does not own yet
    }

    [Header("Contents")]
    public Kind kind = Kind.Supplies;
    [Tooltip("Scales every payout. Set by the director so a cache further from the roads is worth the walk.")]
    [Range(0.5f, 3f)] public float richness = 1f;

    [Header("The tell")]
    [Tooltip("Height of the light shaft. Has to clear the treetops or it is invisible from exactly the distance it exists for.")]
    public float beaconHeight = 26f;
    public float beaconWidth = 1.1f;
    [Tooltip("Beyond this the shaft fades out — it should read as a landmark, not light up the whole map.")]
    public float beaconFadeDistance = 190f;

    [Header("The marker")]
    [Tooltip("Compass pip appears inside this range. Deliberately short: the light is the long-range cue.")]
    public float compassRange = 70f;

    [Header("The price")]
    [Tooltip("Opening it raises a region alert. Off for the smallest caches.")]
    public bool noisyToOpen = true;

    private static readonly Color SuppliesColour = new Color(0.45f, 0.95f, 0.50f);
    private static readonly Color PowerColour    = new Color(0.40f, 0.72f, 1.00f);
    private static readonly Color ArmouryColour  = new Color(1.00f, 0.80f, 0.30f);

    private LootChest _chest;
    private LineRenderer _shaft;
    private Light _glow;
    private CompassMarkerItem _pip;
    private Transform _player;
    private float _checkTimer;

    public Color TellColour => kind switch
    {
        Kind.Power => PowerColour,
        Kind.Armoury => ArmouryColour,
        _ => SuppliesColour,
    };

    private void Awake()
    {
        _chest = GetComponent<LootChest>();
    }

    private void OnEnable()
    {
        if (_chest != null) _chest.Opened += OnOpened;
    }

    private void OnDisable()
    {
        if (_chest != null) _chest.Opened -= OnOpened;
    }

    private void Start()
    {
        BuildTell();

        // The pip starts switched OFF, not absent. CompassMarkerItem registers
        // itself in OnEnable and unregisters in OnDisable, so enabling the
        // component is the range gate and there is nothing else to maintain.
        _pip = gameObject.AddComponent<CompassMarkerItem>();
        _pip.enabled = false;
    }

    private void BuildTell()
    {
        var go = new GameObject("CacheTell");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _shaft = go.AddComponent<LineRenderer>();
        _shaft.useWorldSpace = false;
        _shaft.positionCount = 2;
        _shaft.SetPosition(0, Vector3.up * 0.2f);
        _shaft.SetPosition(1, Vector3.up * beaconHeight);
        _shaft.widthCurve = AnimationCurve.Linear(0f, beaconWidth, 1f, beaconWidth * 0.35f);
        _shaft.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _shaft.receiveShadows = false;
        _shaft.alignment = LineAlignment.View;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        Color c = TellColour;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);   // additive: reads against dark forest
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        _shaft.material = mat;

        // A small light at the base so the chest itself is lit when you arrive.
        // Without it the shaft hangs over an object you still cannot see.
        var lightGo = new GameObject("CacheGlow");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.up * 0.8f;
        _glow = lightGo.AddComponent<Light>();
        _glow.type = LightType.Point;
        _glow.color = c;
        _glow.range = 7f;
        _glow.intensity = 2.2f;
        _glow.shadows = LightShadows.None;   // dozens of these must not each cost a shadow map
    }

    private void Update()
    {
        // Four times a second is plenty for a distance check, and there can be a
        // dozen of these standing at once.
        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = 0.25f;

        if (_player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            _player = pc.transform;
        }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (_pip != null && _pip.enabled != dist <= compassRange)
            _pip.enabled = dist <= compassRange;

        if (_shaft != null)
        {
            // Fade rather than cut, so a landmark does not blink into existence
            // as the player crosses an invisible line.
            float a = 1f - Mathf.Clamp01((dist - beaconFadeDistance * 0.6f) / (beaconFadeDistance * 0.4f));
            Color c = TellColour;
            c.a = a;
            _shaft.startColor = c;
            Color tip = c; tip.a = a * 0.15f;
            _shaft.endColor = tip;
            if (_shaft.enabled != a > 0.01f) _shaft.enabled = a > 0.01f;
        }
    }

    // ---- the payout ----------------------------------------------------------

    private void OnOpened()
    {
        Grant();
        PutTheTellOut();

        if (noisyToOpen) WakeTheRegion();

        ExplorationCacheDirector.NoteFound(this);
    }

    private void Grant()
    {
        var rm = ResourceManager.Instance;
        switch (kind)
        {
            case Kind.Supplies:
                if (rm == null) break;
                rm.AddStashResources(
                    Mathf.RoundToInt(Random.Range(45f, 90f) * richness),
                    Mathf.RoundToInt(Random.Range(30f, 70f) * richness),
                    Mathf.RoundToInt(Random.Range(20f, 45f) * richness));
                rm.UpdateUI();
                break;

            case Kind.Power:
                // LootChest's own burst already handles xp and crystals; this
                // just makes a Power cache noticeably richer than an ordinary one.
                var chest = GetComponent<LootChest>();
                if (chest != null)
                {
                    chest.minLootItems = Mathf.RoundToInt(chest.minLootItems * (1.8f * richness));
                    chest.maxLootItems = Mathf.RoundToInt(chest.maxLootItems * (1.8f * richness));
                }
                break;

            case Kind.Armoury:
                if (rm != null) rm.AddDiamonds(Mathf.RoundToInt(Random.Range(35f, 70f) * richness));
                GrantUnownedWeapon();
                break;
        }
    }

    // The find that is actually exciting: a weapon, not a number.
    //
    // Ownership already lives in PlayerPrefs as "WeaponUnlocked_<id>", which the
    // shop reads — so unlocking one here puts a real weapon in the player's rack
    // with no new system to build. If they already own everything, the diamonds
    // above stand in for it rather than the cache paying nothing.
    private void GrantUnownedWeapon()
    {
        // Via WeaponIndex, not Resources.LoadAll: the WeaponData assets live in
        // Assets/ShopItems, which is not a Resources folder, so loading them
        // directly finds nothing and the cache would silently pay out diamonds
        // alone while claiming to be an armoury.
        var locked = WeaponIndex.Unowned();
        if (locked.Count == 0) return;

        var prize = locked[Random.Range(0, locked.Count)];
        PlayerPrefs.SetInt("WeaponUnlocked_" + prize.weaponID, 1);
        PlayerPrefs.Save();

        ToastManager.Show(LocalizationManager.Tr("CACHE_WEAPON_FOUND", prize.weaponName),
                          ToastManager.ToastKind.Achievement);
    }

    private void PutTheTellOut()
    {
        // The landmark has to stop being a landmark the moment it is spent, or
        // the player keeps walking back to a light that means nothing.
        if (_shaft != null) _shaft.enabled = false;
        if (_glow != null) _glow.enabled = false;
        if (_pip != null) _pip.enabled = false;
    }

    private void WakeTheRegion()
    {
        var alerts = RegionAlertDirector.Instance;
        if (alerts == null) return;
        alerts.RaiseAlert(transform.position, transform);
    }
}
