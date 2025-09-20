using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Resets ScrollRect to top and clears UI focus when the panel is shown.
[DisallowMultipleComponent]
public sealed class HelpResetOnOpen : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private bool resetToTop = true;

    void Reset() { scroll = GetComponent<ScrollRect>(); }

    void OnEnable()
    {
        EventSystem.current?.SetSelectedGameObject(null);
        if (!scroll) return;

        // Force layout so heights are valid, then snap to top.
        Canvas.ForceUpdateCanvases();
        if (resetToTop) scroll.verticalNormalizedPosition = 1f;
    }
}
