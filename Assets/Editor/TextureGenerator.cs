using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureGenerator
{
    // Цей рядок створить нову кнопку у верхньому меню Unity!
    [MenuItem("Tools/Generate Biome Textures")]
    public static void GenerateTextures()
    {
        // Створюємо 4 кольори (Трава, Пісок, Сніг, Камінь)
        CreateColorTexture("GrassTex", new Color(0.29f, 0.43f, 0.19f));
        CreateColorTexture("SandTex", new Color(0.89f, 0.76f, 0.43f));
        CreateColorTexture("SnowTex", new Color(0.95f, 0.97f, 1f));
        CreateColorTexture("RockTex", new Color(0.35f, 0.35f, 0.35f));

        // Оновлюємо папку Assets, щоб картинки з'явилися
        AssetDatabase.Refresh();
        Debug.Log("Текстури успішно згенеровані в головній папці Assets!");
    }

    private static void CreateColorTexture(string name, Color color)
    {
        // Створюємо маленьку картинку 16x16 пікселів
        Texture2D tex = new Texture2D(16, 16);
        Color[] pixels = new Color[16 * 16];

        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;

        tex.SetPixels(pixels);
        tex.Apply();

        // Зберігаємо як PNG файл
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + name + ".png", bytes);
    }
}