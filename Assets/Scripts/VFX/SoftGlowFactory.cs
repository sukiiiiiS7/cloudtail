using UnityEngine;

/// Creates a soft radial glow sprite at runtime (no external asset required).
/// Use with a SpriteRenderer for a subtle breathing light behind character.
public static class SoftGlowFactory
{
    /// Generate a radial white glow sprite.
    /// size:   texture size in pixels (64~256 recommended)
    /// gamma:  softness of the falloff (1.8~2.4 looks good)
    /// innerHardness: 0..1 (0 = fully soft center, 1 = dimmer center)
    public static Sprite CreateRadialSprite(int size = 128, float gamma = 2.2f, float innerHardness = 0.15f)
    {
        size = Mathf.Max(8, size);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxR = Mathf.Max(cx, cy);

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / maxR;
                float dy = (y - cy) / maxR;
                float r  = Mathf.Sqrt(dx * dx + dy * dy); // 0..~1.4
                // radial alpha: 1 at center -> 0 at edge
                float a = Mathf.Clamp01(1f - r);
                // gamma curve for softness
                a = Mathf.Pow(a, gamma);
                // soften center slightly (avoid harsh hotspot)
                if (innerHardness > 0f)
                {
                    float k = Mathf.Clamp01(1f - innerHardness);
                    a *= k + (1f - k) * (1f - r);
                }
                byte ba = (byte)Mathf.RoundToInt(a * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, ba);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);

        var rect = new Rect(0, 0, size, size);
        var pivot = new Vector2(0.5f, 0.5f);
        // PPU only affects scale in world; 100 is fine here.
        return Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
    }
}
