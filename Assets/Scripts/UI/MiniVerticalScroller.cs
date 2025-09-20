using UnityEngine;

/// Vertical scroller for a top-anchored content inside a masked viewport.
[DisallowMultipleComponent]
public sealed class MiniVerticalScroller : MonoBehaviour
{
    [SerializeField] RectTransform viewport;   // Masked area
    [SerializeField] RectTransform content;    // Top-anchored content

    float MaxScroll => Mathf.Max(0f, content.rect.height - viewport.rect.height);

    void Reset()
    {
        if (!viewport) viewport = transform.GetComponentInChildren<RectTransform>();
    }

    public void RecalcAndClamp()
    {
        if (!viewport || !content) return;
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, 0f, MaxScroll);
        content.anchoredPosition = pos;
    }

    public void ScrollBy(float deltaPixels)
    {
        if (!viewport || !content) return;
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y + deltaPixels, 0f, MaxScroll);
        content.anchoredPosition = pos;
    }

    public void SnapTop()    => SetY(0f);
    public void SnapBottom() => SetY(MaxScroll);

    public void SetY(float y)
    {
        if (!content) return;
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(y, 0f, MaxScroll);
        content.anchoredPosition = pos;
    }

#if UNITY_EDITOR
    // Quick debug in inspector if needed
    [ContextMenu("Snap Top")]    void _Top() => SnapTop();
    [ContextMenu("Snap Bottom")] void _Bottom() => SnapBottom();
#endif
}
