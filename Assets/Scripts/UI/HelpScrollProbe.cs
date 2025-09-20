using UnityEngine;
using TMPro;

public sealed class HelpScrollProbe : MonoBehaviour
{
    [SerializeField] RectTransform viewport;
    [SerializeField] RectTransform content;
    [SerializeField] TextMeshProUGUI readout; // optional small TMP label in corner

    float t;
    void Update()
    {
        if (!viewport || !content) return;
        t += Time.unscaledDeltaTime;
        if (t < 0.25f) return; // update 4Hz
        t = 0f;

        float viewH = viewport.rect.height;
        float contH = content.rect.height;
        float max   = Mathf.Max(0f, contH - viewH);
        float y     = content.anchoredPosition.y;

        Debug.Log($"[HelpProbe] viewH={viewH:F1}  contentH={contH:F1}  max={max:F1}  y={y:F1}");
        if (readout) readout.text = $"view {viewH:F0}\ncont {contH:F0}\nmax {max:F0}\ny {y:F0}";
    }
}
