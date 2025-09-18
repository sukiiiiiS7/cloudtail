using UnityEngine;

/// Forces a RectTransform to full-screen stretch with zero offsets.
/// Works regardless of anchor changes made by other scripts at runtime.
[DisallowMultipleComponent]
public class ForceFullScreenRect : MonoBehaviour
{
    RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Apply()
    {
        if (!rt) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    void OnEnable() { Apply(); }

    void OnRectTransformDimensionsChange() { Apply(); }

    void LateUpdate()
    {
        // Re-apply if any property drifts away from full-stretch state.
        if (rt && (
            rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one ||
            rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero))
        {
            Apply();
        }
    }
}
