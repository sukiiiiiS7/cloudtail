using UnityEngine;
using UnityEngine.EventSystems;

/// Resets the help panel when it becomes visible.
public sealed class HelpPanelResetOnOpen : MonoBehaviour
{
    [SerializeField] private MiniVerticalScroller scroller;  
    [SerializeField] private bool resetToTop = true;

    private void Reset()
    {
        if (!scroller) scroller = GetComponent<MiniVerticalScroller>();
    }

    private void OnEnable()
    {
        // Release UI focus so keyboard is not captured by any button.
        EventSystem.current?.SetSelectedGameObject(null);

        // Clamp and optionally snap to top.
        if (scroller)
        {
            scroller.RecalcAndClamp();
            if (resetToTop) scroller.SnapTop();
        }
    }
}
