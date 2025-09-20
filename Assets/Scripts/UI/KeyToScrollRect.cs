using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// Drives a ScrollRect vertically with keyboard only (W/S, arrows, PageUp/Down, Home/End).
/// Does not depend on mouse wheel or legacy input module.
[DisallowMultipleComponent]
public sealed class KeyToScrollRect : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform viewport;     // to compute a page step
    [SerializeField] private float speedNormalized = 1.2f; // normalized units per second
    [SerializeField] private float pageFactor = 0.9f;      // PageUp/Down = viewport/content * factor
    [SerializeField] private bool invert = false;          // flip direction if needed

    void Reset()
    {
        scroll = GetComponent<ScrollRect>();
        viewport = scroll ? scroll.viewport : null;
    }

    void Update()
    {
        if (!scroll || !scroll.content || !viewport) return;

        float contentH = Mathf.Max(1f, scroll.content.rect.height);
        float viewH    = Mathf.Max(1f, viewport.rect.height);
        if (contentH <= viewH) return; // nothing to scroll

        float stepNorm = Mathf.Clamp01((viewH / contentH) * pageFactor);
        float dir = 0f;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   dir += 1f; // up = increase normalized
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir -= 1f; // down = decrease
        if (kb.pageUpKey.wasPressedThisFrame)   Nudge(+stepNorm);
        if (kb.pageDownKey.wasPressedThisFrame) Nudge(-stepNorm);
        if (kb.homeKey.wasPressedThisFrame)     SetNorm(1f);
        if (kb.endKey.wasPressedThisFrame)      SetNorm(0f);
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   dir += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir -= 1f;
        if (Input.GetKeyDown(KeyCode.PageUp))   Nudge(+stepNorm);
        if (Input.GetKeyDown(KeyCode.PageDown)) Nudge(-stepNorm);
        if (Input.GetKeyDown(KeyCode.Home))     SetNorm(1f);
        if (Input.GetKeyDown(KeyCode.End))      SetNorm(0f);
#endif

        if (dir != 0f)
        {
            float delta = dir * speedNormalized * Time.unscaledDeltaTime;
            if (invert) delta = -delta;
            SetNorm(scroll.verticalNormalizedPosition + delta);
        }
    }

    void Nudge(float d) => SetNorm(scroll.verticalNormalizedPosition + (invert ? -d : d));
    void SetNorm(float v) => scroll.verticalNormalizedPosition = Mathf.Clamp01(v);
}
