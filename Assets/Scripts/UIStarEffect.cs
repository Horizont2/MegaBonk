using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class UIStarEffect : MonoBehaviour
{
    [Header("Continuous Spawning Settings")]
    public GameObject starPrefab;
    [Tooltip("як часто з'€вл€ютьс€ нов≥ з≥рки (у секундах)")]
    public float spawnInterval = 0.1f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float lifetime = 1.5f;

    [Header("Colors")]
    public Color[] starColors = { Color.yellow, Color.white, new Color(0.5f, 1f, 0.5f) };

    private RectTransform myRect;
    private Coroutine spawnLoopCoroutine;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    public void PlayEffect()
    {
        ClearOldStars();
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        ClearOldStars();
    }

    private void ClearOldStars()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            StartCoroutine(AnimateStar(-1));
            StartCoroutine(AnimateStar(1));

            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private IEnumerator AnimateStar(int sideMultiplier)
    {
        GameObject star = Instantiate(starPrefab, transform);
        RectTransform rect = star.GetComponent<RectTransform>();
        Image img = star.GetComponent<Image>();

        float edgeX = (myRect.rect.width / 2f) * sideMultiplier;

        float randomY = Random.Range(-20f, 20f);

        rect.anchoredPosition = new Vector2(edgeX, randomY);

        if (img != null && starColors.Length > 0)
        {
            img.color = starColors[Random.Range(0, starColors.Length)];
        }

        float baseAngle = (sideMultiplier == 1) ? 0f : 180f;

        float angle = (baseAngle + Random.Range(-45f, 45f)) * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float speed = Random.Range(minSpeed, maxSpeed);

        float timer = 0f;
        Vector2 currentPos = rect.anchoredPosition;

        while (timer < lifetime)
        {
            if (star == null) yield break;

            timer += Time.unscaledDeltaTime;
            float progress = timer / lifetime;

            currentPos += direction * speed * Time.unscaledDeltaTime;
            rect.anchoredPosition = currentPos;

            rect.Rotate(0, 0, 120f * Time.unscaledDeltaTime);
            if (img != null)
            {
                Color c = img.color;
                c.a = Mathf.Lerp(1f, 0f, progress);
                img.color = c;

                float scale = Mathf.Lerp(1f, 0f, progress);
                rect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        Destroy(star);
    }
}