using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// Auto page indicators for InventoryPager.
/// - Instantiates N dots based on pager.PageCount
/// - Highlights the current page
/// - Clicking a dot jumps to that page (requires Physics2D Raycaster + EventSystem)
[DisallowMultipleComponent]
public class PageDots : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("InventoryPager component (usually on UI_Manager).")]
    public InventoryPager pager;

    [Tooltip("Parent transform that will contain the dots. Leave empty to use this GameObject.")]
    public Transform dotsRoot;

    [Header("Visuals")]
    [Tooltip("Sprite for each dot (e.g., an 8x8 or 12x12 pixel circle/star).")]
    public Sprite dotSprite;

    [Tooltip("Horizontal spacing between dots in world units (PPU=128 → 0.125 ≈ 16px).")]
    public float spacing = 0.12f;

    [Tooltip("Base scale for dots.")]
    public float scale = 1f;

    [Tooltip("Scale when a dot is active (current page).")]
    public float activeScale = 1.15f;

    [Tooltip("Color of inactive dots.")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("Color of the active (current) page dot.")]
    public Color activeColor = Color.white;

    [Header("Sorting")]
    [Tooltip("Sorting layer used by the SpriteRenderers.")]
    public string sortingLayerName = "UI";

    [Tooltip("Sorting order used by the SpriteRenderers.")]
    public int sortingOrder = 200;

    private readonly List<SpriteRenderer> dots = new();
    private int cachedPageCount = -1;

    void Awake()
    {
        if (!dotsRoot) dotsRoot = transform;
    }

    void OnEnable()
    {
        if (pager != null)
        {
            pager.OnPageChanged += HandlePageChanged;
        }
        Rebuild();
        if (pager != null)
        {
            HandlePageChanged(pager.Page, pager.PageCount);
        }
    }

    void OnDisable()
    {
        if (pager != null)
        {
            pager.OnPageChanged -= HandlePageChanged;
        }
    }

    private void HandlePageChanged(int page, int pageCount)
    {
        // If page count changed (e.g., items list updated), rebuild the dots.
        if (pageCount != cachedPageCount)
        {
            Rebuild();
        }

        // Highlight the current page.
        for (int i = 0; i < dots.Count; i++)
        {
            var sr = dots[i];
            if (!sr) continue;

            bool active = (i == page);
            sr.color = active ? activeColor : inactiveColor;
            sr.transform.localScale = Vector3.one * (active ? activeScale : scale);
        }
    }

    private void Rebuild()
    {
        // Clear previous dots
        for (int i = dotsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(dotsRoot.GetChild(i).gameObject);
        }
        dots.Clear();

        int pageCount = (pager != null) ? pager.PageCount : 0;
        cachedPageCount = pageCount;

        // If only one page or missing sprite, skip building
        if (pageCount <= 1 || dotSprite == null) return;

        float total = (pageCount - 1) * spacing;
        float startX = -total * 0.5f;

        for (int i = 0; i < pageCount; i++)
        {
            var go = new GameObject($"Dot {i}");
            go.transform.SetParent(dotsRoot, false);
            go.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.color = inactiveColor;

            // Click support: BoxCollider2D + IPointerClickHandler
            var col = go.AddComponent<BoxCollider2D>();
            var size = sr.sprite ? sr.sprite.bounds.size : new Vector3(0.08f, 0.08f, 0f);
            col.size = size;

            var click = go.AddComponent<PageDotClick>();
            click.pager = pager;
            click.pageIndex = i;

            dots.Add(sr);
        }
    }
}

/// Click handler for a single page dot.
/// Requires: Physics 2D Raycaster on the camera + EventSystem in the scene.
public class PageDotClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Target pager to jump on click.")]
    public InventoryPager pager;

    [Tooltip("Page index represented by this dot.")]
    public int pageIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (pager != null) pager.JumpTo(pageIndex);
    }
}
