using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureSeasonGenerator : EditorWindow
{
    [MenuItem("Assets/Generate ALL Season Textures (6 States)")]
    static void GenerateAllSeasons()
    {
        Texture2D sourceTex = Selection.activeObject as Texture2D;
        if (sourceTex == null) return;

        string assetPath = AssetDatabase.GetAssetPath(sourceTex);
        MakeTextureReadable(assetPath);

        // 1. Рання осінь (Жовто-зелена)
        GenerateTexture(sourceTex, assetPath, new Color(0.7f, 0.75f, 0.3f), "_2_EarlyAutumn");

        // 2. Золота Осінь (Помаранчева)
        GenerateTexture(sourceTex, assetPath, new Color(0.85f, 0.5f, 0.2f), "_3_Autumn");

        // 3. Пізня Осінь (Тьмяна, коричнева, гола земля)
        GenerateTexture(sourceTex, assetPath, new Color(0.6f, 0.5f, 0.4f), "_4_LateAutumn");

        // 4. Зима (Сніг)
        GenerateTexture(sourceTex, assetPath, new Color(0.9f, 0.95f, 1f), "_5_Winter");

        // 5. Відлига / Весна (Брудна, темна зелень із залишками вологи)
        GenerateTexture(sourceTex, assetPath, new Color(0.45f, 0.55f, 0.35f), "_6_Spring");

        AssetDatabase.Refresh();
        Debug.Log("<color=cyan>Успіх! Усі 5 перехідних сезонів згенеровано!</color>");
    }

    [MenuItem("Assets/Generate ALL Season Textures (6 States)", true)]
    static bool ValidateGeneration() { return Selection.activeObject is Texture2D; }

    static void MakeTextureReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void GenerateTexture(Texture2D sourceTex, string assetPath, Color targetColor, string suffix)
    {
        Texture2D newTex = new Texture2D(sourceTex.width, sourceTex.height, sourceTex.format, false);
        newTex.filterMode = FilterMode.Point;
        Color[] pixels = sourceTex.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
            if (h >= 0.15f && h <= 0.45f && s > 0.15f && v > 0.15f)
            {
                Color finalColor = targetColor * v;
                finalColor.a = pixels[i].a;
                pixels[i] = finalColor;
            }
        }

        newTex.SetPixels(pixels);
        newTex.Apply();

        byte[] bytes = newTex.EncodeToPNG();
        string newPath = assetPath.Replace(".png", suffix + ".png").Replace(".jpg", suffix + ".jpg");
        if (!newPath.Contains(suffix)) newPath = assetPath + suffix + ".png";

        File.WriteAllBytes(newPath, bytes);
    }
}