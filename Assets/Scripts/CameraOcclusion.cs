using UnityEngine;
using System.Collections.Generic;

public class CameraOcclusion : MonoBehaviour
{
    [Header("Target & Scanning")]
    public Transform playerTarget;
    public LayerMask foliageLayer;
    public float raycastRadius = 1.5f;

    [Header("Fade Settings")]
    [Range(0f, 1f)] public float fadeAlpha = 0.25f;
    public float fadeSpeed = 4f;

    private List<FadingObject> currentlyFaded = new List<FadingObject>();
    private List<FadingObject> hitsThisFrame = new List<FadingObject>();

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    private void Update()
    {
        if (playerTarget == null) return;

        hitsThisFrame.Clear();

        Vector3 startPos = transform.position;
        Vector3 endPos = playerTarget.position + Vector3.up * 1.5f;

        // ФІКС 1: Обов'язково нормалізуємо напрямок променя! 
        Vector3 dir = (endPos - startPos).normalized;
        float dist = Vector3.Distance(startPos, endPos);

        // ФІКС 2: Віднімаємо 0.5f від дистанції, щоб промінь не чіпляв об'єкти, які знаходяться рівно ЗА спиною гравця
        RaycastHit[] hits = Physics.SphereCastAll(startPos, raycastRadius, dir, dist - 0.5f, foliageLayer);

        foreach (RaycastHit hit in hits)
        {
            // ФІКС 3: Додатково перевіряємо, чи центр об'єкта не знаходиться далі за самого гравця
            if (Vector3.Distance(startPos, hit.transform.position) > dist) continue;

            FadingObject fader = hit.collider.GetComponentInParent<FadingObject>();

            if (fader == null)
            {
                fader = hit.collider.gameObject.AddComponent<FadingObject>();
                fader.Initialize(fadeAlpha, fadeSpeed);
            }

            hitsThisFrame.Add(fader);
            fader.FadeOut();

            if (!currentlyFaded.Contains(fader))
            {
                currentlyFaded.Add(fader);
            }
        }

        for (int i = currentlyFaded.Count - 1; i >= 0; i--)
        {
            FadingObject fader = currentlyFaded[i];
            if (!hitsThisFrame.Contains(fader))
            {
                fader.FadeIn();
                currentlyFaded.RemoveAt(i);
            }
        }
    }
}