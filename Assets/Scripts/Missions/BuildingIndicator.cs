using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи з Image!

public class BuildingIndicator : MonoBehaviour
{
    [Header("Settings")]
    public float visibleDistance = 20f; // З якої відстані починає з'являтися

    [Tooltip("Наскільки точно треба дивитись на об'єкт (1 = ідеально по центру, 0.5 = можна краєм ока)")]
    [Range(0.1f, 1f)]
    public float lookThreshold = 0.7f;

    private Image iconImage;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        iconImage = GetComponent<Image>();

        // Робимо іконку невидимою на старті
        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = 0f;
            iconImage.color = c;
        }
    }

    void Update()
    {
        if (mainCam == null || iconImage == null) return;

        // 1. Перевіряємо дистанцію від камери до іконки
        float dist = Vector3.Distance(transform.position, mainCam.transform.position);

        // 2. Перевіряємо, чи ДИВИТЬСЯ камера на будівлю (Dot Product)
        Vector3 dirToIcon = (transform.position - mainCam.transform.position).normalized;
        float lookDot = Vector3.Dot(mainCam.transform.forward, dirToIcon);

        float targetAlpha = 0f;

        // Якщо ми достатньо близько І дивимось у бік будівлі
        if (dist <= visibleDistance && lookDot >= lookThreshold)
        {
            // Прозорість від дистанції (чим ближче, тим чіткіше)
            float distAlpha = Mathf.Clamp01((visibleDistance - dist) / 5f);

            // Прозорість від погляду (якщо дивимось прямо - 100% видно, якщо відвертаємось - плавно зникає)
            float lookAlpha = Mathf.Clamp01((lookDot - lookThreshold) / (1f - lookThreshold));

            // Беремо найменше значення, щоб обидва фактори працювали гармонійно
            targetAlpha = Mathf.Min(distAlpha, lookAlpha);
        }

        // 3. Застосовуємо плавну зміну прозорості (Lerp робить це дуже м'яко)
        Color c = iconImage.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 8f);
        iconImage.color = c;

        // 4. Ефект Білборду (іконка завжди повертається лицем до екрану гравця)
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                         mainCam.transform.rotation * Vector3.up);
    }
}