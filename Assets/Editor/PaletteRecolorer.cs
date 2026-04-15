using UnityEngine;
using UnityEditor;
using System.IO;

public class PaletteRecolorer
{
    // Створюємо нові кнопки в меню по правому кліку миші
    [MenuItem("Assets/Auto-Recolor/❄️ Make Snow Version")]
    public static void MakeSnow() => ProcessTexture(new Color(0.9f, 0.95f, 1f)); // Сніговий колір

    [MenuItem("Assets/Auto-Recolor/🌵 Make Desert Version")]
    public static void MakeDesert() => ProcessTexture(new Color(0.85f, 0.7f, 0.3f)); // Пустельний колір

    private static void ProcessTexture(Color targetColor)
    {
        // Отримуємо картинку, яку ти виділив
        Texture2D selectedTex = Selection.activeObject as Texture2D;
        if (selectedTex == null)
        {
            Debug.LogWarning("Спочатку виділи картинку-текстуру (Palette) у вікні Project!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedTex);

        // Робимо текстуру доступною для читання кодом
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // Створюємо копію картинки
        Texture2D newTex = new Texture2D(selectedTex.width, selectedTex.height);
        newTex.SetPixels(selectedTex.GetPixels());

        // Проходимося по кожному пікселю
        for (int x = 0; x < newTex.width; x++)
        {
            for (int y = 0; y < newTex.height; y++)
            {
                Color c = newTex.GetPixel(x, y);

                // ЛОГІКА: Якщо піксель "зеленуватий" (зеленого більше, ніж червоного і синього)
                if (c.g > c.r && c.g > c.b && c.g > 0.1f)
                {
                    // Фарбуємо його в наш новий колір (сніг або пустеля)
                    newTex.SetPixel(x, y, targetColor);
                }
            }
        }
        newTex.Apply();

        // Формуємо ім'я нового файлу
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string suffix = targetColor.b > 0.8f ? "_Snow" : "_Desert";
        string finalPath = dir + "/" + name + suffix + ".png";

        // Зберігаємо файл
        byte[] bytes = newTex.EncodeToPNG();
        File.WriteAllBytes(finalPath, bytes);

        // Оновлюємо Unity, щоб файл з'явився
        AssetDatabase.Refresh();
        Debug.Log("✅ Успіх! Нова текстура створена: " + finalPath);
    }

    // Ця функція робить так, що кнопки активні ТІЛЬКИ якщо ти виділив картинку
    [MenuItem("Assets/Auto-Recolor/❄️ Make Snow Version", true)]
    [MenuItem("Assets/Auto-Recolor/🌵 Make Desert Version", true)]
    private static bool ValidateTextureSelection()
    {
        return Selection.activeObject is Texture2D;
    }
}