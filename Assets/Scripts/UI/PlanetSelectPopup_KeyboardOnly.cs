using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// Keyboard-only planet select popup (modal).
/// - Open via OpenWithSuggestionIndex().
/// - While open, switch with Left/Right or A/D.
/// - ESC/close handled by your dialog manager; this script cleans up on OnDisable().
/// - Hides only "Title"/"Body" siblings (keeps BG visible).
/// - Auto-enables all parents on open so the panel is visible.
/// - Exposes IsOpen for external toggling.
[DisallowMultipleComponent]
public class PlanetSelectPopup_KeyboardOnly : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Dialog panel to show/hide (child under Panel_Dialog, e.g., Panel_PlanetSelect).")]
    public GameObject panel;

    [Tooltip("Shared PlanetSwitcher instance in the scene.")]
    public PlanetSwitcher switcher;

    [Header("UI (optional visuals)")]
    [Tooltip("Optional TMP text for dynamic title. Leave null for a static title outside this script.")]
    public TMP_Text titleText;

    [Tooltip("Single UI Image used as preview; sprite is swapped on index change.")]
    public Image previewImage;

    [Header("Data (index-aligned)")]
    [Tooltip("Preview sprites aligned with PlanetSwitcher.planets order (size = planet count).")]
    public List<Sprite> planetPreviewSprites = new();

    [Tooltip("Display names per index (optional; falls back to \"planet {i}\").")]
    public List<string> planetDisplayNames = new();

    [Header("Lifecycle")]
    [Tooltip("Hide 'panel' on Start.")]
    public bool hideOnStart = true;

    [Tooltip("Temporarily hide bottom bar (if any) while popup is open.")]
    public GameObject bottomBarToHide;

    // --- state ---
    bool _opened;
    public bool IsOpen => _opened;          // expose open state
    Action<int> _onChanged;
    readonly List<GameObject> _hiddenSiblings = new(); // only Title/Body here

    void Awake()
    {
        if (hideOnStart && panel) panel.SetActive(false);
    }

    void OnEnable()
    {
        _onChanged = OnPlanetChanged;
        if (switcher != null) switcher.OnPlanetChanged += _onChanged;
    }

    void OnDisable()
    {
        if (switcher != null && _onChanged != null)
            switcher.OnPlanetChanged -= _onChanged;

        // If the dialog root was disabled externally, restore siblings and close panel.
        if (_opened)
        {
            RestoreSiblingPanels();
            if (bottomBarToHide) bottomBarToHide.SetActive(true);
            if (panel) panel.SetActive(false);
            _opened = false;
        }
    }

    void Update()
    {
        if (!_opened) return;
        var kb = Keyboard.current;
        if (kb == null || switcher == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            switcher.Prev();
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            switcher.Next();
        // ESC handled by your global dialog manager.
    }

    /// Opens the popup and jumps to a suggested index (clamped).
    public void OpenWithSuggestionIndex(int index)
    {
        if (switcher == null) return;

        // Ensure the whole parent chain is active (Panel_PlanetSelect -> Panel_Dialog -> Canvas_Dialog ...).
        EnsureParentsActive(panel != null ? panel.transform : transform);

        // Hide only Title/Body siblings; keep BG visible.
        HideSiblingPanels();

        if (panel)
        {
            panel.SetActive(true);
            var rt = panel.GetComponent<RectTransform>();
            if (rt) rt.anchoredPosition = Vector2.zero; // keep centered
        }

        if (bottomBarToHide) bottomBarToHide.SetActive(false);

        _opened = true;

        int max = Mathf.Max(0, switcher.planets.Count - 1);
        switcher.JumpTo(Mathf.Clamp(index, 0, max));
        RefreshUI();
    }

    /// Closes the popup and restores previously hidden siblings.
    public void Close()
    {
        _opened = false;
        RestoreSiblingPanels();
        if (bottomBarToHide) bottomBarToHide.SetActive(true);
        if (panel) panel.SetActive(false);
    }

    void OnPlanetChanged(int _) { if (_opened) RefreshUI(); }

    void RefreshUI()
    {
        if (switcher == null) return;
        int idx = switcher.Index;

        if (titleText)
        {
            string name = (idx >= 0 && idx < planetDisplayNames.Count && !string.IsNullOrEmpty(planetDisplayNames[idx]))
                ? planetDisplayNames[idx]
                : $"planet {idx}";
            titleText.text = $"Recommended planet: {name}";
        }

        if (previewImage)
        {
            Sprite s = (idx >= 0 && idx < planetPreviewSprites.Count) ? planetPreviewSprites[idx] : null;
            previewImage.sprite = s;
            previewImage.enabled = (s != null);
        }
    }

    // --- helpers ---
    void HideSiblingPanels()
    {
        _hiddenSiblings.Clear();
        if (panel == null) return;

        Transform parent = panel.transform.parent; // Usually Panel_Dialog
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            if (child == panel) continue;

            string n = child.name;
            if (n == "Title" || n == "Body")
            {
                if (child.activeSelf)
                {
                    _hiddenSiblings.Add(child);
                    child.SetActive(false);
                }
            }
        }
    }

    void RestoreSiblingPanels()
    {
        for (int i = 0; i < _hiddenSiblings.Count; i++)
        {
            var go = _hiddenSiblings[i];
            if (go) go.SetActive(true);
        }
        _hiddenSiblings.Clear();
    }

    void EnsureParentsActive(Transform start)
    {
        if (start == null) return;
        for (var t = start; t != null; t = t.parent)
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
    }
}
