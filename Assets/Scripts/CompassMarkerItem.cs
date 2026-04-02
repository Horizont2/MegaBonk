using UnityEngine;

public class CompassMarkerItem : MonoBehaviour
{
    private void OnEnable()
    {
        // --- ANTI-CRASH FIX ---
        // If this script is accidentally attached to a UI element (like the radar icon), destroy it
        if (GetComponent<RectTransform>() != null)
        {
            Destroy(this);
            return;
        }

        if (!SmoothCompass.activeMarkers.Contains(transform))
        {
            SmoothCompass.activeMarkers.Add(transform);
        }
    }

    private void OnDisable()
    {
        SmoothCompass.activeMarkers.Remove(transform);
    }
}