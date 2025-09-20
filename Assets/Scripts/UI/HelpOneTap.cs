using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// One-stop helper for a non-modal Help panel.
/// - Auto-wire/find/create Viewport/Content/Text
/// - Optional VerticalLayoutGroup on Content (or keep author's offsets)
/// - Height computed from TMP preferred values using the label's REAL rect width (prevents clipping)
/// - Keyboard scrolling: W/S, Arrows, PageUp/Down, Home/End
/// - Does NOT force font size unless lockFontSize is enabled
[DisallowMultipleComponent]
public sealed class HelpOneTap : MonoBehaviour
{
    // ---------- Wiring ----------
    [Header("Optional wiring (may be left empty)")]
    [SerializeField] RectTransform viewport;         // Mask area (should have RectMask2D)
    [SerializeField] RectTransform content;          // Scroll container (moves vertically)
    [SerializeField] TextMeshProUGUI label;          // TMP with full help text

    // ---------- Scroll ----------
    [Header("Scroll behavior")]
    [SerializeField] float speedPxPerSec = 420f;     // Hold speed (px/s)
    [SerializeField] float pageFactor    = 0.9f;     // PageUp/Down = viewportHeight * factor
    [SerializeField] bool  invert        = false;    // Flip direction if needed

    // ---------- Font ----------
    [Header("Font override (optional)")]
    [SerializeField] bool  lockFontSize = false;     // Keep false to respect Inspector font size
    [SerializeField] float fallbackFontSize = 12f;   // Used only if lockFontSize=true and size invalid

    // ---------- Layout knobs ----------
    [Header("Layout enforcement")]
    [SerializeField] bool useLayoutGroup = true;     // true = VerticalLayoutGroup on Content
    [SerializeField] int  contentTopPadding = 0;     // when useLayoutGroup=true, top padding in px
    [SerializeField] bool enforceTopStretch = true;  // keep Text top-stretch anchors
    [SerializeField] bool preserveTextOffsets = true;// keep author's Left/Right/Top on Text
    [SerializeField] bool preserveTextMargins = true;// keep author's TMP margins

    // ---------- Hard lock for Text top inset (optional) ----------
    [Header("Freeze top (optional)")]
    [SerializeField] bool  freezeTop = false;        // if true, pin Text top inset every LateUpdate
    [SerializeField] float topInsetPx = 0f;          // desired top inset (RectTransform offsetMax.y = -top)

    void Reset() => AutoWire();

    void OnEnable()
    {
        // Release focus so keys are not captured by previously selected UI
        EventSystem.current?.SetSelectedGameObject(null);

        AutoWire();
        EnforceViewportMask();
        EnforceContentLayout();
        EnforceLabelFlags();

        ForceReflow();                  // first layout pass
        FallbackIfZeroOrTooShort();     // ensure measurable height even with tiny fonts
        SnapTop();
        LogSizes("[HelpOneTap] OnEnable");
    }

    void OnRectTransformDimensionsChange()
    {
        ForceReflow();
        FallbackIfZeroOrTooShort();
    }

    void Update()
    {
        if (!viewport || !content) return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current; if (kb == null) return;
        float dir = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   dir -= 1f;  // up
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir += 1f;  // down
        if (kb.pageUpKey.wasPressedThisFrame)   ScrollBy(Sign(-PageStep()));
        if (kb.pageDownKey.wasPressedThisFrame) ScrollBy(Sign(+PageStep()));
        if (kb.homeKey.wasPressedThisFrame)     SnapTop();
        if (kb.endKey.wasPressedThisFrame)      SnapBottom();
#else
        float dir = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   dir -= 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir += 1f;
        if (Input.GetKeyDown(KeyCode.PageUp))   ScrollBy(Sign(-PageStep()));
        if (Input.GetKeyDown(KeyCode.PageDown)) ScrollBy(Sign(+PageStep()));
        if (Input.GetKeyDown(KeyCode.Home))     SnapTop();
        if (Input.GetKeyDown(KeyCode.End))      SnapBottom();
#endif
        if (dir != 0f) ScrollBy(Sign(dir * speedPxPerSec * Time.unscaledDeltaTime));
    }

    void LateUpdate()
    {
        // Optional: hard-freeze the Text top inset after all layout passes
        if (!freezeTop || !label) return;
        var rt = label.rectTransform;
        var offMax = rt.offsetMax;      // offsetMax.y = -Top
        offMax.y = -topInsetPx;
        rt.offsetMax = offMax;
    }

    // ---------- wiring & enforcement ----------
    void AutoWire()
    {
        // Viewport
        if (!viewport)
        {
            var t = transform.Find("Viewport") as RectTransform;
            if (!t)
            {
                var mask = GetComponentInChildren<RectMask2D>(true);
                if (mask) t = mask.GetComponent<RectTransform>();
            }
            viewport = t;
        }

        // Content
        if (viewport && !content)
        {
            var t = viewport.Find("Content") as RectTransform;
            if (!t && viewport.childCount > 0) t = viewport.GetChild(0) as RectTransform;
            if (!t)
            {
                var go = new GameObject("Content", typeof(RectTransform));
                t = go.GetComponent<RectTransform>();
                t.SetParent(viewport, false);
            }
            content = t;
        }

        // Label
        if (content && !label) label = content.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!label)
        {
            var go = new GameObject("Text_Help", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(content, false);
            label = go.GetComponent<TextMeshProUGUI>();
            label.text = "HELP (placeholder)";
            label.fontSize = 12f;
        }
    }

    void EnforceViewportMask()
    {
        if (!viewport) return;
        var img = viewport.GetComponent<Image>();
        if (!img) img = viewport.gameObject.AddComponent<Image>();
        img.color = new Color(0,0,0,0);
        img.raycastTarget = true;
        if (!viewport.GetComponent<RectMask2D>()) viewport.gameObject.AddComponent<RectMask2D>();
    }

    void EnforceContentLayout()
    {
        if (!content) return;

        // Top-anchored, horizontal stretch
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot     = new Vector2(0f, 1f);

        // Width follows parent; keep author's vertical offset on Content
        var sd = content.sizeDelta; sd.x = 0f; content.sizeDelta = sd;
        var pos = content.anchoredPosition; pos.x = 0f; /* keep pos.y */ content.anchoredPosition = pos;

        // Height follows preferred size
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Layout group on/off + top padding
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (useLayoutGroup)
        {
            if (!vlg) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, Mathf.Max(0, contentTopPadding), 0);
            vlg.spacing = 0f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
        }
        else
        {
            // Disable immediately and remove to avoid one-frame reflow
            if (vlg)
            {
                vlg.enabled = false;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(vlg);
                else Destroy(vlg);
#else
                Destroy(vlg);
#endif
            }
        }
    }

    void EnforceLabelFlags()
    {
        if (!label) return;
        var rt = label.rectTransform;

        if (enforceTopStretch)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
        }

        // Respect author's Left/Right/Top when requested
        if (!preserveTextOffsets)
        {
            var sd = rt.sizeDelta; sd.x = 0f; rt.sizeDelta = sd;
            var p  = rt.anchoredPosition;
            p.x = 0f; /* keep p.y to retain author's Top */
            rt.anchoredPosition = p;
        }

        // Stable TMP flags
        label.enableAutoSizing   = false;                     // keep Inspector font size
        label.enableWordWrapping = true;
        label.overflowMode       = TextOverflowModes.Overflow;
        label.alignment          = TextAlignmentOptions.TopLeft;

        if (!preserveTextMargins && label.margin == Vector4.zero)
            label.margin = new Vector4(12,12,12,12);

        if (lockFontSize && label.fontSize < 0.1f)
            label.fontSize = fallbackFontSize;
    }

    void ForceReflow()
    {
        if (!label || !content || !viewport) return;
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
    }

    // Uses TMP preferred height based on the label's REAL rect width.
    // Ensures content is at least (viewport.height + 1) so small fonts still allow scrolling.
    void FallbackIfZeroOrTooShort()
    {
        if (!label || !content || !viewport) return;

        float currentH = content.rect.height;
        float viewH    = viewport.rect.height;
        if (currentH > viewH + 0.5f) return; // already tall enough

        float padY   = (label.margin.y + label.margin.w);
        float labelW = Mathf.Max(1f, label.rectTransform.rect.width - (label.margin.x + label.margin.z));
        var preferred = label.GetPreferredValues(label.text, labelW, Mathf.Infinity);
        float prefH   = Mathf.Ceil(preferred.y + padY);

        // Guarantee scrollability when small fonts or short text
        if (prefH <= viewH) prefH = viewH + 1f;

        // Apply heights
        label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, prefH);
        var sd = content.sizeDelta; sd.y = prefH; content.sizeDelta = sd;

        Canvas.ForceUpdateCanvases();
    }

    // ---------- scrolling ----------
    float MaxScroll() => Mathf.Max(0f, content.rect.height - viewport.rect.height);
    float PageStep()  => viewport ? viewport.rect.height * pageFactor : 320f;
    float Sign(float v) => invert ? -v : v;

    void ScrollBy(float deltaPx)
    {
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y + deltaPx, 0f, MaxScroll());
        content.anchoredPosition = pos;
    }
    void SnapTop()    { var p = content.anchoredPosition; p.y = 0f;           content.anchoredPosition = p; }
    void SnapBottom() { var p = content.anchoredPosition; p.y = MaxScroll();  content.anchoredPosition = p; }

    void LogSizes(string tag)
    {
        if (!viewport || !content) return;
        Debug.Log($"{tag}: viewH={viewport.rect.height:F1} contentH={content.rect.height:F1} max={MaxScroll():F1} y={content.anchoredPosition.y:F1}");
    }
}
