using UnityEngine;

// A soft round particle sprite, generated once at runtime.
//
// A particle material with no texture draws every particle as a hard-edged QUAD.
// That is the entire reason dust and debris in this project looked like flying
// Minecraft blocks — nothing to do with the meshes, everything to do with a
// missing sprite. Generating it here rather than shipping a PNG keeps the trailer
// components drop-in: no asset to wire, nothing to forget.
public static class TrailerSoftSprite
{
    private static Texture2D s_tex;

    public static Texture2D Get()
    {
        if (s_tex != null) return s_tex;

        const int N = 64;
        s_tex = new Texture2D(N, N, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "TrailerSoftDot",
        };

        var px = new Color32[N * N];
        float c = (N - 1) * 0.5f;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
            // Squared falloff. A linear ramp still leaves a visible disc edge,
            // which is enough to read as a sprite rather than as dust.
            float a = Mathf.Clamp01(1f - d);
            a *= a;
            px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }

        s_tex.SetPixels32(px);
        s_tex.Apply(true);
        return s_tex;
    }
}
