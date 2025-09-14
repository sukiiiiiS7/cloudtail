using UnityEngine;
using UnityEngine.EventSystems;

/// Click-to-flip for InventoryPager.
/// Works with both EventSystem (IPointerClickHandler) and legacy OnMouseUpAsButton.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PageFlipOnClickPager : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("InventoryPager to control. If left empty, will try to find one at runtime.")]
    public InventoryPager pager;

    [Tooltip("true = next page, false = previous page.")]
    public bool forward = true;

    void Awake()
    {
        if (!pager) pager = FindObjectOfType<InventoryPager>();
        if (!pager) Debug.LogWarning("[PageFlipOnClickPager] InventoryPager not found at Awake.");
    }

    // Path 1: EventSystem route (requires EventSystem + Physics2DRaycaster on the camera)
    public void OnPointerClick(PointerEventData eventData)
    {
        Flip("[Pager] OnPointerClick");
    }

    // Path 2: Legacy physics route (works with just a Collider2D and any camera)
    void OnMouseUpAsButton()
    {
        Flip("[Pager] OnMouseUpAsButton");
    }

    private void Flip(string source)
    {
        if (!pager)
        {
            Debug.LogWarning("[PageFlipOnClickPager] No pager assigned.");
            return;
        }

        if (forward) pager.NextPage();
        else pager.PrevPage();

        Debug.Log($"{source}: {(forward ? "NextPage" : "PrevPage")}");
    }
}
