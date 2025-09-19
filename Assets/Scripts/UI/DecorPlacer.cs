using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Places selected decoration prefabs onto 2D placement surfaces.
/// - UI calls Select(prefab) to choose what to place.
/// - Left click -> spawn under decorRoot at exact screen point (Screen→World + OverlapPoint).
/// - Right click -> delete a spawned item (under decorRoot or optional tag).
///
/// Features:
/// - Alignment: Center or Bottom-on-Ground (Alt can invert per click)
/// - Unified sorting for all child SpriteRenderers
/// - Pixel snap & pixel nudge (px → world via PPU)
/// - Optional UI blocking (UGUI ray + collider layer)
[DisallowMultipleComponent]
public class DecorPlacer : MonoBehaviour
{
    public static DecorPlacer Instance { get; private set; }

    public enum AlignMode { Center, BottomOnGround }

    [Header("Scene wiring")]
    public Camera cam;                      // auto-fill MainCamera if empty
    public Transform decorRoot;             // parent for spawned items
    public LayerMask placementMask;         // layers allowed for placing (e.g., World)

    [Header("Placement alignment")]
    [Tooltip("Default alignment when placing items.")]
    public AlignMode alignMode = AlignMode.Center;
    [Tooltip("Considered 'ground' when using BottomOnGround.")]
    public LayerMask groundLayers;
    [Tooltip("Hold Alt to invert current alignment just for this click.")]
    public bool allowAltToInvert = true;

    [Header("Pixel tuning")]
    [Tooltip("Art Pixels Per Unit.")]
    public float pixelsPerUnit = 128f;
    public bool snapToPixelGrid = true;
    [Tooltip("Extra pixel nudge applied after alignment (X,Y in pixels). +Y is up.")]
    public Vector2 pixelNudge = Vector2.zero;

    [Header("Rendering defaults")]
    public string sortingLayerName = "World";
    public int sortingOrder = 20;

    [Header("UI blocking")]
    public bool blockUI = true;             // block when pointer over UGUI or UI colliders
    public bool blockUGUI = false;          
    public LayerMask uiBlockMask = 0;       // optional (UI colliders)

    [Header("Spawn tag (optional)")]
    public bool assignTag = false;
    public string assignTagName = "Decor";

    [Header("Debug")]
    public bool debugLog = false;
    public bool debugHUD = false;

    private GameObject currentPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (!cam) cam = Camera.main;
        if (!decorRoot)
        {
            var found = GameObject.Find("DecorRoot");
            if (found) decorRoot = found.transform;
        }
        if (placementMask.value == 0) placementMask = LayerMask.GetMask("World");
        if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("World");
    }

    public void Select(GameObject prefab)
    {
        currentPrefab = prefab;
        if (debugLog) Debug.Log("[Placer] Selected: " + (prefab ? prefab.name : "<null>"));
    }

    void Update()
    {
        if (currentPrefab == null || cam == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        // UI block
        if (blockUI)
        {
            if (blockUGUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (uiBlockMask.value != 0)
            {
                float z = Mathf.Abs(cam.transform.position.z);
                Vector2 sp = mouse.position.ReadValue();
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, z));
                var uiCols = Physics2D.OverlapPointAll(wp, uiBlockMask);
                if (uiCols != null && uiCols.Length > 0) return;
            }
        }

        // LEFT → place (Screen→World + OverlapPoint exact detection)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            Vector2 sp = mouse.position.ReadValue();
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, z));

            // pick the first collider under cursor that is in placementMask
            Collider2D hitCol = null;
            var cols = Physics2D.OverlapPointAll(wp);
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (IsLayerInMask(placementMask, c.gameObject.layer)) { hitCol = c; break; }
            }

            if (hitCol != null)
                PlaceAt(wp, hitCol); // use exact world point under cursor
        }

        // RIGHT → delete
        if (mouse.rightButton.wasPressedThisFrame)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            Vector2 sp = mouse.position.ReadValue();
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, z));

            var hitCol = Physics2D.OverlapPoint(wp, ~0);
            if (!hitCol) return;

            bool isDecor = (decorRoot && hitCol.transform.IsChildOf(decorRoot));
            if (!isDecor && assignTag && !string.IsNullOrEmpty(assignTagName))
            {
                try { isDecor = hitCol.CompareTag(assignTagName); } catch { isDecor = false; }
            }
            if (isDecor) Destroy(hitCol.gameObject);
        }
    }

    /// Hard-align to visible geometry (combined SpriteRenderer.bounds), not prefab root.
    void PlaceAt(Vector2 hitPoint, Collider2D surface)
    {
        // 1) Decide alignment for this click (Alt inverts)
        AlignMode effectiveAlign = alignMode;
        if (allowAltToInvert && Keyboard.current != null && Keyboard.current.altKey.isPressed)
            effectiveAlign = (alignMode == AlignMode.Center) ? AlignMode.BottomOnGround : AlignMode.Center;

        // 2) Spawn at the hit point (temporary)
        Vector3 pos = new Vector3(hitPoint.x, hitPoint.y, 0f);
        var go = Instantiate(currentPrefab, pos, Quaternion.identity, decorRoot ? decorRoot : null);

        // 3) Normalize SR sorting
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in srs)
        {
            if (!string.IsNullOrEmpty(sortingLayerName)) r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }

        // 4) Combined visible bounds
        bool hasBounds = false;
        Bounds cb = default;
        for (int i = 0; i < srs.Length; i++)
        {
            var r = srs[i];
            if (r && r.enabled && r.sprite)
            {
                if (!hasBounds) { cb = r.bounds; hasBounds = true; }
                else cb.Encapsulate(r.bounds);
            }
        }

        // 5) Hard alignment (center or bottom)
        if (hasBounds)
        {
            bool isGround = IsLayerInMask(groundLayers, surface.gameObject.layer);

            if (effectiveAlign == AlignMode.BottomOnGround && isGround)
            {
                // align bottom edge (cb.min.y) to click Y; keep click X
                float dy = hitPoint.y - cb.min.y;
                pos = go.transform.position + new Vector3(0f, dy, 0f);
            }
            else
            {
                // align visible center to click point (X and Y)
                Vector3 delta = new Vector3(hitPoint.x - cb.center.x, hitPoint.y - cb.center.y, 0f);
                pos = go.transform.position + delta;
            }

            go.transform.position = pos;
        }

        // 6) Pixel nudge (px → world)
        if (pixelNudge != Vector2.zero)
        {
            float ppu = Mathf.Max(1f, pixelsPerUnit);
            go.transform.position += new Vector3(pixelNudge.x / ppu, pixelNudge.y / ppu, 0f);
            pos = go.transform.position;
        }

        // 7) Snap to pixel grid (last)
        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            float ppu = pixelsPerUnit;
            var p = go.transform.position;
            p.x = Mathf.Round(p.x * ppu) / ppu;
            p.y = Mathf.Round(p.y * ppu) / ppu;
            go.transform.position = p;
            pos = p;
        }

        // 8) Optional tag
        if (assignTag && !string.IsNullOrEmpty(assignTagName))
        {
            try { go.tag = assignTagName; } catch { /* tag may not exist; ignore */ }
        }

        if (debugLog)
        {
            bool bottom = (effectiveAlign == AlignMode.BottomOnGround);
            Debug.Log($"[Placer] Spawned {go.name} at {pos} {(bottom ? "(bottom-aligned)" : "(center-aligned)")}.");
        }
    }

    void OnGUI()
    {
        if (!debugHUD) return;
        string prefabName = currentPrefab ? currentPrefab.name : "<none>";
        GUI.Label(new Rect(10, 10, 1400, 22),
            "Placer | Prefab: " + prefabName +
            " | Align: " + alignMode +
            " | Mask: " + MaskNames(placementMask));
    }

    // -------- helpers --------
    private static bool IsLayerInMask(LayerMask mask, int layer)
    {
        return ((mask.value & (1 << layer)) != 0);
    }

    private static string MaskNames(LayerMask m)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            int bit = 1 << i;
            if ((m.value & bit) != 0)
            {
                string n = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(n)) sb.Append(n).Append('|');
            }
        }
        return sb.Length > 0 ? sb.ToString() : "(none)";
    }
}

