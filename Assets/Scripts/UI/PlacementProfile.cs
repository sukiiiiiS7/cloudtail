using UnityEngine;

/// Optional per-prefab placement overrides.
/// Attach to a prefab (e.g. cloud) if you need custom alignment or offset.
public class PlacementProfile : MonoBehaviour
{
    [Header("Alignment override")]
    [Tooltip("Force center alignment for this prefab, ignoring ground snap.")]
    public bool forceCenter = false;

    [Tooltip("Force bottom alignment on ground surfaces for this prefab.")]
    public bool forceBottomOnGround = false;

    [Header("Extra offset (pixels)")]
    [Tooltip("Extra pixel offset to apply after alignment (X,Y in pixels).")]
    public Vector2 extraNudgePx = Vector2.zero;
}
