using UnityEngine;

/// Anchors a world-space UI container to a viewport point.
/// Keeps original Z. Only auto-finds camera at runtime (not in Prefab mode).
[DefaultExecutionOrder(10000)]
[ExecuteAlways]
[DisallowMultipleComponent]
public class ViewportAnchor2D : MonoBehaviour
{
    public Camera cam;
    public Vector2 anchor = new Vector2(0.5f, 0f);
    public Vector2 pixelOffset = new Vector2(0f, 64f);
    public float pixelsPerUnit = 128f;
    public bool snapToPixelGrid = false;

    float _z;

    void OnEnable()
    {
        _z = transform.position.z;
        // Do NOT auto-assign in Edit/Prefab mode
        if (Application.isPlaying && !cam)
            cam = Camera.main ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
    }

    void LateUpdate()
    {
        // In Edit mode: only run if a cam is explicitly assigned (no auto-find here)
        if (!cam)
        {
            if (Application.isPlaying)
                cam = Camera.main ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (!cam) return;
        }

        float dist = Mathf.Abs(cam.transform.position.z);
        var vp = cam.ViewportToWorldPoint(new Vector3(anchor.x, anchor.y, dist));
        var offs = new Vector3(pixelOffset.x / pixelsPerUnit, pixelOffset.y / pixelsPerUnit, 0f);

        var target = new Vector3(vp.x, vp.y, _z) + offs;

        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            target.x = Mathf.Round(target.x * pixelsPerUnit) / pixelsPerUnit;
            target.y = Mathf.Round(target.y * pixelsPerUnit) / pixelsPerUnit;
        }
        transform.position = target;
    }
}
