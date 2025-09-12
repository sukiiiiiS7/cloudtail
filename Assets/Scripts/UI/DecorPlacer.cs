using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Places selected decoration prefabs onto a 2D world surface.
/// - Call Select(prefab) from UI (inventory slots) to choose what to place.
/// - Left click on a collider in placementMask to spawn it under decorRoot.
/// - Right click a placed item to delete (by tag "Decor" OR being child of decorRoot).
[DisallowMultipleComponent]
public class DecorPlacer : MonoBehaviour
{
    public static DecorPlacer Instance { get; private set; }

    [Header("Scene wiring")]
    public Camera cam;                  // leave None; auto-fills at runtime
    public Transform decorRoot;         // parent for spawned items (e.g., "DecorRoot")
    public LayerMask placementMask;     // e.g., only "World" layer

    [Header("Pixel snapping")]
    public float pixelsPerUnit = 128f;
    public bool snapToPixelGrid = true;

    [Header("Rendering defaults for spawned items")]
    public string sortingLayerName = "World";
    public int sortingOrder = 5;

    GameObject currentPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (!Application.isPlaying) return;

        if (!cam) cam = Camera.main;
        if (!decorRoot)
        {
            var found = GameObject.Find("DecorRoot");
            if (found) decorRoot = found.transform;
        }
        if (placementMask.value == 0)
            placementMask = LayerMask.GetMask("World"); // safe default
    }

    /// Select which prefab to place next.
    public void Select(GameObject prefab) => currentPrefab = prefab;

    void Update()
    {
        if (!Application.isPlaying || currentPrefab == null || cam == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Ignore when pointer is over UI.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float zDist = Mathf.Abs(cam.transform.position.z);

        // Left click -> place
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDist));
            Vector2 p = wp;

            var hit = Physics2D.OverlapPoint(p, placementMask);
            if (hit) PlaceAt(p);
        }

        // Right click -> delete (optional)
        if (mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDist));
            var hitCol = Physics2D.OverlapPoint(wp, ~0);
            if (hitCol)
            {
                bool isDecor =
                    hitCol.CompareTag("Decor") ||
                    (decorRoot && hitCol.transform.IsChildOf(decorRoot));
                if (isDecor) Destroy(hitCol.gameObject);
            }
        }
    }

    void PlaceAt(Vector2 pos)
    {
        Vector3 target = new Vector3(pos.x, pos.y, 0f);
        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            target.x = Mathf.Round(target.x * pixelsPerUnit) / pixelsPerUnit;
            target.y = Mathf.Round(target.y * pixelsPerUnit) / pixelsPerUnit;
        }

        var go = Instantiate(currentPrefab, target, Quaternion.identity,
                             decorRoot ? decorRoot : null);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr)
        {
            if (!string.IsNullOrEmpty(sortingLayerName)) sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }

        // Tag is optional; delete logic also checks parent under decorRoot.
        // Create the "Decor" tag once in Project Settings > Tags if you want to use it.
        try { go.tag = "Decor"; } catch { /* tag may not exist; ignore */ }
    }
}
