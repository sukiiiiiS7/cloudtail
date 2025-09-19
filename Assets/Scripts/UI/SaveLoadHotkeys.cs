#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using Cloudtail.IO;

/// <summary>
/// Development-only hotkeys for save/load/clear.
/// F5 = Save, F9 = Load, Shift+Delete = Clear.
/// </summary>
[DisallowMultipleComponent]
public class SaveLoadHotkeys : MonoBehaviour
{
    public PlacementSave save;

    void Update()
    {
        if (!save) return;
        if (Input.GetKeyDown(KeyCode.F5)) save.SaveNow();
        if (Input.GetKeyDown(KeyCode.F9)) save.LoadNow();
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Delete)) save.ClearNow();
    }
}
#endif
