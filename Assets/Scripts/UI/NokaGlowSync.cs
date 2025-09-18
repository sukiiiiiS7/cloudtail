using UnityEngine;

/// <summary>
/// Mirrors a source SpriteRenderer (sprite and flip) onto a glow SpriteRenderer,
/// optionally applying additive tint and a subtle breathing alpha.
/// Intended for self-only glow that does not affect other objects.
/// </summary>
[DisallowMultipleComponent]
public class NokaGlowSync : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("Source renderer, e.g., NokaSprite.")]
    public SpriteRenderer source;

    [Tooltip("Glow renderer, e.g., NokaGlow (child).")]
    public SpriteRenderer glow;

    [Header("Sorting")]
    [Tooltip("Match sorting layer with the source renderer.")]
    public bool matchSortingLayer = true;

    [Tooltip("Sorting order offset relative to source.")]
    public int orderOffset = 1;

    [Header("Transform Follow")]
    [Tooltip("Keep glow aligned to source (local space).")]
    public bool followSource = true;

    [Tooltip("Local position offset applied to the glow renderer.")]
    public Vector3 localOffset = Vector3.zero;

    [Tooltip("Local scale multiplier applied to the glow renderer.")]
    public Vector3 localScaleMultiplier = Vector3.one;

    [Header("Glow Style")]
    [Tooltip("Optional additive material; uses glow's current material if null.")]
    public Material additiveMaterial;

    [Tooltip("Base tint for glow. Alpha is the primary intensity control.")]
    public Color tint = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Enables subtle breathing on alpha.")]
    public bool breathing = false;

    [Range(0f, 1f)] public float baseAlpha = 0.25f;
    [Range(0f, 1f)] public float amplitude = 0.15f;
    [Tooltip("Breathing cycles per second.")]
    public float speed = 0.8f;

    Sprite _lastSprite;
    bool _lastFlipX, _lastFlipY;

    void Awake()
    {
        if (glow != null && additiveMaterial != null)
            glow.material = additiveMaterial;
    }

    void LateUpdate()
    {
        if (source == null || glow == null) return;

        // Mirror sprite when changed.
        if (source.sprite != _lastSprite)
        {
            glow.sprite = source.sprite;
            _lastSprite = source.sprite;
        }

        // Mirror flip when changed.
        if (source.flipX != _lastFlipX)
        {
            glow.flipX = source.flipX;
            _lastFlipX = source.flipX;
        }
        if (source.flipY != _lastFlipY)
        {
            glow.flipY = source.flipY;
            _lastFlipY = source.flipY;
        }

        // Sorting / layer.
        if (matchSortingLayer)
        {
            glow.sortingLayerID = source.sortingLayerID;
            glow.sortingLayerName = source.sortingLayerName;
        }
        glow.sortingOrder = source.sortingOrder + orderOffset;

        // Transform follow (local).
        if (followSource)
        {
            glow.transform.localPosition = source.transform.localPosition + localOffset;
            var s = source.transform.localScale;
            glow.transform.localScale = new Vector3(
                s.x * localScaleMultiplier.x,
                s.y * localScaleMultiplier.y,
                s.z * localScaleMultiplier.z);
            glow.transform.localRotation = source.transform.localRotation;
        }

        // Tint/breathing.
        var c = tint;
        if (breathing)
        {
            float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * 0.5f + 0.5f) * amplitude;
            c.a = Mathf.Clamp01(baseAlpha + t);
        }
        glow.color = c;
    }
}
