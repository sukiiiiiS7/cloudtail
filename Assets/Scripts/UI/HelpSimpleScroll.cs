using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// Drop-in helper: makes a help panel scrollable with keyboard (W/S, Arrows, PageUp/Down, Home/End).
/// Also auto-fixes anchors/margins and snaps to top when enabled.
[DisallowMultipleComponent]
public sealed class HelpSimpleScroll : MonoBehaviour
{
    [Header("Wire these")]
    [SerializeField] private RectTransform viewport;   // Mask area
    [SerializeField] private RectTransform content;    // Top-anchored content
    [SerializeField] private TextMeshProUGUI label;    // The TMP text to show (full text placed here)

    [Header("Behavior")]
    [SerializeField] private float speedPxPerSec = 420f; // Hold W/S speed (pixels/sec)
    [SerializeField] private float pageFactor   = 0.9f;  // PageUp/Down = viewportHeight * factor
    [SerializeField] private bool  invert       = false; // Flip direction if needed
    [SerializeField] private bool  resetToTopOnEnable = true;

    void Reset()
    {
        // Best-effort auto-wire
        if (!viewport) viewport = transform.Find("Viewport") as RectTransform;
        if (viewport && !content && viewport.childCount > 0)
            content = viewport.GetChild(0) as RectTransform;
        if (!label && content) label = content.GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // release UI focus so keys aren't captured by a button
        EventSystem.current?.SetSelectedGameObject(null);

        FixViewport();
        FixContent();
        FixLabel();

        if (resetToTopOnEnable) SnapTop();
        else Clamp();
    }

    void Update()
    {
        if (!viewport || !content) return;

        float dir = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   dir -= 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir += 1f;

        if (kb.pageUpKey.wasPressedThisFrame)   ScrollBy(Sign(-PageStep()));
        if (kb.pageDownKey.wasPressedThisFrame) ScrollBy(Sign(+PageStep()));
        if (kb.homeKey.wasPressedThisFrame)     SnapTop();
        if (kb.endKey.wasPressedThisFrame)      SnapBottom();
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   dir -= 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir += 1f;

        if (Input.GetKeyDown(KeyCode.PageUp))   ScrollBy(Sign(-PageStep()));
        if (Input.GetKeyDown(KeyCode.PageDown)) ScrollBy(Sign(+PageStep()));
        if (Input.GetKeyDown(KeyCode.Home))     SnapTop();
        if (Input.GetKeyDown(KeyCode.End))      SnapBottom();
#endif
        if (dir != 0f) ScrollBy(Sign(dir * speedPxPerSec * Time.unscaledDeltaTime));
    }

    // -- helpers --
    float MaxScroll()
    {
        float max = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        return max;
    }

    float PageStep() => viewport ? viewport.rect.height * pageFactor : 320f;
    float Sign(float v) => invert ? -v : v;

    void ScrollBy(float deltaPx)
    {
        var p = content.anchoredPosition;
        p.y = Mathf.Clamp(p.y + deltaPx, 0f, MaxScroll());
        content.anchoredPosition = p;
    }

    void SnapTop()    { var p = content.anchoredPosition; p.y = 0f;            content.anchoredPosition = p; }
    void SnapBottom() { var p = content.anchoredPosition; p.y = MaxScroll();   content.anchoredPosition = p; }
    void Clamp()      { var p = content.anchoredPosition; p.y = Mathf.Clamp(p.y, 0f, MaxScroll()); content.anchoredPosition = p; }

    void FixViewport()
    {
        if (!viewport) return;
        // Transparent image so it can receive raycasts
        var img = viewport.GetComponent<Image>();
        if (!img) img = viewport.gameObject.AddComponent<Image>();
        img.color = new Color(0,0,0,0);
        img.raycastTarget = true;

        if (!viewport.GetComponent<RectMask2D>())
            viewport.gameObject.AddComponent<RectMask2D>();
    }

    void FixContent()
    {
        if (!content) return;

        // Top-anchored stretch horizontally
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot     = new Vector2(0f, 1f);

        // Width follows viewport; start at Y=0
        var sd = content.sizeDelta; sd.x = 0f; content.sizeDelta = sd;
        var p  = content.anchoredPosition; p.x = 0f; p.y = 0f; content.anchoredPosition = p;

        // Height grows with text
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
    }

    void FixLabel()
    {
        if (!label) return;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        // 12px margins by default
        if (label.margin == Vector4.zero)
            label.margin = new Vector4(12,12,12,12);
    }
}
