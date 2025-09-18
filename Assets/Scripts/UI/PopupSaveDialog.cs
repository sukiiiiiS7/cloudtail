using UnityEngine;
using Cloudtail.IO;

/// Simple save/load/clear popup controller.
/// - Wire buttons with DialogActionButton (below).
/// - For Clear, optional two-tap confirmation inside the popup (no extra UI needed).
[DisallowMultipleComponent]
public class PopupSaveDialog : MonoBehaviour
{
    [Header("Refs")]
    public PlacementSave save;          // point to SaveManager's PlacementSave
    public GameObject panel;            // popup panel; if null, this GameObject

    [Header("Lifecycle")]
    public bool hideOnStart = true;     // hide at Start for safety

    [Header("Confirm for Clear")]
    public bool confirmClear = true;          // enable two-tap confirm on Clear
    public float confirmWindow = 1.2f;        // seconds
    public SpriteRenderer clearButtonVisual;   // optional: tint this SR when armed
    public Color armedColor = new Color(1f, 0.8f, 0.3f, 1f);

    private bool _armed;
    private float _armUntil;
    private Color _orig;

    void Awake()
    {
        if (!panel) panel = gameObject;
        if (hideOnStart) panel.SetActive(false);
        if (clearButtonVisual) _orig = clearButtonVisual.color;
    }

    public enum Action { Save, Load, Clear, Close }

    public void Show()
    {
        panel.SetActive(true);
        Disarm();
    }

    public void Hide()
    {
        panel.SetActive(false);
        Disarm();
    }

    public void Do(Action a)
    {
        if (a == Action.Close) { Hide(); return; }
        if (!save) return;

        switch (a)
        {
            case Action.Save:
                save.SaveNow();
                Hide();
                break;

            case Action.Load:
                save.LoadNow();
                Hide();
                break;

            case Action.Clear:
                if (confirmClear)
                {
                    if (!_armed || Time.time > _armUntil)
                    {
                        // first tap → arm
                        _armed = true;
                        _armUntil = Time.time + confirmWindow;
                        if (clearButtonVisual) clearButtonVisual.color = armedColor;
                        return;
                    }
                }
                save.ClearNow();
                Hide();
                break;
        }
    }

    void Update()
    {
        if (_armed && Time.time > _armUntil) Disarm();
    }

    private void Disarm()
    {
        _armed = false;
        if (clearButtonVisual) clearButtonVisual.color = _orig;
    }
}
