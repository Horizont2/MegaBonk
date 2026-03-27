using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class UIStarEffect : MonoBehaviour
{
    [Header("Continuous Spawning Settings")]
    public GameObject starPrefab;
    [Tooltip("Як часто з'являються нові зірки (у секундах)")]
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
        // Отримуємо компонент RectTransform нашого тексту, щоб знати його ширину
        myRect = GetComponent<RectTransform>();
    }

    public void PlayEffect()
    {
        // Якщо ефект вже йде, зупиняємо його перед новим запуском
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

        // Запускаємо безкінечний цикл спавну
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    // Цей метод автоматично спрацьовує, коли панель Level Up ховається
    private void OnDisable()
    {
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
    }

    private IEnumerator SpawnLoop()
    {
        // Безкінечний цикл, поки об'єкт увімкнений
        while (true)
        {
            // Спавнимо одну зірку зліва (-1) і одну справа (1) одночасно
            StartCoroutine(AnimateStar(-1));
            StartCoroutine(AnimateStar(1));

            // Чекаємо перед наступним спавном (використовуємо Realtime, бо гра на паузі)
            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    private IEnumerator AnimateStar(int sideMultiplier)
    {
        GameObject star = Instantiate(starPrefab, transform);
        RectTransform rect = star.GetComponent<RectTransform>();
        Image img = star.GetComponent<Image>();

        // 1. ВИРАХОВУЄМО ПОЗИЦІЮ (Краї тексту)
        // myRect.rect.width / 2f - це рівно половина тексту. 
        // sideMultiplier робить її від'ємною для лівої сторони і додатною для правої
        float edgeX = (myRect.rect.width / 2f) * sideMultiplier;

        // Додаємо трохи випадковості по висоті, щоб вони не летіли ідеально в одну лінію
        float randomY = Random.Range(-20f, 20f);

        rect.anchoredPosition = new Vector2(edgeX, randomY);

        // 2. КОЛІР
        if (img != null && starColors.Length > 0)
        {
            img.color = starColors[Random.Range(0, starColors.Length)];
        }

        // 3. НАПРЯМОК ПОЛЬОТУ
        // Якщо зірка зліва (-1), вона має летіти вліво (кут 180). Якщо справа (1) - вправо (кут 0).
        float baseAngle = (sideMultiplier == 1) ? 0f : 180f;

        // Додаємо випадковий розкид кута (віялом)
        float angle = (baseAngle + Random.Range(-45f, 45f)) * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float speed = Random.Range(minSpeed, maxSpeed);

        float timer = 0f;
        Vector2 currentPos = rect.anchoredPosition;

        // 4. ЦИКЛ АНІМАЦІЇ ПОЛЬОТУ
        while (timer < lifetime)
        {
            if (star == null) yield break;

            timer += Time.unscaledDeltaTime;
            float progress = timer / lifetime;

            // Рух
            currentPos += direction * speed * Time.unscaledDeltaTime;
            rect.anchoredPosition = currentPos;

            // Обертання навколо своєї осі
            rect.Rotate(0, 0, 120f * Time.unscaledDeltaTime);

            // Плавне зникнення та зменшення
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