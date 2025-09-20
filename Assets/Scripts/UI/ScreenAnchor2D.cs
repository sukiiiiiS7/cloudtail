using UnityEngine;

/// Anchors a world-space object to a screen corner with pixel offset.
/// Intended for Pixel-Perfect setups; offset is expressed in pixels.
[DisallowMultipleComponent]
public sealed class ScreenAnchor2D : MonoBehaviour
{
    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [SerializeField] private Corner corner = Corner.TopRight;
    [SerializeField] private Vector2 pixelOffset = new Vector2(8f, 8f);
    [SerializeField] private float pixelsPerUnit = 128f;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        float sx = (corner == Corner.TopRight || corner == Corner.BottomRight) ? cam.pixelWidth  : 0f;
        float sy = (corner == Corner.TopLeft  || corner == Corner.TopRight)    ? cam.pixelHeight : 0f;
        Vector3 screen = new Vector3(sx, sy, 0f);

        Vector3 world = cam.ScreenToWorldPoint(screen);
        float unit = 1f / Mathf.Max(1f, pixelsPerUnit);

        float ox = ((corner == Corner.TopRight || corner == Corner.BottomRight) ? -pixelOffset.x : pixelOffset.x) * unit;
        float oy = ((corner == Corner.TopLeft  || corner == Corner.TopRight)    ? -pixelOffset.y : pixelOffset.y) * unit;

        transform.position = new Vector3(world.x + ox, world.y + oy, transform.position.z);
    }
}
