using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws every revealed MapEventMarker onto the minimap.
//
// It borrows the minimap's geometry from the tracker that is already wired to it
// rather than asking for its own references. That is deliberate: this has to
// work in a scene nobody prepared for it, and a component that needs three
// inspector fields filled in is a component that is silently doing nothing in
// half the scenes it lives in — which is exactly how the exploration markers
// would have gone missing the same way the reliquaries did.
//
// Icons are pooled. Markers appear and vanish as the player moves and as chests
// are looted, and creating a UI object per marker per frame would show up in a
// profile long before it showed up as a bug.
[DisallowMultipleComponent]
public class MapMarkerLayer : MonoBehaviour
{
    private RectTransform _map;
    private Camera _cam;
    private Transform _player;
    private MapEventIcons _set;
    private float _radius;

    private readonly List<Image> _pool = new List<Image>(16);
    private float _rebind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<MapMarkerLayer>() != null) return;
        var go = new GameObject("[MapMarkers]");
        DontDestroyOnLoad(go);
        go.AddComponent<MapMarkerLayer>();
    }

    private void LateUpdate()
    {
        if (!Bind()) { HideAll(); return; }
        if (MapEventMarker.All.Count == 0) { HideAll(); return; }

        float unitsInView = _cam.orthographic
            ? _cam.orthographicSize * 2f
            : 2f * _cam.transform.position.y * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        if (unitsInView <= 0.01f) { HideAll(); return; }

        float pixelsPerMetre = _map.sizeDelta.x / unitsInView;
        int used = 0;

        for (int i = 0; i < MapEventMarker.All.Count; i++)
        {
            var m = MapEventMarker.All[i];
            if (m == null || (m.done && m.hideWhenDone)) continue;

            var entry = _set.For(m.kind);
            if (entry == null || entry.revealRadius <= 0f) continue;

            float reveal = m.RevealRadius(_set);
            Vector3 rel = m.transform.position - _player.position;
            float dist = new Vector2(rel.x, rel.z).magnitude;

            // Fades in across the last few metres of the reveal band. A marker
            // that pops on at an exact distance reads as a glitch the first few
            // times you see it happen.
            float alpha;
            if (dist <= reveal - entry.fadeBand) alpha = 1f;
            else if (dist <= reveal) alpha = Mathf.InverseLerp(reveal, reveal - entry.fadeBand, dist);
            else alpha = 0f;

            if (alpha > 0.02f) m.seen = true;
            else if (m.seen && entry.rememberOnceSeen) alpha = 0.55f;   // remembered, but dimmer than live
            else continue;

            var icon = Rent(used++);
            icon.sprite = entry.icon;
            icon.enabled = true;
            var c = entry.tint; c.a = alpha;
            icon.color = c;

            var rt = icon.rectTransform;
            rt.sizeDelta = new Vector2(entry.size, entry.size);

            Vector2 uiPos = new Vector2(rel.x, rel.z) * pixelsPerMetre;
            // Clamped to the rim, so something outside the minimap's view still
            // tells you which way to walk instead of disappearing.
            if (uiPos.magnitude > _radius) uiPos = uiPos.normalized * _radius;
            rt.anchoredPosition = uiPos;
        }

        for (int i = used; i < _pool.Count; i++) _pool[i].enabled = false;
    }

    // Re-finds the minimap when the scene changes. Throttled, because the failure
    // mode is a scene with no minimap at all and searching for one every frame in
    // the camp would be pure waste.
    private bool Bind()
    {
        if (_map != null && _cam != null && _player != null && _set != null) return true;

        _rebind -= Time.unscaledDeltaTime;
        if (_rebind > 0f) return false;
        _rebind = 1f;

        _set = MapEventIcons.Load();
        if (_set == null) return false;

        var host = FindFirstObjectByType<MinimapIconTracker>();
        if (host == null || host.minimapRect == null) return false;
        _map = host.minimapRect;
        _cam = host.minimapCamera != null
             ? host.minimapCamera
             : FindFirstObjectByType<MinimapCamera>()?.GetComponent<Camera>();
        if (_cam == null) return false;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return false;
        _player = p.transform;

        _radius = (_map.sizeDelta.x / 2f) - 10f;

        // The pool lives under the minimap, so it inherits its mask and moves
        // with it. Anything already pooled from a previous scene is gone.
        _pool.Clear();
        return true;
    }

    private Image Rent(int index)
    {
        while (_pool.Count <= index)
        {
            var go = new GameObject($"MapMarker_{_pool.Count}", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_map, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            _pool.Add(img);
        }
        return _pool[index];
    }

    private void HideAll()
    {
        for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
    }
}
