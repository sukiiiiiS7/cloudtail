using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
public sealed class HelpReflow : MonoBehaviour
{
    [SerializeField] RectTransform viewport;
    [SerializeField] RectTransform content;
    [SerializeField] TextMeshProUGUI label;

    void Reset()
    {
        if (!viewport) viewport = transform.Find("Viewport") as RectTransform;
        if (viewport && !content && viewport.childCount > 0)
            content = viewport.GetChild(0) as RectTransform;
        if (!label && content) label = content.GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // clear focus so keys are not captured by last selected UI
        EventSystem.current?.SetSelectedGameObject(null);
        RebuildNow();
        // force to top once heights are valid
        var p = content.anchoredPosition; p.y = 0; content.anchoredPosition = p;
    }

    void OnRectTransformDimensionsChange() => RebuildNow();

    void RebuildNow()
    {
        if (!viewport || !content || !label) return;
        // ensure the content is driven by fitter
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        }

        // normalize TMP flags
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;

        // force a full layout pass so content.rect.height becomes valid
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
    }
}

