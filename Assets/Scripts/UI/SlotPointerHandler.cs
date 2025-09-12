using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Generic pointer handler for 2D interactive elements (slots, arrows, etc.).
/// Works with the new Input System via EventSystem + Physics2DRaycaster.
/// Attach to any object that has a Collider2D. Optionally drive a SlotHighlight.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SlotPointerHandler : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Optional visual driver")]
    [Tooltip("Assign a SlotHighlight on the same GameObject. Will auto-wire if left empty.")]
    public SlotHighlight highlight;

    [Header("Unity Events (optional)")]
    public UnityEvent onPointerEnter;
    public UnityEvent onPointerExit;
    public UnityEvent onClick;

    [Header("Debug")]
    [Tooltip("When enabled, logs clicks to the Console.")]
    public bool debugLogs = false;

    void Awake()
    {
        // Auto-wire highlight if present on the same GameObject
        if (!highlight) highlight = GetComponent<SlotHighlight>();
    }

#if UNITY_EDITOR
    void Reset() { if (!highlight) highlight = GetComponent<SlotHighlight>(); }
    void OnValidate() { if (!highlight) highlight = GetComponent<SlotHighlight>(); }
#endif

    public void OnPointerEnter(PointerEventData e)
    {
        if (highlight) highlight.SetHover(true);
        onPointerEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (highlight) highlight.SetHover(false);
        onPointerExit?.Invoke();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (highlight) highlight.SetPressed(true);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (highlight) highlight.SetPressed(false);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (highlight) highlight.ToggleSelected();
        onClick?.Invoke();
        if (debugLogs) Debug.Log("Clicked: " + gameObject.name);
    }
}
