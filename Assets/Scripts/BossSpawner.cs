using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Spawns a boss every X minutes. Shows a dramatic warning banner before spawn.
/// Attach to a GameObject in the game scene. Requires a boss prefab with EnemyAI + BossEnemy.
/// </summary>
public class BossSpawner : MonoBehaviour
{
    [Header("Boss Spawn Settings")]
    public GameObject bossPrefab;
    public Transform player;
    public float firstBossTime = 180f; // 3 minutes
    public float bossInterval = 120f;  // Every 2 minutes after first
    public float spawnDistance = 18f;

    [Header("Scaling")]
    public float bossHealthBase = 300f;
    public float bossHealthPerMinute = 100f;
    public float bossDamageBase = 15f;
    public float bossDamagePerMinute = 5f;

    [Header("Warning Banner")]
    public float warningDuration = 2.5f;

    private float nextBossTime;
    private bool warningShown = false;
    private int bossesSpawned = 0;

    // Runtime UI
    private Canvas warningCanvas;
    private CanvasGroup warningGroup;
    private TextMeshProUGUI warningText;
    private TextMeshProUGUI warningSubtext;

    private void Start()
    {
        nextBossTime = firstBossTime;
        BuildWarningUI();
    }

    private void Update()
    {
        if (player == null || bossPrefab == null) return;

        // Show warning 3 seconds before boss
        if (!warningShown && GameManager.survivalTime >= nextBossTime - 3f)
        {
            warningShown = true;
            StartCoroutine(ShowWarning());
        }

        if (GameManager.survivalTime >= nextBossTime)
        {
            SpawnBoss();
            bossesSpawned++;
            nextBossTime = GameManager.survivalTime + bossInterval;
            warningShown = false;
        }
    }

    private void SpawnBoss()
    {
        // Spawn at distance from player
        Vector2 dir = Random.insideUnitCircle.normalized;
        float spawnX = player.position.x + dir.x * spawnDistance;
        float spawnZ = player.position.z + dir.y * spawnDistance;
        float spawnY = 2f;

        if (Terrain.activeTerrain != null)
        {
            Vector3 worldPos = new Vector3(spawnX, 0, spawnZ);
            spawnY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y + 2f;
        }

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
        GameObject bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        // Scale stats based on time survived
        float minutes = GameManager.survivalTime / 60f;
        EnemyAI ai = bossObj.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.maxHealth = bossHealthBase + bossHealthPerMinute * minutes;
            ai.damage = bossDamageBase + bossDamagePerMinute * minutes;
            ai.moveSpeed *= 0.7f; // Bosses are slower but deadlier
        }
    }

    // ─── WARNING BANNER UI ───

    private void BuildWarningUI()
    {
        GameObject canvasObj = new GameObject("BossWarningCanvas");
        canvasObj.transform.SetParent(transform);
        warningCanvas = canvasObj.AddComponent<Canvas>();
        warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warningCanvas.sortingOrder = 90;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        // Warning panel (full width strip in upper third)
        GameObject panelObj = new GameObject("WarningPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.6f);
        panelRect.anchorMax = new Vector2(1, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.5f, 0f, 0f, 0.6f);

        warningGroup = panelObj.AddComponent<CanvasGroup>();
        warningGroup.alpha = 0f;

        // Main warning text
        GameObject textObj = new GameObject("WarningText");
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.4f);
        textRect.anchorMax = new Vector2(1, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        warningText = textObj.AddComponent<TextMeshProUGUI>();
        warningText.text = "BOSS INCOMING";
        warningText.fontSize = 48;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = new Color(1f, 0.2f, 0.2f);
        warningText.alignment = TextAlignmentOptions.Center;

        // Subtext
        GameObject subObj = new GameObject("SubText");
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0, 0f);
        subRect.anchorMax = new Vector2(1, 0.45f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        warningSubtext = subObj.AddComponent<TextMeshProUGUI>();
        warningSubtext.text = "Prepare yourself...";
        warningSubtext.fontSize = 24;
        warningSubtext.color = new Color(1f, 0.6f, 0.6f);
        warningSubtext.alignment = TextAlignmentOptions.Center;
    }

    private IEnumerator ShowWarning()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("bossWarning");

        // Flash in
        float t = 0f;
        float fadeIn = 0.3f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            warningGroup.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }

        // Pulse the text scale for dramatic effect
        t = 0f;
        float pulseTime = warningDuration - 0.6f;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(t * 6f) * 0.08f;
            warningText.rectTransform.localScale = Vector3.one * pulse;

            // Flash alpha
            float flash = 0.6f + Mathf.Sin(t * 8f) * 0.4f;
            warningText.color = new Color(1f, 0.2f, 0.2f, flash);
            yield return null;
        }

        warningText.rectTransform.localScale = Vector3.one;

        // Fade out
        t = 0f;
        float fadeOut = 0.3f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            warningGroup.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }
        warningGroup.alpha = 0f;
    }
}
