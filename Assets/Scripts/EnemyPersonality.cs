using System.Collections.Generic;
using UnityEngine;

// Makes one skeleton different from the skeleton standing next to it.
//
// ==== THE PROBLEM ====
//
// All nine enemy prefabs — minion, warrior, rogue, mage, necromancer, archer and
// all three bosses — share a single AnimatorController with nine states. So every
// enemy in the game idles the same, runs the same, is hit the same and dies the
// same, and a camp of four skeletons is one skeleton drawn four times. Worse,
// they are usually spawned in the same frame, so their animations are in perfect
// lockstep: four figures breathing on the same beat, which reads as a rendering
// glitch rather than as four people.
//
// ==== THE APPROACH: OVERRIDE, DON'T REBUILD ====
//
// An AnimatorOverrideController keeps the state machine exactly as it is and only
// swaps which CLIP each state plays. That is the whole trick. No states are added,
// no transitions are rewired, no controller asset is edited — so none of the ways
// a hand-edited state machine can silently break are available. Each enemy gets
// its own override instance, picks its own clips, and the graph it runs on is
// still the one that has always worked.
//
// It also means gait is a clip swap rather than a new state: a patrol that walks
// and then chases is the same Running state with a different clip in it.
//
// ==== THREE LAYERS OF DIFFERENCE ====
//
//   ARCHETYPE. A warrior should not move like a rogue. Each kind gets its own
//   pools — heavy two-handed swings and a slow deliberate walk, or dual-wield
//   slashes and a quick light one — plus its own speed and build.
//
//   INDIVIDUAL. Within an archetype, each one still rolls its own idle, its own
//   death, its own gait speed and a fractional difference in size.
//
//   PHASE. And then the animators are nudged out of step with each other, which
//   is the single cheapest and most effective of the three. Identical figures
//   moving on different beats stop reading as copies almost entirely.
[DisallowMultipleComponent]
public class EnemyPersonality : MonoBehaviour
{
    public enum Archetype { Minion, Warrior, Rogue, Mage, Necromancer, Archer, Boss }

    [Tooltip("Leave at Auto-detect unless a prefab is named in a way the detector cannot read.")]
    public bool autoDetectArchetype = true;
    public Archetype archetype = Archetype.Minion;

    [Header("Individual variation")]
    [Tooltip("Animation playback speed range. Small numbers: past about 15% either way a shared clip starts to read as slow motion or fast-forward rather than as a different person.")]
    public Vector2 speedJitter = new Vector2(0.94f, 1.07f);
    [Tooltip("Build. Deliberately tiny — this scales the collider and the hitbox with the model, so anything larger changes how the fight plays, not just how it looks.")]
    public Vector2 scaleJitter = new Vector2(0.97f, 1.03f);
    public bool varyScale = true;

    private Animator _animator;
    private AnimatorOverrideController _override;
    private EnemyAnimationSet _set;
    private System.Random _rng;

    // The clip names the base controller uses, which are the KEYS an override is
    // addressed by. An override maps original-clip to replacement, so getting a
    // name wrong here is a silent no-op rather than an error — hence reading them
    // off the controller at startup instead of assuming.
    private string _idleKey, _runKey, _hitKey, _deathKey, _attackKey;

    private AnimationClip _walkClip, _runClip;
    private bool _walking = true;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null || _animator.runtimeAnimatorController == null) { enabled = false; return; }

        _set = EnemyAnimationSet.Load();
        if (_set == null) { enabled = false; return; }

        // Seeded per instance so a given enemy is consistent with itself across a
        // frame, while two spawned in the same frame still differ.
        _rng = new System.Random(GetInstanceID());

        if (autoDetectArchetype) archetype = Detect(gameObject.name);

        BuildOverride();
        ApplyBody();
        Desync();
    }

    private static Archetype Detect(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("boss")) return Archetype.Boss;
        if (n.Contains("necro")) return Archetype.Necromancer;
        if (n.Contains("mage")) return Archetype.Mage;
        if (n.Contains("archer") || n.Contains("bow")) return Archetype.Archer;
        if (n.Contains("rogue")) return Archetype.Rogue;
        if (n.Contains("warrior")) return Archetype.Warrior;
        return Archetype.Minion;
    }

    private void BuildOverride()
    {
        _override = new AnimatorOverrideController(_animator.runtimeAnimatorController);

        // Read the real key names off the controller. They are matched loosely so
        // a renamed clip does not quietly disable the whole system.
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        _override.GetOverrides(pairs);
        foreach (var p in pairs)
        {
            if (p.Key == null) continue;
            string n = p.Key.name.ToLowerInvariant();
            if (_idleKey == null && n.Contains("idle") && !n.Contains("dizzy") && !n.Contains("aim")) _idleKey = p.Key.name;
            else if (_runKey == null && (n.Contains("running") || n.Contains("run"))) _runKey = p.Key.name;
            else if (_hitKey == null && n.Contains("hit")) _hitKey = p.Key.name;
            else if (_deathKey == null && (n.Contains("death") || n.Contains("die"))) _deathKey = p.Key.name;
            else if (_attackKey == null && (n.Contains("attack") || n.Contains("stab") || n.Contains("slice"))) _attackKey = p.Key.name;
        }

        AnimationClip idle = PickIdle();
        AnimationClip death = EnemyAnimationSet.Pick(_set.deaths, _rng);
        AnimationClip attack = PickAttack();
        _walkClip = PickWalk();
        _runClip = PickRun();

        Set(_idleKey, idle);
        Set(_deathKey, death);
        Set(_attackKey, attack);
        Set(_hitKey, EnemyAnimationSet.Pick(_set.hits, _rng));
        Set(_runKey, _walkClip ?? _runClip);   // starts calm; SetGait corrects it

        _animator.runtimeAnimatorController = _override;
    }

    private void Set(string key, AnimationClip clip)
    {
        if (string.IsNullOrEmpty(key) || clip == null) return;
        _override[key] = clip;
    }

    // ---- archetype flavour ---------------------------------------------------

    private AnimationClip PickIdle()
    {
        switch (archetype)
        {
            case Archetype.Boss:
            case Archetype.Warrior:
                return First(_set.idles, "Idle_A") ?? EnemyAnimationSet.Pick(_set.idles, _rng);
            case Archetype.Rogue:
            case Archetype.Mage:
            case Archetype.Necromancer:
                return First(_set.idles, "Idle_B") ?? EnemyAnimationSet.Pick(_set.idles, _rng);
            case Archetype.Archer:
                return First(_set.bow, "Ranged_Bow_Aiming_Idle") ?? EnemyAnimationSet.Pick(_set.idles, _rng);
            default:
                // The rank and file get the shambling skeleton idle where it
                // exists, and roll freely otherwise — they are the ones there are
                // most of, so they are the ones variety matters most for.
                return First(_set.idles, "Skeletons_Idle") ?? EnemyAnimationSet.Pick(_set.idles, _rng);
        }
    }

    private AnimationClip PickWalk()
    {
        switch (archetype)
        {
            case Archetype.Boss:
            case Archetype.Warrior:   return First(_set.walks, "Walking_A") ?? EnemyAnimationSet.Pick(_set.walks, _rng);
            case Archetype.Rogue:     return First(_set.walks, "Walking_B") ?? EnemyAnimationSet.Pick(_set.walks, _rng);
            case Archetype.Minion:    return First(_set.walks, "Skeletons_Walking") ?? First(_set.walks, "Walking_C")
                                             ?? EnemyAnimationSet.Pick(_set.walks, _rng);
            default:                  return EnemyAnimationSet.Pick(_set.walks, _rng);
        }
    }

    private AnimationClip PickRun()
    {
        if (archetype == Archetype.Archer && _set.runHoldingBow != null) return _set.runHoldingBow;
        if (archetype == Archetype.Rogue) return First(_set.runs, "Running_B") ?? EnemyAnimationSet.Pick(_set.runs, _rng);
        return EnemyAnimationSet.Pick(_set.runs, _rng);
    }

    private AnimationClip PickAttack()
    {
        switch (archetype)
        {
            case Archetype.Boss:        return EnemyAnimationSet.Pick(_set.bossAttacks, _rng)
                                            ?? EnemyAnimationSet.Pick(_set.attacks2H, _rng);
            case Archetype.Warrior:     return EnemyAnimationSet.Pick(_set.attacks2H, _rng);
            case Archetype.Rogue:       return EnemyAnimationSet.Pick(_set.attacksDualWield, _rng)
                                            ?? EnemyAnimationSet.Pick(_set.attacks1H, _rng);
            case Archetype.Mage:        return EnemyAnimationSet.Pick(_set.spellcasts, _rng);
            case Archetype.Necromancer: return _set.summon ?? EnemyAnimationSet.Pick(_set.spellcasts, _rng);
            case Archetype.Archer:      return null;   // the bow states already handle it
            default:                    return EnemyAnimationSet.Pick(_set.attacksUnarmed, _rng)
                                            ?? EnemyAnimationSet.Pick(_set.attacks1H, _rng);
        }
    }

    private static AnimationClip First(AnimationClip[] pool, string name)
    {
        if (pool == null) return null;
        foreach (var c in pool) if (c != null && c.name == name) return c;
        return null;
    }

    // ---- body and timing -----------------------------------------------------

    private void ApplyBody()
    {
        float speed = Lerp(speedJitter);
        switch (archetype)
        {
            case Archetype.Boss:    speed *= 0.86f; break;   // heavy, deliberate
            case Archetype.Warrior: speed *= 0.94f; break;
            case Archetype.Rogue:   speed *= 1.10f; break;   // light and quick
            case Archetype.Mage:    speed *= 0.97f; break;
        }
        _animator.speed = speed;

        if (!varyScale || archetype == Archetype.Boss) return;   // a boss is the size it was authored
        float s = Lerp(scaleJitter);
        if (archetype == Archetype.Warrior) s *= 1.03f;
        if (archetype == Archetype.Rogue) s *= 0.97f;
        transform.localScale *= s;
    }

    // Nudge the animator off whatever beat everything else spawned on.
    //
    // Animator.Update is the way to do this. Animator.Play(0, layer, time) looks
    // like it should work and does not: hash 0 is not "whatever state is current",
    // it is an invalid state, and the call is silently ignored.
    private void Desync()
    {
        float offset = (float)_rng.NextDouble() * 1.2f;
        _animator.Update(offset);
    }

    private float Lerp(Vector2 range) => Mathf.Lerp(range.x, range.y, (float)_rng.NextDouble());

    // ---- gait ----------------------------------------------------------------

    // Called by EnemyAI as its state changes. Chasing runs; patrolling, searching
    // and walking back to a post do not — which is the whole reason the game's
    // patrols have always looked like they were sprinting to nowhere.
    public void SetGait(bool running)
    {
        if (_override == null) return;
        if (running != _walking) return;   // already in the gait being asked for
        _walking = !running;

        AnimationClip want = running ? _runClip : _walkClip;
        if (want == null || string.IsNullOrEmpty(_runKey)) return;
        _override[_runKey] = want;
    }

    // One-off clip swaps for a specific moment — the necromancer's summon, a
    // boss's slam — without needing a state for each.
    public void UseAttackClip(AnimationClip clip)
    {
        if (clip != null) Set(_attackKey, clip);
    }

    public AnimationClip Taunt => EnemyAnimationSet.Pick(_set != null ? _set.taunts : null, _rng);
}
