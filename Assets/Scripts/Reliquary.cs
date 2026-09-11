using UnityEngine;

// A place worth walking to, that you can simply see.
//
// ==== WHY NOT A BEAM ====
//
// The first version put a shaft of light over a chest. It works, and it is what
// every game does, and that is the problem: a coloured laser is a HUD element
// wearing a costume. It tells the player "the designer put a reward here"
// instead of "somebody built something here", and once the map has a few of them
// the world reads as a menu.
//
// A reliquary is a structure instead. Banners on tall poles are the landmark —
// they are the one prop that reads from a distance across broken terrain,
// because they are high, coloured, and the eye is very good at spotting a
// vertical line that is not a tree. Rune stones and a cairn make it legible as a
// SHRINE rather than as scenery, so the player understands there is a reason to
// walk over before they can see what is at the centre.
//
// The only light is a lantern at the base, and only because a landmark you can
// see all day and not at night is a landmark that stops working half the time.
//
// ==== AND WHY IT IS NOT GENEROUS ====
//
// Armour is shop stock — see ArmourLootTable for the full argument. Most visits
// pay in supplies. Armour is the uncommon outcome, the good tiers are rare, and
// a reliquary that gives armour gives no diamonds, because handing out both the
// item and the price of the item is how a side system quietly becomes the only
// system that matters.
[DisallowMultipleComponent]
public class Reliquary : MonoBehaviour
{
    [Header("Grade")]
    [Tooltip("A legendary site: bigger silhouette, far better armour odds. Placed rarely — see ReliquaryDirector.")]
    public bool legendary = false;

    [Header("Payout")]
    [Tooltip("Scales the supplies. Armour odds are NOT scaled by this — they live in ArmourLootTable so the economy has one owner.")]
    [Range(0.5f, 3f)] public float richness = 1f;

    private LootChest _chest;
    private Light _lantern;

    public void Bind(LootChest chest)
    {
        _chest = chest;
        if (_chest != null) _chest.Opened += OnOpened;
    }

    private void OnDestroy()
    {
        if (_chest != null) _chest.Opened -= OnOpened;
    }

    // ---- the silhouette ------------------------------------------------------

    // Assembles the shrine around itself. Called by the director once, at
    // placement, with the prefab set it loaded.
    public void Raise(ReliquarySet set)
    {
        if (set == null) return;

        int banners = legendary ? 4 : 2;
        float ringRadius = legendary ? 4.6f : 3.2f;

        // Banners first and furthest out: they are the part that has to be
        // visible from across the valley, so nothing else may occlude them.
        for (int i = 0; i < banners; i++)
        {
            var prefab = set.PickBanner();
            if (prefab == null) break;
            float a = (i / (float)banners) * Mathf.PI * 2f + Mathf.PI * 0.25f;
            Vector3 p = Offset(a, ringRadius);
            var go = Instantiate(prefab, p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), transform);
            Strip(go);
        }

        // Rune stones make it a shrine rather than a picnic. Odd count and
        // uneven spacing so it reads as raised by hand, not placed by a loop.
        int stones = legendary ? 7 : 5;
        for (int i = 0; i < stones; i++)
        {
            var prefab = set.PickRuneStone();
            if (prefab == null) break;
            float a = (i / (float)stones) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            Vector3 p = Offset(a, ringRadius * 0.62f);
            var go = Instantiate(prefab, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            go.transform.localScale *= Random.Range(0.85f, 1.25f);
            Strip(go, keepColliders: true);   // stones are cover; they may block
        }

        // A legendary site is a grave, and it should look like one before the
        // player is close enough to read the chest.
        if (legendary)
        {
            var arch = set.archPrefab;
            if (arch != null) Strip(Instantiate(arch, Offset(0f, ringRadius * 1.25f),
                                                Quaternion.Euler(0f, 180f, 0f), transform), keepColliders: true);
            for (int i = 0; i < 6; i++)
            {
                var bone = set.PickRemains();
                if (bone == null) break;
                Vector3 p = Offset(Random.Range(0f, Mathf.PI * 2f), Random.Range(1.4f, ringRadius));
                var go = Instantiate(bone, p, Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(0f, 360f), Random.Range(-14f, 14f)), transform);
                Strip(go);
            }
        }

        if (set.lanternPrefab != null)
        {
            var lamp = Instantiate(set.lanternPrefab, Offset(Mathf.PI, ringRadius * 0.5f), Quaternion.identity, transform);
            Strip(lamp);
            // One small light, no shadows. A landmark that only exists in
            // daylight stops being a landmark for half the run — but a dozen of
            // these each casting shadows would cost more than the feature.
            var go = new GameObject("ReliquaryLantern");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lamp.transform.localPosition + Vector3.up * 1.6f;
            _lantern = go.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.color = legendary ? new Color(1f, 0.55f, 0.25f) : new Color(1f, 0.83f, 0.55f);
            _lantern.range = legendary ? 12f : 8f;
            _lantern.intensity = legendary ? 3.2f : 2.1f;
            _lantern.shadows = LightShadows.None;
        }
    }

    private Vector3 Offset(float angle, float radius)
    {
        Vector3 local = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        Vector3 world = transform.position + local;
        world.y = GroundAt(world);
        return world;
    }

    // Dressing must not fight the player: no stray triggers, no minimap dots, no
    // physics. Stones keep their colliders because standing behind one is a
    // legitimate thing to want to do.
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

    // ---- the payout ----------------------------------------------------------

    private void OnOpened()
    {
        bool gaveArmour = false;

        if (Random.value < ArmourLootTable.ArmourChance(legendary))
            gaveArmour = GrantArmour();

        // Supplies are the common case and the reason a reliquary is always
        // worth opening. Deliberately modest: this is a top-up, not an income.
        var rm = ResourceManager.Instance;
        if (rm != null)
        {
            float scale = richness * (legendary ? 1.6f : 1f);
            rm.AddStashResources(
                Mathf.RoundToInt(Random.Range(35f, 70f) * scale),
                Mathf.RoundToInt(Random.Range(25f, 55f) * scale),
                Mathf.RoundToInt(Random.Range(15f, 35f) * scale));

            // No diamonds on top of a piece of armour. See ArmourLootTable.
            if (!gaveArmour) rm.AddDiamonds(Mathf.RoundToInt(Random.Range(12f, 26f) * scale));
            rm.UpdateUI();
        }

        if (_lantern != null) _lantern.enabled = false;
        ReliquaryDirector.NoteOpened(this);
    }

    private bool GrantArmour()
    {
        var prize = ArmourLootTable.Roll(legendary);
        if (prize == null) return false;

        PlayerPrefs.SetInt("ArmorUnlocked_" + prize.armorID, 1);
        PlayerPrefs.Save();
        ArmourLootTable.NoteGranted();

        int tier = ArmourLootTable.TierOf(prize);
        RewardReveal.Show(
            prize.icon,
            prize.armorName,
            LocalizationManager.Tr("REVEAL_ARMOUR_SUB",
                                   LocalizationManager.Tr(ArmourLootTable.TierNameKey(tier)),
                                   prize.category.ToString(),
                                   prize.basePower),
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
