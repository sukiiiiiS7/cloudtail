using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Click-to-flip behavior for paging arrows.
/// Attach to forward/back buttons. Optionally assign a PageController,
/// otherwise it will auto-find one in the scene at runtime.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PageFlipOnClick : MonoBehaviour, IPointerClickHandler
{
    public enum Direction { Next, Prev }

    [Tooltip("Next = increment page, Prev = decrement page.")]
    public Direction direction = Direction.Next;

    [Tooltip("Optional explicit reference. If left empty, will FindObjectOfType at runtime.")]
    public PageController controller;

    void Awake()
    {
        if (!controller) controller = FindObjectOfType<PageController>();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!controller)
        {
            Debug.LogWarning("PageFlipOnClick: PageController not found.");
            return;
        }

        if (direction == Direction.Next) controller.NextPage();
        else controller.PrevPage();
    }
}
