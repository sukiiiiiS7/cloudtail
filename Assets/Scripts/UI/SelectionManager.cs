using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager I;
    SlotHighlight _current;

    void Awake() { I = this; }

    public void Select(SlotHighlight h)
    {
        if (_current == h) return;
        if (_current) _current.SetSelected(false);
        _current = h;
        if (_current) _current.SetSelected(true);
    }

    public void Clear()
    {
        if (_current) _current.SetSelected(false);
        _current = null;
    }
}
