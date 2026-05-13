using UnityEngine;
using System.Collections.Generic;

public class FadingObject : MonoBehaviour
{
    private float targetAlpha = 1f;
    private float currentAlpha = 1f;
    private float minAlpha;
    private float fadeSpeed;

    private Renderer[] renderers;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> fadeMaterials = new Dictionary<Renderer, Material[]>();

    private bool isFullyFadedIn = true;

    public void Initialize(float targetMinAlpha, float speed)
    {
        minAlpha = targetMinAlpha;
        fadeSpeed = speed;

        renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (rend is ParticleSystemRenderer) continue;

            originalMaterials[rend] = rend.sharedMaterials;
            Material[] clones = new Material[rend.sharedMaterials.Length];

            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                Material clone = new Material(rend.sharedMaterials[i]);

                clone.SetFloat("_Surface", 1); 
                clone.SetOverrideTag("RenderType", "Transparent");
                clone.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                clone.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                clone.SetInt("_ZWrite", 0);
                clone.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                clones[i] = clone;
            }
            fadeMaterials[rend] = clones;
        }
    }

    public void FadeOut()
    {
        targetAlpha = minAlpha;
        if (isFullyFadedIn)
        {
            isFullyFadedIn = false;
            foreach (Renderer rend in renderers)
            {
                if (fadeMaterials.ContainsKey(rend))
                    rend.materials = fadeMaterials[rend];
            }
        }
    }

    public void FadeIn()
    {
        targetAlpha = 1f;
    }

    private void Update()
    {
        if (isFullyFadedIn) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        foreach (Renderer rend in renderers)
        {
            if (!fadeMaterials.ContainsKey(rend)) continue;

            foreach (Material mat in fadeMaterials[rend])
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = currentAlpha;
                    mat.SetColor("_BaseColor", c);
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color c = mat.GetColor("_Color");
                    c.a = currentAlpha;
                    mat.SetColor("_Color", c);
                }
            }
        }

        if (currentAlpha >= 0.99f && targetAlpha == 1f)
        {
            isFullyFadedIn = true;
            foreach (Renderer rend in renderers)
            {
                if (originalMaterials.ContainsKey(rend))
                    rend.sharedMaterials = originalMaterials[rend];
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var matArray in fadeMaterials.Values)
        {
            foreach (Material mat in matArray) Destroy(mat);
        }
    }
}