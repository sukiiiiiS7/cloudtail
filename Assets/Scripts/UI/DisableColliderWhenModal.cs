using UnityEngine;

/// Disables all Collider2D on the GameObject while a modal UI gate is active.
[DisallowMultipleComponent]
public sealed class DisableColliderWhenModal : MonoBehaviour
{
    private Collider2D[] cols;

    private void Awake()
    {
        cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    private void LateUpdate()
    {
        bool enabled = !UiModalGate.IsBlocked;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null) cols[i].enabled = enabled;
        }
    }
}
