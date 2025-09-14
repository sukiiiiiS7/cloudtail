using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Places selected decoration prefabs onto 2D placement surfaces.
/// - UI calls Select(prefab) to choose what to place.
/// - Left click on a surface (in placementMask) -> spawn under decorRoot.
/// - Right click a spawned item -> delete (child of decorRoot, or optional Tag).
///
/// Extras:
/// - Camera→2D ray for accurate hit points (no z drift).
/// - Center or Bottom-on-Ground alignment (toggle + Alt modifier).
/// - Unify sorting for all child SpriteRenderers.
/// - Pixel snap & pixel nudge.
/// - Optional UI blocking; optional tag assignment.
[DisallowMultipleComponent]
public class DecorPlacer : MonoBehaviour
{
    public static DecorPlacer Instance { get; private set; }

    public enum AlignMode { Center, BottomOnGround }

    [Header("Scene wiring")]
    public Camera cam;                      // auto-fill MainCamera if empty
    public Transform decorRoot;             // parent for spawned items
    public LayerMask placementMask;         // World / Sky...

    [Header("Placement alignment")]
    [Tooltip("Default alignment when placing items.")]
    public AlignMode alignMode = AlignMode.Center;    // ← 默认中心对齐
    [Tooltip("Layers considered 'ground' when using BottomOnGround.")]
    public LayerMask groundLayers;                    // 勾 World
    [Tooltip("Hold Alt while clicking to invert the current alignment mode for that one placement.")]
    public bool allowAltToInvert = true;

    [Header("Pixel tuning")]
    public float pixelsPerUnit = 128f;      // match art PPU
    public bool snapToPixelGrid = true;
    [Tooltip("Extra pixel nudge applied after alignment (X,Y in pixels).")]
    public Vector2 pixelNudge = Vector2.zero;

    [Header("Rendering defaults")]
    public string sortingLayerName = "World";
    public int sortingOrder = 20;

    [Header("UI blocking")]
    public bool blockUI = true;
    public bool blockUGUI = false;          // 你场景里先关掉较稳
    public LayerMask uiBlockMask = 0;       // optional UI colliders

    [Header("Spawn tag (optional)")]
    public bool assignTag = false;          // 默认关，不依赖 Tag
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
        if (debugLog) Debug.Log("[Placer] Selected prefab: " + (prefab ? prefab.name : "<null>"));
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
                var uiRay = cam.ScreenPointToRay(mouse.position.ReadValue());
                var uiHit = Physics2D.GetRayIntersection(uiRay, Mathf.Infinity, uiBlockMask);
                if (uiHit.collider) return;
            }
        }

        // LEFT → place
        if (mouse.leftButton.wasPressedThisFrame)
        {
            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, placementMask);
            if (hit.collider) PlaceAt(hit.point, hit.collider);
        }

        // RIGHT → delete
        if (mouse.rightButton.wasPressedThisFrame)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            Vector2 screen = mouse.position.ReadValue();
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
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

    void PlaceAt(Vector2 hitPoint, Collider2D surface)
    {
        // Decide effective alignment for this click (Alt can invert).
        AlignMode effectiveAlign = alignMode;
        if (allowAltToInvert && Keyboard.current != null && Keyboard.current.altKey.isPressed)
            effectiveAlign = (alignMode == AlignMode.Center) ? AlignMode.BottomOnGround : AlignMode.Center;

        // 先在命中点实例化（暂位）
        Vector3 target = new Vector3(hitPoint.x, hitPoint.y, 0f);
        var go = Instantiate(currentPrefab, target, Quaternion.identity, decorRoot ? decorRoot : null);

        // 统一所有 SpriteRenderer 的排序，避免被岛盖住
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (!string.IsNullOrEmpty(sortingLayerName)) r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }

        // 对齐：仅当模式为 BottomOnGround 且命中层属于 groundLayers 时生效
        bool isGround = ((groundLayers.value & (1 << surface.gameObject.layer)) != 0);
        if (effectiveAlign == AlignMode.BottomOnGround && isGround && renderers.Length > 0)
        {
            float maxHalfHeight = 0f; // 实例中所有可见 SR 的最大 extents.y
            foreach (var r in renderers)
                if (r && r.enabled && r.sprite) maxHalfHeight = Mathf.Max(maxHalfHeight, r.bounds.extents.y);

            if (maxHalfHeight > 0f)
            {
                target.y += maxHalfHeight;
                go.transform.position = target;
            }
        }

        // 像素微调（px→world）
        if (pixelNudge != Vector2.zero)
        {
            float ppu = Mathf.Max(1f, pixelsPerUnit);
            go.transform.position += new Vector3(pixelNudge.x / ppu, pixelNudge.y / ppu, 0f);
            target = go.transform.position;
        }

        // 像素对齐（最后一步）
        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            var p = go.transform.position;
            p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
            p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
            go.transform.position = p;
            target = p;
        }

        if (assignTag && !string.IsNullOrEmpty(assignTagName))
        {
            try { go.tag = assignTagName; } catch { /* tag may not exist; ignore */ }
        }

        if (debugLog) Debug.Log("[Placer] Spawned " + go.name + " at " + target +
                                ((effectiveAlign == AlignMode.BottomOnGround && isGround) ? " (bottom-aligned)" : ""));
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
