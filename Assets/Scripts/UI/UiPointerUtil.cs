using UnityEngine;
using UnityEngine.EventSystems;

/// Utility for detecting whether the current pointer is over UI.
public static class UiPointerUtil
{
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Mouse pointer
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // Touch pointers (multi-touch safe)
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }
}
