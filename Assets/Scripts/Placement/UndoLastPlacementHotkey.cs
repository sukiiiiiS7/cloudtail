using UnityEngine;
using UnityEngine.InputSystem;

/// Global hotkey listener: Delete key removes the most recent placed decor.
[DisallowMultipleComponent]
public sealed class UndoLastPlacementHotkey : MonoBehaviour
{
    [SerializeField] private bool blockWhenModal = true; // Requires UiModalGate if present

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.deleteKey.wasPressedThisFrame) return;

        if (blockWhenModal && typeof(UiModalGate) != null)
        {
            if (UiModalGate.IsBlocked) return;
        }

        PlacementHistory.RemoveLast();
    }
}
