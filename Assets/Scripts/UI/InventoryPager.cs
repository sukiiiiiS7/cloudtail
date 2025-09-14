using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Simple pager that fills visible DecorSlot/SlotIcon pairs from a master list.
/// Supports auto-wiring: set slotsRoot to the parent (e.g., "SlotsRow")
/// and it will find child DecorSlot + SlotIcon in hierarchy order.
/// Also supports per-item UI hints via ItemIconHint on the item prefab.
[DisallowMultipleComponent]
public class InventoryPager : MonoBehaviour
{
    [Header("Data source")]
    [Tooltip("All placeable prefabs in display order.")]
    public List<GameObject> items = new List<GameObject>();

    [Tooltip("Optional icon overrides; same length/order as items. Leave empty to auto-pull from prefab SpriteRenderers.")]
    public List<Sprite> itemIcons = new List<Sprite>();

    [Header("Auto wiring (recommended)")]
    [Tooltip("Parent of your visible slots row (e.g., SlotsRow). Leave empty if you fill 'slots' manually.")]
    public Transform slotsRoot;

    [Header("Visible strip (current page)")]
    [Tooltip("DecorSlot components for the visible row (e.g., 5 slots). Leave empty to auto-find from slotsRoot.")]
    public List<DecorSlot> slots = new List<DecorSlot>();

    [Tooltip("SlotIcon for each visible slot (same indexing as 'slots'). Leave empty to auto-find under each slot.")]
    public List<SlotIcon> slotIcons = new List<SlotIcon>();

    [Header("Paging")]
    [Tooltip("Start page index (0-based).")]
    public int startPage = 0;

    [Tooltip("Wrap around when going past first/last page.")]
    public bool wrap = true;

    [Header("UI Hints")]
    [Tooltip("Read ItemIconHint (nudge/scale) from the item's prefab and push to SlotIcon.")]
    public bool useItemHints = true;

    // ----- Runtime state -----
    private int page = 0;

    // ----- Public read-only properties (for widgets like PageDots) -----
    public int Page => page;
    public int PageSize => Mathf.Max(1, slots.Count);
    public int PageCount => Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)PageSize));

    // (page, pageCount) -> fired after any page refresh
    public Action<int, int> OnPageChanged;

    void Awake()
    {
        // Auto-wire slots from slotsRoot if not provided
        if (slots.Count == 0 && slotsRoot != null)
        {
            slots = slotsRoot.GetComponentsInChildren<DecorSlot>(true)
                .OrderBy(s => s.transform.GetSiblingIndex())
                .ToList();
        }

        // Auto-wire icons (under each slot) if not provided
        if (slotIcons.Count == 0 && slots.Count > 0)
        {
            slotIcons = new List<SlotIcon>(slots.Count);
            foreach (var s in slots)
            {
                var icon = s ? s.GetComponentInChildren<SlotIcon>(true) : null;
                slotIcons.Add(icon);
            }
        }

        page = Mathf.Max(0, startPage);
    }

    void Start()
    {
        RefreshPage();
    }

    /// Go to next page (respects wrap)
    public void NextPage() => Go(+1);

    /// Go to previous page (respects wrap)
    public void PrevPage() => Go(-1);

    /// Jump directly to a specific page index (clamped)
    public void JumpTo(int targetPage)
    {
        int pc = PageCount;
        int clamped = Mathf.Clamp(targetPage, 0, pc - 1);
        if (clamped != page)
        {
            page = clamped;
            RefreshPage();
        }
    }

    private void Go(int dir)
    {
        int pageSize = PageSize;
        int pageCount = PageCount;

        int old = page;
        page += dir;

        if (wrap)
        {
            if (page >= pageCount) page = 0;
            if (page < 0) page = pageCount - 1;
        }
        else
        {
            page = Mathf.Clamp(page, 0, pageCount - 1);
        }

        // Debug.Log($"[Pager] page {old} -> {page} / {pageCount} (size={pageSize}, items={items.Count})");
        RefreshPage();
    }

    /// Fill visible row based on current page, toggle unused slots, and notify listeners.
    public void RefreshPage()
    {
        int pageSize = PageSize;

        for (int i = 0; i < pageSize; i++)
        {
            int itemIndex = page * pageSize + i;
            bool hasItem = (itemIndex >= 0 && itemIndex < items.Count);

            var slot = slots[i];
            var icon = (i < slotIcons.Count) ? slotIcons[i] : null;

            if (!slot) continue;

            if (hasItem)
            {
                var prefab = items[itemIndex];
                slot.prefab = prefab;

                // choose icon: explicit override -> prefab SR -> prefab child SR
                Sprite s = null;
                if (itemIcons != null && itemIndex < itemIcons.Count) s = itemIcons[itemIndex];

                if (!s && prefab)
                {
                    var sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr && sr.sprite) s = sr.sprite;
                    if (!s)
                    {
                        var child = prefab.GetComponentInChildren<SpriteRenderer>();
                        if (child) s = child.sprite;
                    }
                }

                if (icon)
                {
                    // 绑定 sprite
                    icon.iconOverride = s;

                    // ★ 下发 per-item UI 微调（来自 prefab 上的 ItemIconHint）
                    if (useItemHints && prefab)
                    {
                        var hint = prefab.GetComponent<ItemIconHint>();
                        if (hint)
                        {
                            icon.overrideNudgePixels = hint.nudgePixels;
                            icon.overrideScaleMul    = (hint.scaleMul > 0f) ? hint.scaleMul : 1f;
                        }
                        else
                        {
                            icon.overrideNudgePixels = Vector2.zero;
                            icon.overrideScaleMul    = 1f;
                        }
                    }
                    else
                    {
                        icon.overrideNudgePixels = Vector2.zero;
                        icon.overrideScaleMul    = 1f;
                    }

                    icon.Refresh();
                }

                slot.gameObject.SetActive(true);
            }
            else
            {
                if (icon)
                {
                    icon.iconOverride = null;
                    icon.overrideNudgePixels = Vector2.zero;
                    icon.overrideScaleMul = 1f;
                    icon.Refresh();
                }
                slot.prefab = null;
                slot.gameObject.SetActive(false);
            }
        }

        // 通知（给页码点之类的）
        OnPageChanged?.Invoke(page, PageCount);
    }
}
