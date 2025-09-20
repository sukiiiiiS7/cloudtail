using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class KeyScrollRelay : MonoBehaviour
{
    [SerializeField] MiniVerticalScroller scroller;
    [SerializeField] RectTransform viewport;
    [SerializeField] float speed = 420f;      // px/s
    [SerializeField] float pageFactor = 0.9f; // PageUp/Down = viewport * factor
    [SerializeField] bool invert = false;     

    float PageStep => viewport ? viewport.rect.height * pageFactor : 320f;
    void OnEnable()  => scroller?.RecalcAndClamp();

    void Update()
    {
        if (!scroller) return;
        float dir = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   dir -= 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir += 1f;

        if (kb.pageUpKey.wasPressedThisFrame)   scroller.ScrollBy(Sgn(-PageStep));
        if (kb.pageDownKey.wasPressedThisFrame) scroller.ScrollBy(Sgn(+PageStep));
        if (kb.homeKey.wasPressedThisFrame)     scroller.SnapTop();
        if (kb.endKey.wasPressedThisFrame)      scroller.SnapBottom();
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   dir -= 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir += 1f;

        if (Input.GetKeyDown(KeyCode.PageUp))   scroller.ScrollBy(Sgn(-PageStep));
        if (Input.GetKeyDown(KeyCode.PageDown)) scroller.ScrollBy(Sgn(+PageStep));
        if (Input.GetKeyDown(KeyCode.Home))     scroller.SnapTop();
        if (Input.GetKeyDown(KeyCode.End))      scroller.SnapBottom();
#endif
        if (dir != 0f) scroller.ScrollBy(Sgn(dir * speed * Time.unscaledDeltaTime));
    }
    float Sgn(float v) => invert ? -v : v;
}
