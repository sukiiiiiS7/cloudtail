using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// Auto-fixes common layout issues for the help panel and snaps scroll to top on open.
[DisallowMultipleComponent]
public sealed class HelpPanelDoctor : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] RectTransform viewport;            // Viewport (with RectMask2D)
    [SerializeField] RectTransform content;             // Content (top-anchored)
    [SerializeField] MiniVerticalScroller scroller;     // Scroller on the same panel
    [SerializeField] KeyScrollRelay keyRelay;           // Optional

    [Header("Behavior")]
    [SerializeField] bool resetToTopOnOpen = true;

    void Reset()
    {
        // Best-effort auto-wire
        viewport = transform.Find("Viewport") as RectTransform;
        if (viewport)
        {
            var c = viewport.childCount > 0 ? viewport.GetChild(0) as RectTransform : null;
            if (c) content = c;
        }
        scroller = GetComponent<MiniVerticalScroller>();
        keyRelay = GetComponent<KeyScrollRelay>();
    }

    void OnEnable()
    {
        // Release focus to avoid key capture by last selected UI
        EventSystem.current?.SetSelectedGameObject(null);

        FixViewport();
        FixContent();
        scroller?.RecalcAndClamp();
        if (resetToTopOnOpen) scroller?.SnapTop();

        if (keyRelay) keyRelay.enabled = true;
    }

    void OnDisable()
    {
        if (keyRelay) keyRelay.enabled = false;
    }

    void FixViewport()
    {
        if (!viewport) return;

        // Ensure mask + raycast target
        var img = viewport.GetComponent<Image>();
        if (!img) img = viewport.gameObject.AddComponent<Image>();
        img.color = new Color(0,0,0,0);      // transparent
        img.raycastTarget = true;

        if (!viewport.GetComponent<RectMask2D>())
            viewport.gameObject.AddComponent<RectMask2D>();
    }

    void FixContent()
    {
        if (!content) return;

        // Top anchor + pivot
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot     = new Vector2(0f, 1f);

        // Width follows viewport; height by preferred
        var pos = content.anchoredPosition;
        pos.x = 0f; pos.y = 0f;
        content.anchoredPosition = pos;
        var sd = content.sizeDelta;
        sd.x = 0f;
        content.sizeDelta = sd;

        // Ensure ContentSizeFitter vertical preferred
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Normalize TMP label if present
        var tmp = content.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp)
        {
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.margin = new Vector4(12,12,12,12);
        }
    }
}
