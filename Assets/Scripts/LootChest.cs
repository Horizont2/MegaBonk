using UnityEngine;
using System.Collections;

public class LootChest : MonoBehaviour
{
    [Header("References")]
    public Animator chestAnimator;

    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Shake Settings")]
    public float shakeDuration = 0.6f;
    public float shakeAmount = 0.15f;

    [Header("Loot Settings")]
    public GameObject[] possibleLoot;
    public int minLootItems = 3;
    public int maxLootItems = 6;

    public float delayForLoot = 1.5f;

    [Header("Destruction")]
    [Tooltip("Seconds after opening before the chest removes itself. Zero or less means it stays — which is what a chest standing inside a hand-built location wants, since the location should not develop a hole in it a few seconds after the player loots it.")]
    public float destroyDelay = 10f;

    private bool isInteracted = false;
    private bool isPromptShowing = false;
    private Transform player;
    private Vector3 originalPos;

    private void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;

        originalPos = transform.position;

        if (chestAnimator == null) chestAnimator = GetComponentInChildren<Animator>(true);

        // Say so out loud when the lid physically cannot move.
        //
        // Both of these failed SILENTLY before — the chest fell back to a plain
        // timer, the loot appeared out of a shut box, and there was nothing in the
        // console to distinguish "no animator" from "animator with no controller"
        // from "it played and you missed it". Every round of "the opening
        // animation still doesn't work" was unfalsifiable as a result.
        if (chestAnimator == null)
            Debug.LogWarning($"[LootChest] '{name}' has no Animator anywhere in its hierarchy — it will open on a " +
                             "timer with the lid shut. The chest models carry the rig but no Animator component; " +
                             "one has to be added and pointed at Fantasy_Polygon_Chest_Animation_Controller.", this);
        else if (chestAnimator.runtimeAnimatorController == null)
            Debug.LogWarning($"[LootChest] '{name}' has an Animator with no controller assigned — SetTrigger(\"Open\") " +
                             "goes nowhere and the lid stays shut.", this);
    }

    private void Update()
    {
        if (isInteracted || player == null || suppressOwnInteraction) return;

        // sqrMagnitude — sqrt was firing every frame per chest.
        float rangeSqr = interactRange * interactRange;
        bool inRange = (transform.position - player.position).sqrMagnitude <= rangeSqr;

        if (inRange)
        {
            // Discoverability: chests had no floating prompt, so players
            // walked past them. Show the standard [E] prompt while in range.
            if (!isPromptShowing && GlobalHUD.Instance != null)
            {
                GlobalHUD.Instance.ShowPrompt(LocalizationManager.Tr("PROMPT_OPEN_CHEST"));
                isPromptShowing = true;
            }
            if (Input.GetKeyDown(interactKey))
            {
                if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
                isPromptShowing = false;
                StartCoroutine(OpenSequence());
            }
        }
        else if (isPromptShowing)
        {
            if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
            isPromptShowing = false;
        }
    }

    // Raised the moment the lid commits, before the loot burst. Lets a Reliquary
    // hang its own payout off an ordinary chest without this class needing to
    // know anything about reliquaries.
    public event System.Action Opened;

    // Raised when the LID IS ACTUALLY UP, at the same instant the loot bursts out.
    //
    // Separate from Opened on purpose. Opened fires when the player commits, which
    // is a second and a half of shake-and-creak before anything visibly happens —
    // paying a reward there means the numbers change while the chest is still
    // shut, and the player reads it as the chest having done nothing. Anything the
    // player is supposed to SEE arrive hangs off this one instead.
    public event System.Action LidOpened;

    public bool IsOpened => isInteracted;

    // Where the loot should appear from: the top of the chest, not its pivot.
    public Vector3 LootOrigin
    {
        get
        {
            var rends = GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return transform.position + Vector3.up * 0.5f;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
    }

    [HideInInspector]
    // Set by a Reliquary, which owns the interaction itself: the chest there is
    // sealed behind guardians and a channel, so its own [E] must not offer a way
    // to skip that.
    //
    // A FLAG, not `enabled = false`, and that distinction is the bug this
    // replaced: a disabled MonoBehaviour cannot start a coroutine, so ForceOpen
    // silently did nothing and the chest opened with no loot at all.
    public bool suppressOwnInteraction = false;

    // Open it from outside, bypassing the [E] prompt.
    public void ForceOpen()
    {
        if (isInteracted) return;
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning($"[LootChest] ForceOpen on '{name}' while the component is disabled — " +
                             "the open coroutine cannot run and the loot would be lost. Use " +
                             "suppressOwnInteraction instead of disabling the component.");
            return;
        }
        if (GlobalHUD.Instance != null) GlobalHUD.Instance.HidePrompt();
        isPromptShowing = false;
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        isInteracted = true;
        Opened?.Invoke();

        // Правильний звук відкриття скрині замість перевикористаного
        // звуку рубання дерева, який тут стояв раніше.
        // MUST be 3D: the WoodChestOpen FMOD event is spatialised, so the 2D
        // PlaySFX played it at world origin (0,0,0) and it attenuated to silence
        // away from there — that's why the chest sound never seemed to play.
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D(AudioID.Env_ChestOpen, transform.position);

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        if (chestAnimator != null)
        {
            int closedHash = chestAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            chestAnimator.SetTrigger("Open");

            // Sync loot to the ACTUAL open animation. The previous version
            // broke out of its wait on the very first frame — right after
            // SetTrigger the animator is still sitting in the CLOSED state
            // (not yet in transition, clip length > 0), so it measured the
            // closed clip and dumped the loot before the lid ever moved.
            //
            // 1) Wait until we actually LEAVE the closed state (transition
            //    into Open has begun).
            float t = 0f;
            while (t < 1.5f)
            {
                var s = chestAnimator.GetCurrentAnimatorStateInfo(0);
                if (chestAnimator.IsInTransition(0) || s.fullPathHash != closedHash) break;
                t += Time.deltaTime;
                yield return null;
            }
            // 2) Wait for the transition to settle onto the Open state.
            t = 0f;
            while (t < 1f && chestAnimator.IsInTransition(0))
            {
                t += Time.deltaTime;
                yield return null;
            }
            // 3) Now genuinely on the Open clip — hold until it's ~90% played
            //    so the loot bursts out as the lid finishes opening.
            var open = chestAnimator.GetCurrentAnimatorStateInfo(0);
            float openLen = (open.length > 0.05f) ? open.length * 0.9f : delayForLoot;
            yield return new WaitForSeconds(openLen);
        }
        else
        {
            yield return new WaitForSeconds(delayForLoot);
        }

        LidOpened?.Invoke();
        SpawnLoot();

        if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
    }

    private void SpawnLoot()
    {
        if (possibleLoot == null || possibleLoot.Length == 0) return;
        // Out of the OPEN MOUTH of the chest. Spawning at the pivot put the burst
        // inside the model, so half of it was hidden and the rest looked like it
        // had squeezed out through the woodwork.
        Vector3 mouth = LootOrigin;
        int count = Random.Range(minLootItems, maxLootItems + 1);
        for (int i = 0; i < count; i++)
        {
            GameObject loot = possibleLoot[Random.Range(0, possibleLoot.Length)];
            if (loot == null) continue;
            Instantiate(loot, mouth, Quaternion.identity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}