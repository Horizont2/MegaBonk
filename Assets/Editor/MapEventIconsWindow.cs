using System.IO;
using UnityEditor;
using UnityEngine;

// One window for the whole map-marker set.
//
// The icons could be assigned on each event prefab instead, and that would be
// worse for a reason worth stating: an icon set is only coherent when you can
// see it all at once. Judging whether the barrow reads as more important than
// the wayside chest, or whether the cage and the altar are distinguishable at
// twenty-six pixels, is impossible one prefab at a time. Same for the radii —
// "how far away should the player learn this exists" is a question about the
// whole map, not about one event.
public class MapEventIconsWindow : EditorWindow
{
    private const string Path = "Assets/Resources/" + MapEventIcons.ResourceName + ".asset";

    private MapEventIcons _set;
    private Vector2 _scroll;

    [MenuItem("Tools/World/Map Event Icons")]
    public static void Open()
    {
        var w = GetWindow<MapEventIconsWindow>("Map Event Icons");
        w.minSize = new Vector2(520f, 420f);
        w.Reload();
    }

    private void Reload()
    {
        _set = AssetDatabase.LoadAssetAtPath<MapEventIcons>(Path);
    }

    private void OnGUI()
    {
        if (_set == null)
        {
            EditorGUILayout.HelpBox(
                "No MapEventIcons asset yet. It has to live in a Resources folder — the layer that draws the " +
                "markers is created at runtime and has no inspector for anyone to wire.", MessageType.Info);
            if (GUILayout.Button("Create it", GUILayout.Height(30f))) Create();
            Reload();
            return;
        }

        EditorGUILayout.LabelField("Reveal radius is the dial between a map that spoils everything and a map " +
                                   "that hides the best content. It is per event because the events are not " +
                                   "the same kind of promise.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6f);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        var so = new SerializedObject(_set);
        so.Update();

        foreach (var e in _set.entries)
        {
            if (e == null) continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            // A real preview, tinted the way it will actually appear, because an
            // icon judged as a white thumbnail is an icon judged wrong.
            Rect box = GUILayoutUtility.GetRect(56f, 56f, GUILayout.Width(56f), GUILayout.Height(56f));
            EditorGUI.DrawRect(box, new Color(0.16f, 0.16f, 0.18f));
            if (e.icon != null)
            {
                var tex = AssetPreview.GetAssetPreview(e.icon) ?? e.icon.texture;
                if (tex != null)
                {
                    Color prev = GUI.color;
                    GUI.color = e.tint;
                    GUI.DrawTexture(new Rect(box.x + 6f, box.y + 6f, 44f, 44f), tex, ScaleMode.ScaleToFit);
                    GUI.color = prev;
                }
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(e.kind.ToString(), EditorStyles.boldLabel);
            e.icon = (Sprite)EditorGUILayout.ObjectField("Icon", e.icon, typeof(Sprite), false);
            e.tint = EditorGUILayout.ColorField("Tint", e.tint);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            e.revealRadius = EditorGUILayout.FloatField(
                new GUIContent("Reveal radius (m)", "0 hides it completely — a legitimate choice for anything that should only ever be found by looking."),
                e.revealRadius);
            e.fadeBand = EditorGUILayout.FloatField(
                new GUIContent("Fade band (m)", "Fades in over the last stretch instead of popping on."), e.fadeBand);
            e.size = EditorGUILayout.FloatField(new GUIContent("Icon size (px)"), e.size);
            e.rememberOnceSeen = EditorGUILayout.Toggle(
                new GUIContent("Remember once seen", "Keeps showing it, dimmed, after you walk back out of range."),
                e.rememberOnceSeen);

            EditorGUILayout.LabelField(Describe(e.revealRadius), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);
        if (GUILayout.Button("Save", GUILayout.Height(28f)))
        {
            EditorUtility.SetDirty(_set);
            AssetDatabase.SaveAssets();
            MapEventIcons.ClearCache();
        }
        if (GUI.changed) EditorUtility.SetDirty(_set);
    }

    // Turns a number into the thing the number actually means in play, because
    // "180" says nothing about whether a player will find it.
    private static string Describe(float r) =>
        r <= 0f ? "Hidden — never marked. Found only by looking."
        : r < 90f ? "Incidental — you find it because you were already walking that way."
        : r < 160f ? "Noticeable — a short detour from a route you were taking anyway."
        : r < 280f ? "A destination — visible from far enough that you change your route for it."
        : "Landmark — effectively always known once you are in the region.";

    private static void Create()
    {
        Directory.CreateDirectory("Assets/Resources");
        var set = CreateInstance<MapEventIcons>();
        AssetDatabase.CreateAsset(set, Path);
        AssetDatabase.SaveAssets();
        MapEventIcons.ClearCache();
        Debug.Log($"[MapIcons] Created {Path}. Assign the icons here, then the markers appear on the minimap " +
                  "with no per-prefab wiring.");
    }
}
