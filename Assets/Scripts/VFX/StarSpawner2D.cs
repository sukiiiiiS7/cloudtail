using UnityEngine;
using System.Collections.Generic;

/// 2D star field spawner (WORLD SPACE, SpriteRenderer-based).
/// - Weighted prefabs (e.g., S more frequent, XL rare)
/// - Area modes: Auto fit to Camera.main (orthographic) / World units / Pixels
/// - Size control without touching prefabs:
///     * UniformRange  (scale multiplier range)
///     * FitPixelSize  (make on-screen longer side ≈ X~Y pixels)
/// - Applies SpriteRenderer sorting if requested
/// - Cleans up ONLY what it spawned
/// - Gizmos + editor context menu for quick testing
[DisallowMultipleComponent]
public class StarSpawner2D : MonoBehaviour
{
    public enum AreaMode  { AutoCamera, WorldUnits, Pixels }
    public enum ScaleMode { None, UniformRange, FitPixelSize }

    [System.Serializable]
    public class WeightedPrefab
    {
        [Tooltip("Star prefab (must contain a SpriteRenderer).")]
        public GameObject prefab;

        [Tooltip("Relative spawn weight (higher = more common).")]
        public int weight = 1;
    }

    // ---------------------------- Prefabs & Count ----------------------------
    [Header("Prefabs & Count")]
    [Tooltip("Star prefabs with associated weights (e.g., S/M/L/XL).")]
    public WeightedPrefab[] weightedPrefabs;

    [Tooltip("How many stars to spawn in total.")]
    public int count = 80;

    // ------------------------------ Area Mode --------------------------------
    [Header("Area Mode")]
    [Tooltip("AutoCamera: fit Camera.main (orthographic).\nWorldUnits: use center/size in world units.\nPixels: use pixelCenter/pixelSize converted by pixelsPerUnit.")]
    public AreaMode mode = AreaMode.AutoCamera;

    [Tooltip("Extra world-units margin around camera rect when AutoCamera is used.")]
    public float cameraMargin = 0.5f;

    [Tooltip("World-space rect when mode = WorldUnits.")]
    public Vector2 center = Vector2.zero;
    public Vector2 size   = new Vector2(18f, 10f);

    [Tooltip("Pixel rect when mode = Pixels.")]
    public Vector2 pixelCenter = Vector2.zero;
    public Vector2 pixelSize   = new Vector2(1280f, 720f);

    [Tooltip("Pixels per one world unit (match your sprites' PPU; e.g., 100/128).")]
    public float pixelsPerUnit = 100f;

    // ----------------------------- Parent & Z --------------------------------
    [Header("Parent & Z")]
    [Tooltip("Optional parent for spawned stars; defaults to this transform.")]
    public Transform container;

    [Tooltip("Z position for spawned stars (world space).")]
    public float z = 0f;

    // --------------------------- Sprite Sorting ------------------------------
    [Header("Sorting (SpriteRenderer)")]
    public bool   applySorting      = true;
    public string sortingLayerName  = "Background";
    public int    orderInLayer      = 10;

    // ------------------------------ Scaling ----------------------------------
    [Header("Scale (no prefab edits)")]
    [Tooltip("How to size each spawned star.")]
    public ScaleMode scaleMode = ScaleMode.UniformRange;

    [Tooltip("Used when ScaleMode=UniformRange (multiplies instance scale).")]
    public Vector2 uniformScaleRange = new Vector2(0.12f, 0.28f);

    [Tooltip("Desired on-screen pixel size range for the LONGER side (ScaleMode=FitPixelSize).")]
    public Vector2 pixelSizeRange = new Vector2(2f, 6f); // tiny twinkles

    // ------------------------------ Internals --------------------------------
    readonly List<GameObject> _spawned = new List<GameObject>(256);

    // Ownership marker so we only delete what we created
    class OwnedMarker : MonoBehaviour { public StarSpawner2D owner; }

    // -------------------------------------------------------------------------
    void OnEnable()  { Respawn(); }
    void OnDisable() { CleanupOwned(); }

    /// <summary>Clear then spawn a fresh batch.</summary>
    public void Respawn()
    {
        CleanupOwned();

        if (weightedPrefabs == null || weightedPrefabs.Length == 0)
        {
            Debug.LogWarning("[StarSpawner2D] No weighted prefabs assigned.");
            return;
        }

        int totalWeight = 0;
        foreach (var wp in weightedPrefabs)
            if (wp != null && wp.prefab) totalWeight += Mathf.Max(1, wp.weight);

        if (totalWeight <= 0)
        {
            Debug.LogWarning("[StarSpawner2D] Total weight is zero, nothing to spawn.");
            return;
        }

        Vector2 c, s;
        if (!ComputeRect(out c, out s))
        {
            Debug.LogWarning("[StarSpawner2D] Cannot compute spawn rect; check camera or parameters.");
            return;
        }

        var parent = container ? container : transform;
        Vector2 half = s * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var prefab = PickWeightedPrefab(totalWeight);
            if (!prefab) continue;

            var go = Instantiate(prefab, parent, false);

            // Ownership mark
            var mk = go.AddComponent<OwnedMarker>(); mk.owner = this;

            // Random position within rect (world units)
            float x = Random.Range(c.x - half.x, c.x + half.x);
            float y = Random.Range(c.y - half.y, c.y + half.y);
            go.transform.position = new Vector3(x, y, z);

            // Sorting
            if (applySorting)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
                    sr.sortingOrder = orderInLayer;
                }
            }

            // Size control (does NOT modify prefabs)
            ApplyScale(go);

            _spawned.Add(go);
        }

        if (_spawned.Count == 0)
            Debug.LogWarning("[StarSpawner2D] Spawned 0 objects. Check prefabs/weights/rect/scale.");
    }

    /// <summary>Destroy previously spawned instances (owned only).</summary>
    public void CleanupOwned()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) DestroySafe(_spawned[i]);
        _spawned.Clear();

        var parent = container ? container : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            var mk = c.GetComponent<OwnedMarker>();
            if (mk && mk.owner == this) DestroySafe(c.gameObject);
        }
    }

    // ------------------------------ Helpers ----------------------------------
    GameObject PickWeightedPrefab(int totalWeight)
    {
        int roll = Random.Range(0, totalWeight);
        foreach (var wp in weightedPrefabs)
        {
            if (wp == null || !wp.prefab) continue;
            int w = Mathf.Max(1, wp.weight);
            if (roll < w) return wp.prefab;
            roll -= w;
        }
        return null;
    }

    void ApplyScale(GameObject go)
    {
        switch (scaleMode)
        {
            case ScaleMode.None:
                // keep prefab scale
                break;

            case ScaleMode.UniformRange:
            {
                float s = Mathf.Clamp(Random.Range(uniformScaleRange.x, uniformScaleRange.y), 0.01f, 10f);
                go.transform.localScale = go.transform.localScale * s;
                break;
            }

            case ScaleMode.FitPixelSize:
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (!sr || sr.sprite == null) return;

                // sprite data
                var sp = sr.sprite;
                float ppu = sp.pixelsPerUnit > 0 ? sp.pixelsPerUnit : (pixelsPerUnit > 0 ? pixelsPerUnit : 100f);
                Vector2 px = sp.rect.size;              // pixels
                Vector2 wu = px / ppu;                  // world units at scale=1
                float longerSideWU = Mathf.Max(wu.x, wu.y);

                // target on-screen pixels
                float targetPx = Random.Range(pixelSizeRange.x, pixelSizeRange.y);
                float targetWU = targetPx / ppu;

                float scale = (longerSideWU > 0f) ? (targetWU / longerSideWU) : 1f;
                scale = Mathf.Clamp(scale, 0.01f, 10f);

                go.transform.localScale = go.transform.localScale * scale;
                break;
            }
        }
    }

    bool ComputeRect(out Vector2 c, out Vector2 s)
    {
        switch (mode)
        {
            case AreaMode.AutoCamera:
            {
                var cam = Camera.main;
                if (!cam || !cam.orthographic) { c = center; s = size; return false; }
                float halfH = cam.orthographicSize + cameraMargin;
                float halfW = halfH * cam.aspect + cameraMargin;
                c = cam.transform.position;
                s = new Vector2(halfW * 2f, halfH * 2f);
                return true;
            }
            case AreaMode.WorldUnits:
                c = center; s = size; return true;

            case AreaMode.Pixels:
                if (pixelsPerUnit <= 0f) pixelsPerUnit = 100f;
                c = pixelCenter / pixelsPerUnit;
                s = pixelSize   / pixelsPerUnit;
                return true;
        }
        c = center; s = size; return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Respawn (Editor)")] void _Respawn() => Respawn();

    void OnDrawGizmosSelected()
    {
        Vector2 c, s;
        if (!ComputeRect(out c, out s)) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.20f);
        Vector3 cc = new Vector3(c.x, c.y, z);
        Vector3 sz = new Vector3(s.x, s.y, 0.01f);
        Gizmos.DrawWireCube(cc, sz);
    }
#endif

    static void DestroySafe(Object obj)
    {
        if (!obj) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) Object.DestroyImmediate(obj);
        else Object.Destroy(obj);
#else
        Object.Destroy(obj);
#endif
    }
}
