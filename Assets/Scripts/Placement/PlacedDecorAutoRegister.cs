using UnityEngine;

/// Auto-registers a spawned decor into PlacementHistory on first enable.
/// No dependency on the spawning code.
[DisallowMultipleComponent]
public sealed class PlacedDecorAutoRegister : MonoBehaviour
{
    private bool registered;

    private void OnEnable()
    {
        if (!Application.isPlaying || registered) return;
        PlacementHistory.Push(gameObject);
        registered = true;
    }
}
