using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public Light sunLight;
    public float dayDurationInSeconds = 120f; // 2 minutes per full day/night cycle

    [Header("Atmosphere Settings")]
    public Color dayFogColor = new Color(0.2f, 0.3f, 0.4f);
    public Color nightFogColor = new Color(0.02f, 0.02f, 0.05f);

    private void Update()
    {
        if (sunLight == null) return;

        // 1. Rotate the sun
        float rotationAngle = (Time.deltaTime / dayDurationInSeconds) * 360f;
        sunLight.transform.Rotate(Vector3.right, rotationAngle);

        // 2. Calculate if it is day or night based on sun's angle
        // Dot product compares sun's forward direction to pointing straight down
        float timeOfDay = Vector3.Dot(sunLight.transform.forward, Vector3.down);

        // 3. Smoothly blend the fog color
        // timeOfDay is roughly 1 at noon, -1 at midnight. We map it to 0-1 range.
        float blendFactor = Mathf.Clamp01((timeOfDay + 0.2f) / 0.5f);

        RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, blendFactor);
        Camera.main.backgroundColor = RenderSettings.fogColor; // Match skybox to fog
    }
}