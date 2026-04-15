using UnityEngine;

public class BiomeWeather : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;

    [Header("Weather Particle Systems")]
    public ParticleSystem leavesEffect;   // Для шару 0 (Трава)
    public ParticleSystem sandEffect;     // Для шару 1 (Пісок)
    public ParticleSystem snowEffect;     // Для шару 2 (Сніг)
    // Гори (шар 3) зазвичай без ефектів

    private int currentDominantTexture = -1;

    private void Start()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
    }

    private void Update()
    {
        if (terrain == null) return;

        // Дізнаємося, який шар зараз під ногами
        int dominantTexture = GetDominantTerrainTexture(transform.position);

        // Якщо біом змінився - оновлюємо погоду
        if (dominantTexture != currentDominantTexture)
        {
            currentDominantTexture = dominantTexture;
            UpdateWeatherEffects(currentDominantTexture);
        }
    }

    private void UpdateWeatherEffects(int textureIndex)
    {
        // Спочатку вимикаємо всі ефекти
        if (leavesEffect != null) leavesEffect.Stop();
        if (sandEffect != null) sandEffect.Stop();
        if (snowEffect != null) snowEffect.Stop();

        // Вмикаємо той, який відповідає біому
        if (textureIndex == 0 && leavesEffect != null) leavesEffect.Play();      // Трава
        else if (textureIndex == 1 && sandEffect != null) sandEffect.Play(); // Пісок
        else if (textureIndex == 2 && snowEffect != null) snowEffect.Play(); // Сніг
    }

    // --- МАГІЯ ЧИТАННЯ ЗЕМЛІ ---
    private int GetDominantTerrainTexture(Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // Переводимо координати світу в координати карти текстур
        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

        // Захист від виходу за межі
        if (mapX < 0 || mapZ < 0 || mapX >= terrainData.alphamapWidth || mapZ >= terrainData.alphamapHeight)
            return 0;

        // Отримуємо "вагу" кожної фарби в цій точці
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
        float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

        for (int i = 0; i < cellMix.Length; i++)
        {
            cellMix[i] = splatmapData[0, 0, i];
        }

        // Знаходимо текстуру, якої тут найбільше
        float maxMix = 0;
        int maxIndex = 0;
        for (int i = 0; i < cellMix.Length; i++)
        {
            if (cellMix[i] > maxMix)
            {
                maxIndex = i;
                maxMix = cellMix[i];
            }
        }

        return maxIndex; // Поверне 0, 1, 2 або 3
    }
}