using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// Closes a target panel when the transparent overlay is clicked.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class OutsideClickCloser : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject targetPanel;

    private void Reset()
    {
        // Fullscreen rect and transparent image configured for raycasts.
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetPanel != null) targetPanel.SetActive(false);
        gameObject.SetActive(false);
    }
}
