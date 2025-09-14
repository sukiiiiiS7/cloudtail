using UnityEngine;

/// Minimal, stable SlotIcon (manual-first + per-item tweaks).
/// - You hand-place each Icon once in the scene; we won't auto-center.
/// - Base size = your manual scale (captured as baseline).
/// - Per-item overrides (nudge/scaleMul) come from ItemIconHint on the item prefab.
/// - If Lock Scale is ON: size = baseline * scaleMul (so hints still work).
/// - If Lock Scale is OFF: size = autoFit(boxSize) * scaleMul.
/// - Nudge is applied relative to a captured baseline position (no accumulation).
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SlotIcon : MonoBehaviour
{
    [Header("Icon source")]
    [Tooltip("If set, overrides the sprite derived from DecorSlot.prefab.")]
    public Sprite iconOverride;

    [Header("Fit box (only used when Lock Scale = OFF)")]
    [Tooltip("Target box size (W,H) in world units. PPU=128 → 0.5 ≈ 64px.")]
    public Vector2 boxSize = new(0.5f, 0.5f);

    [Tooltip("Padding in pixels inside the box (PPU-aware).")]
    public float paddingPixels = 2f;

    [Header("Manual overrides")]
    [Tooltip("If ON, we keep your manual baseline position (plus per-item nudge).")]
    public bool lockPosition = true;

    [Tooltip("If ON, we keep your manual baseline scale (but still apply per-item scaleMul).")]
    public bool lockScale = true;

    [Header("PPU")]
    [Tooltip("If true, use this fixed PPU for math; else use sprite.pixelsPerUnit.")]
    public bool useFixedPPU = true;
    public float fixedPPU = 128f;

    [Header("Per-item overrides (set by InventoryPager from ItemIconHint)")]
    public Vector2 overrideNudgePixels = Vector2.zero; // pixels (+Y up)
    public float   overrideScaleMul    = 1f;           // >1 bigger, <1 smaller

    [Header("Sorting")]
    public string sortingLayerName = "UI";
    public int sortingOrder = 150;

    // refs
    private SpriteRenderer sr;
    private DecorSlot decorSlot;

    // baselines captured from your scene edits (so we never accumulate)
    private Vector3 baselineLocalPos;
    private float   baselineUniformScale = 1f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        decorSlot = GetComponentInParent<DecorSlot>();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        // capture your manual baselines
        baselineLocalPos = transform.localPosition;
        baselineUniformScale = transform.localScale.x; // we assume uniform scale for icons
        if (baselineUniformScale <= 0f) baselineUniformScale = 1f;
    }

    void OnEnable() { Refresh(); }
#if UNITY_EDITOR
    void OnValidate()
    {
        // keep sorting stable in edit mode
        if (!sr) sr = GetComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        // if you tweak in editor, recapture baselines
        baselineLocalPos = transform.localPosition;
        baselineUniformScale = transform.localScale.x > 0f ? transform.localScale.x : 1f;

        if (isActiveAndEnabled) Refresh();
    }
#endif

    public void Refresh()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();

        // 1) choose sprite (override > prefab SR > prefab child SR)
        Sprite s = iconOverride;
        if (!s && decorSlot && decorSlot.prefab)
        {
            var pr = decorSlot.prefab.GetComponent<SpriteRenderer>();
            if (pr && pr.sprite) s = pr.sprite;
            if (!s)
            {
                var ch = decorSlot.prefab.GetComponentInChildren<SpriteRenderer>();
                if (ch && ch.sprite) s = ch.sprite;
            }
        }

        sr.sprite = s;
        if (!s) { sr.enabled = false; return; }
        sr.enabled = true;

        // 2) sizing
        float ppu = useFixedPPU ? Mathf.Max(1f, fixedPPU) : Mathf.Max(1f, s.pixelsPerUnit);
        float scaleTarget;

        if (lockScale)
        {
            // keep your manual baseline scale, still allow per-item scaleMul
            scaleTarget = baselineUniformScale * Mathf.Max(0.0001f, overrideScaleMul <= 0f ? 1f : overrideScaleMul);
        }
        else
        {
            // auto-fit into boxSize, then apply per-item scaleMul
            float padWU = Mathf.Max(0f, paddingPixels) / ppu;

            Bounds b = s.bounds; // local/world units at scale=1
            float sw = Mathf.Max(1e-6f, b.size.x);
            float sh = Mathf.Max(1e-6f, b.size.y);

            float tw = Mathf.Max(0f, boxSize.x - 2f * padWU);
            float th = Mathf.Max(0f, boxSize.y - 2f * padWU);
            float fit = Mathf.Min(tw / sw, th / sh);

            float mul = (overrideScaleMul > 0f) ? overrideScaleMul : 1f;
            scaleTarget = Mathf.Max(0.0001f, fit * mul);
        }

        transform.localScale = new Vector3(scaleTarget, scaleTarget, 1f);

        // 3) position = your baseline + per-item pixel nudge (no accumulation)
        Vector3 pos = baselineLocalPos;
        if (overrideNudgePixels != Vector2.zero)
        {
            float pxWU = 1f / ppu;
            pos += new Vector3(overrideNudgePixels.x * pxWU, overrideNudgePixels.y * pxWU, 0f);
        }

        if (lockPosition)
            transform.localPosition = pos;
        else
            transform.localPosition = pos; // minimal: still use baseline+nudge; we don't auto-center

        // 4) sorting
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;
    }

    // When you change manual placement/size in scene, call this (or toggle the component) to recapture.
    public void RegrabBaselines()
    {
        baselineLocalPos = transform.localPosition;
        baselineUniformScale = transform.localScale.x > 0f ? transform.localScale.x : 1f;
    }

    public void SetSprite(Sprite s) { iconOverride = s; Refresh(); }
}

