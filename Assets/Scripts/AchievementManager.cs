using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Achievement system with persistent tracking and animated popup notifications.
/// Attach to a GameObject in both MainMenu and GameScene.
/// Call AchievementManager.Instance.CheckAll() periodically or after key events.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private List<AchievementDef> achievements;
    private Queue<AchievementDef> popupQueue = new Queue<AchievementDef>();
    private bool isShowingPopup = false;

    // Runtime popup UI (built programmatically)
    private Canvas popupCanvas;
    private RectTransform popupPanel;
    private CanvasGroup popupGroup;
    private TextMeshProUGUI popupTitle;
    private TextMeshProUGUI popupDesc;
    private Image popupIcon;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitAchievements();
        BuildPopupUI();
    }

    // ─── ACHIEVEMENT DEFINITIONS ───

    private void InitAchievements()
    {
        achievements = new List<AchievementDef>
        {
            // --- KILL MILESTONES ---
            new AchievementDef("first_blood", "First Blood",
                "Kill your first enemy", () => GetLifetimeStat("TotalKills") >= 1),
            new AchievementDef("slayer_10", "Getting Started",
                "Kill 10 enemies in total", () => GetLifetimeStat("TotalKills") >= 10),
            new AchievementDef("slayer_50", "Monster Hunter",
                "Kill 50 enemies in total", () => GetLifetimeStat("TotalKills") >= 50),
            new AchievementDef("slayer_100", "Centurion",
                "Kill 100 enemies in total", () => GetLifetimeStat("TotalKills") >= 100),
            new AchievementDef("slayer_500", "Warlord",
                "Kill 500 enemies in total", () => GetLifetimeStat("TotalKills") >= 500),
            new AchievementDef("slayer_1000", "Genocide",
                "Kill 1000 enemies in total", () => GetLifetimeStat("TotalKills") >= 1000),

            // --- SINGLE RUN KILL RECORDS ---
            new AchievementDef("run_kills_25", "Warm Up",
                "Kill 25 enemies in a single run", () => GameStats.totalKills >= 25),
            new AchievementDef("run_kills_50", "Bloodbath",
                "Kill 50 enemies in a single run", () => GameStats.totalKills >= 50),
            new AchievementDef("run_kills_100", "Unstoppable",
                "Kill 100 enemies in a single run", () => GameStats.totalKills >= 100),
            new AchievementDef("run_kills_200", "One Man Army",
                "Kill 200 enemies in a single run", () => GameStats.totalKills >= 200),

            // --- SURVIVAL TIME ---
            new AchievementDef("survive_1min", "Survivor",
                "Survive for 1 minute", () => GameManager.survivalTime >= 60f),
            new AchievementDef("survive_3min", "Endurance",
                "Survive for 3 minutes", () => GameManager.survivalTime >= 180f),
            new AchievementDef("survive_5min", "Iron Will",
                "Survive for 5 minutes", () => GameManager.survivalTime >= 300f),
            new AchievementDef("survive_10min", "Unkillable",
                "Survive for 10 minutes", () => GameManager.survivalTime >= 600f),
            new AchievementDef("survive_15min", "Eternal",
                "Survive for 15 minutes", () => GameManager.survivalTime >= 900f),

            // --- LEVEL MILESTONES ---
            new AchievementDef("level_5", "Apprentice",
                "Reach level 5", () => GameStats.highestLevel >= 5),
            new AchievementDef("level_10", "Veteran",
                "Reach level 10", () => GameStats.highestLevel >= 10),
            new AchievementDef("level_20", "Master",
                "Reach level 20", () => GameStats.highestLevel >= 20),
            new AchievementDef("level_30", "Grandmaster",
                "Reach level 30", () => GameStats.highestLevel >= 30),

            // --- CRYSTAL ECONOMY ---
            new AchievementDef("crystals_50", "Collector",
                "Collect 50 crystals in a single run", () => GameStats.crystalsCollected >= 50),
            new AchievementDef("crystals_200", "Hoarder",
                "Collect 200 crystals in a single run", () => GameStats.crystalsCollected >= 200),
            new AchievementDef("total_crystals_500", "Wealthy",
                "Accumulate 500 total crystals", () => SaveManager.GetTotalCrystals() >= 500),
            new AchievementDef("total_crystals_2000", "Millionaire",
                "Accumulate 2000 total crystals", () => SaveManager.GetTotalCrystals() >= 2000),

            // --- DAMAGE DEALT ---
            new AchievementDef("damage_1000", "Heavy Hitter",
                "Deal 1000 damage in a single run", () => GameStats.totalDamageDealt >= 1000f),
            new AchievementDef("damage_5000", "Destroyer",
                "Deal 5000 damage in a single run", () => GameStats.totalDamageDealt >= 5000f),
            new AchievementDef("damage_10000", "Annihilator",
                "Deal 10000 damage in a single run", () => GameStats.totalDamageDealt >= 10000f),

            // --- BOSS KILLS ---
            new AchievementDef("boss_first", "Boss Slayer",
                "Kill your first boss", () => GetLifetimeStat("TotalBossKills") >= 1),
            new AchievementDef("boss_5", "Boss Hunter",
                "Kill 5 bosses in total", () => GetLifetimeStat("TotalBossKills") >= 5),
            new AchievementDef("boss_10", "Boss Destroyer",
                "Kill 10 bosses in total", () => GetLifetimeStat("TotalBossKills") >= 10),

            // --- META PROGRESSION ---
            new AchievementDef("first_upgrade", "Investor",
                "Buy your first meta upgrade", () => GetTotalMetaLevel() >= 1),
            new AchievementDef("all_upgrades", "Maxed Out",
                "Buy 25 total meta upgrade levels", () => GetTotalMetaLevel() >= 25),

            // --- SPECIAL / FUN ---
            new AchievementDef("no_damage_1min", "Untouched",
                "Survive 1 minute without taking damage", () => GameManager.survivalTime >= 60f && GameStats.totalDamageTaken == 0f),
            new AchievementDef("speedrunner", "Speedrunner",
                "Reach level 10 in under 3 minutes", () => GameStats.highestLevel >= 10 && GameManager.survivalTime <= 180f),
            new AchievementDef("glass_cannon", "Glass Cannon",
                "Deal 5000 damage while having less than 50 max health",
                () => GameStats.totalDamageDealt >= 5000f && GetPlayerMaxHealth() < 50f),
            new AchievementDef("close_call", "Close Call",
                "Survive with less than 5% health", () => GetPlayerHealthPercent() > 0f && GetPlayerHealthPercent() <= 0.05f),
        };
    }

    // ─── CHECK & UNLOCK ───

    public void CheckAll()
    {
        foreach (AchievementDef ach in achievements)
        {
            if (IsUnlocked(ach.id)) continue;
            if (ach.condition())
            {
                Unlock(ach);
            }
        }
    }

    private void Unlock(AchievementDef ach)
    {
        PlayerPrefs.SetInt("ACH_" + ach.id, 1);
        PlayerPrefs.Save();

        // Increment lifetime stats where applicable
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("achievement");

        popupQueue.Enqueue(ach);
        if (!isShowingPopup) StartCoroutine(ShowPopupQueue());
    }

    public bool IsUnlocked(string id)
    {
        return PlayerPrefs.GetInt("ACH_" + id, 0) == 1;
    }

    public List<AchievementDef> GetAllAchievements()
    {
        return achievements;
    }

    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (AchievementDef ach in achievements)
            if (IsUnlocked(ach.id)) count++;
        return count;
    }

    // ─── LIFETIME STAT HELPERS ───

    /// <summary>
    /// Call at end of each run to accumulate lifetime stats.
    /// </summary>
    public void AccumulateRunStats()
    {
        AddLifetimeStat("TotalKills", GameStats.totalKills);
        AddLifetimeStat("TotalBossKills", GameStats.bossKills);
        AddLifetimeStat("TotalRuns", 1);

        float bestTime = PlayerPrefs.GetFloat("BestSurvivalTime", 0f);
        if (GameManager.survivalTime > bestTime)
            PlayerPrefs.SetFloat("BestSurvivalTime", GameManager.survivalTime);

        PlayerPrefs.Save();
    }

    private static int GetLifetimeStat(string key)
    {
        return PlayerPrefs.GetInt("LS_" + key, 0);
    }

    private static void AddLifetimeStat(string key, int amount)
    {
        int current = PlayerPrefs.GetInt("LS_" + key, 0);
        PlayerPrefs.SetInt("LS_" + key, current + amount);
    }

    private static int GetTotalMetaLevel()
    {
        return SaveManager.GetUpgradeLevel("MetaHealth")
             + SaveManager.GetUpgradeLevel("MetaSpeed")
             + SaveManager.GetUpgradeLevel("MetaDamage")
             + SaveManager.GetUpgradeLevel("MetaArmor")
             + SaveManager.GetUpgradeLevel("MetaMagnet");
    }

    private static float GetPlayerMaxHealth()
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        return pc != null ? pc.maxHealth : 999f;
    }

    private static float GetPlayerHealthPercent()
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc == null || pc.maxHealth <= 0) return 1f;
        return pc.currentHealth / pc.maxHealth;
    }

    // ─── PERIODIC CHECK (in-game) ───

    private float checkTimer;

    private void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= 2f)
        {
            checkTimer = 0f;
            CheckAll();

            // Keep highestLevel synced
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null && pc.currentLevel > GameStats.highestLevel)
                GameStats.highestLevel = pc.currentLevel;
        }
    }

    // ─── POPUP UI ───

    private void BuildPopupUI()
    {
        GameObject canvasObj = new GameObject("AchievementPopupCanvas");
        canvasObj.transform.SetParent(transform);
        popupCanvas = canvasObj.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 200;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        // Panel (top center)
        GameObject panelObj = new GameObject("AchPopupPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        popupPanel = panelObj.AddComponent<RectTransform>();
        popupPanel.anchorMin = new Vector2(0.5f, 1f);
        popupPanel.anchorMax = new Vector2(0.5f, 1f);
        popupPanel.pivot = new Vector2(0.5f, 1f);
        popupPanel.sizeDelta = new Vector2(400, 80);
        popupPanel.anchoredPosition = new Vector2(0, 100); // Start off-screen (above)

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        popupGroup = panelObj.AddComponent<CanvasGroup>();
        popupGroup.alpha = 0f;

        HorizontalLayoutGroup hlg = panelObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(15, 15, 10, 10);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Icon placeholder
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(panelObj.transform, false);
        iconObj.AddComponent<RectTransform>();
        iconObj.AddComponent<LayoutElement>().preferredWidth = 50;
        popupIcon = iconObj.AddComponent<Image>();
        popupIcon.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold placeholder

        // Text container
        GameObject textContainer = new GameObject("TextContainer");
        textContainer.transform.SetParent(panelObj.transform, false);
        RectTransform textRect = textContainer.AddComponent<RectTransform>();
        VerticalLayoutGroup vlg = textContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        textContainer.AddComponent<LayoutElement>().preferredWidth = 300;

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(textContainer.transform, false);
        titleObj.AddComponent<RectTransform>();
        titleObj.AddComponent<LayoutElement>().preferredHeight = 30;
        popupTitle = titleObj.AddComponent<TextMeshProUGUI>();
        popupTitle.fontSize = 22;
        popupTitle.fontStyle = FontStyles.Bold;
        popupTitle.color = new Color(1f, 0.85f, 0.2f); // Gold

        // Description text
        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(textContainer.transform, false);
        descObj.AddComponent<RectTransform>();
        descObj.AddComponent<LayoutElement>().preferredHeight = 25;
        popupDesc = descObj.AddComponent<TextMeshProUGUI>();
        popupDesc.fontSize = 16;
        popupDesc.color = new Color(0.75f, 0.75f, 0.75f);
    }

    private IEnumerator ShowPopupQueue()
    {
        isShowingPopup = true;

        while (popupQueue.Count > 0)
        {
            AchievementDef ach = popupQueue.Dequeue();
            popupTitle.text = ach.title;
            popupDesc.text = ach.description;

            // Slide in from top
            float t = 0f;
            float slideInDur = 0.4f;
            while (t < slideInDur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / slideInDur);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                popupPanel.anchoredPosition = new Vector2(0, Mathf.Lerp(100, -10, ease));
                popupGroup.alpha = ease;
                yield return null;
            }

            // Hold
            yield return new WaitForSecondsRealtime(2.5f);

            // Slide out
            t = 0f;
            float slideOutDur = 0.3f;
            while (t < slideOutDur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / slideOutDur);
                popupPanel.anchoredPosition = new Vector2(0, Mathf.Lerp(-10, 100, p));
                popupGroup.alpha = 1f - p;
                yield return null;
            }

            popupGroup.alpha = 0f;

            // Brief pause between popups
            if (popupQueue.Count > 0)
                yield return new WaitForSecondsRealtime(0.3f);
        }

        isShowingPopup = false;
    }
}

// ─── DATA CLASS ───

[System.Serializable]
public class AchievementDef
{
    public string id;
    public string title;
    public string description;
    public System.Func<bool> condition;

    public AchievementDef(string id, string title, string description, System.Func<bool> condition)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.condition = condition;
    }
}
