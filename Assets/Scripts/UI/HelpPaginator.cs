using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// Pagination that packs text by measuring real pixel height using a hidden TMP label.
/// Avoids dependence on the visible label state, margins, or frame timing.
[DisallowMultipleComponent]
public sealed class HelpPaginator : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TextMeshProUGUI text;        // Visible label
    [SerializeField] private RectTransform   viewport;    // Viewport (has RectMask2D)
    [SerializeField] private TextMeshProUGUI pageLabel;   // Optional "1 / N"

    [Header("Raw Source (pick ONE)")]
    [SerializeField] private TextAsset source;            // External text asset
    [TextArea(6,40)] [SerializeField] private string rawOverride; // Or paste here

    [Header("Layout")]
    [SerializeField] private float paddingX = 12f;
    [SerializeField] private float paddingY = 12f;
    [SerializeField] private float fill = 0.96f;          // 0.90–0.98; lower = more conservative

    [Header("Emergency")]
    [SerializeField] private int hardMaxCharsPerChunk = 600; // For extremely long single lines

    private readonly List<string> pages = new List<string>();
    private int index;
    private Coroutine co;
    private TextMeshProUGUI measure; // hidden label used only for GetPreferredValues()

    private void Awake()
    {
        // Create an offscreen TMP for measurement to avoid interference from the visible one.
        var go = new GameObject("TMP_Measure(Hidden)");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        measure = go.AddComponent<TextMeshProUGUI>();

        // Copy essential visual settings from the visible label
        if (text != null)
        {
            measure.font = text.font;
            measure.fontSize = text.fontSize;
            measure.fontStyle = text.fontStyle;
            measure.enableAutoSizing = false;
            measure.richText = text.richText;
            measure.color = text.color;
        }

        // Measurement label baseline
        measure.enableWordWrapping = true;
        measure.alignment = TextAlignmentOptions.TopLeft;
        measure.overflowMode = TextOverflowModes.Overflow;
        measure.margin = Vector4.zero;        // margins are simulated via paddingX/Y, not on the measure label
        measure.raycastTarget = false;
        measure.gameObject.SetActive(false);  // keep invisible
    }

    private void OnEnable()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RebuildNextFrame());
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RebuildNextFrame());
    }

    private IEnumerator RebuildNextFrame()
    {
        yield return null; // ensure viewport rect is valid

        if (!text || !viewport || !measure) yield break;

        // Normalize the visible label too (display only)
        text.enableWordWrapping   = true;
        text.alignment            = TextAlignmentOptions.TopLeft;
        text.overflowMode         = TextOverflowModes.Overflow;
        text.margin               = new Vector4(paddingX, paddingY, paddingX, paddingY);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

        BuildPagesMeasured(GetRaw());
        Show(0);
        co = null;
    }

    private string GetRaw()
    {
        if (source != null) return source.text;
        if (!string.IsNullOrEmpty(rawOverride)) return rawOverride;
        return string.Empty;
    }

    private void BuildPagesMeasured(string raw)
    {
        pages.Clear();
        if (string.IsNullOrEmpty(raw)) { pages.Add(string.Empty); return; }

        float boxW = Mathf.Max(1f, viewport.rect.width  - paddingX * 2f);
        float boxH = Mathf.Max(1f, viewport.rect.height - paddingY * 2f);
        float maxH = boxH * Mathf.Clamp01(fill);

        // Keep the measurement label in sync with font sizing
        measure.font = text.font;
        measure.fontSize = text.fontSize;
        measure.fontStyle = text.fontStyle;
        measure.enableAutoSizing = false;
        measure.richText = text.richText;

        string[] lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string acc = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            string trial = string.IsNullOrEmpty(acc) ? lines[i] : acc + "\n" + lines[i];
            Vector2 pref = measure.GetPreferredValues(trial, boxW, Mathf.Infinity);

            if (pref.y <= maxH)
            {
                acc = trial; // still fits
                continue;
            }

            if (!string.IsNullOrEmpty(acc))
            {
                pages.Add(acc);
                acc = string.Empty;
            }

            // If a single line overflows a fresh page, split it by binary search chunks.
            foreach (var chunk in SplitLineByMeasure(measure, lines[i], boxW, maxH, hardMaxCharsPerChunk))
                pages.Add(chunk);
        }

        if (!string.IsNullOrEmpty(acc)) pages.Add(acc);
        if (pages.Count == 0) pages.Add(string.Empty);
    }

    private static IEnumerable<string> SplitLineByMeasure(TextMeshProUGUI label, string line, float boxW, float maxH, int hardCap)
    {
        int start = 0, n = line.Length;
        while (start < n)
        {
            int lo = 1, hi = Mathf.Min(hardCap, n - start), best = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                string sub = line.Substring(start, mid);
                Vector2 pref = label.GetPreferredValues(sub, boxW, Mathf.Infinity);
                if (pref.y <= maxH) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            yield return line.Substring(start, best);
            start += best;
        }
    }

    public void Next() { if (index < pages.Count - 1) Show(index + 1); }
    public void Prev() { if (index > 0) Show(index - 1); }

    public void Show(int i)
    {
        if (pages.Count == 0 || !text) return;
        index = Mathf.Clamp(i, 0, pages.Count - 1);
        text.text = pages[index];
        if (pageLabel) pageLabel.text = $"{index + 1} / {pages.Count}";
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private void Update()
    {
        if (pages.Count <= 1) return;
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.pageUpKey.wasPressedThisFrame) Prev();
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.pageDownKey.wasPressedThisFrame) Next();
        if (kb.homeKey.wasPressedThisFrame) Show(0);
        if (kb.endKey.wasPressedThisFrame)  Show(pages.Count - 1);
    }
#else
    private void Update()
    {
        if (pages.Count <= 1) return;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.PageUp)) Prev();
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.PageDown)) Next();
        if (Input.GetKeyDown(KeyCode.Home)) Show(0);
        if (Input.GetKeyDown(KeyCode.End))  Show(pages.Count - 1);
    }
#endif
}
