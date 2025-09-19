using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Makes a SpriteFacing2D face the mouse cursor smoothly.
/// - Runs in LateUpdate to stay in sync with ViewportAnchor2D (which also moves in LateUpdate)
/// - Adds vertical deadband + weighting to avoid Up/Down flicker
/// - Exponential smoothing for buttery transitions
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteFacing2D))]
public class FaceCursor2D : MonoBehaviour
{
    [Tooltip("Camera used for screen->world. If empty, uses Camera.main.")]
    public Camera cam;

    [Tooltip("Optional origin for facing calculation; if null, uses this.transform.")]
    public Transform lookFrom;

    [Header("Filters")]
    [Tooltip("Ignore tiny movements (world units, after projection).")]
    public float deadZone = 0.01f;

    [Tooltip("Treat vertical component as smaller to prefer left/right.\n0.6 means vertical counts 60% as much as horizontal.")]
    [Range(0.1f, 1f)] public float verticalWeight = 0.6f;

    [Tooltip("If |normalized.y| < this, force y=0 so it won't bounce between Up/Down near horizontal.\n0.12 ≈ ±7 degrees.")]
    [Range(0f, 0.9f)] public float yDeadBand = 0.12f;

    [Tooltip("Exponential smoothing strength (0 = off). 16~20 is very smooth.")]
    [Range(0f, 40f)] public float faceSmoothing = 16f;

    [Header("Editor")]
    [Tooltip("Preview in edit mode (Scene view).")]
    public bool runInEditMode = false;

    private SpriteFacing2D _facing;
    private Vector2 _smoothDir;
    private bool _hasDir;

    void Awake()
    {
        _facing = GetComponent<SpriteFacing2D>();
        if (!cam) cam = Camera.main;
    }

#if UNITY_EDITOR
    void Update()
    {
        // In editor, allow preview without entering Play mode
        if (!Application.isPlaying && runInEditMode) Tick(Time.deltaTime);
    }
#endif

    void LateUpdate()
    {
        if (Application.isPlaying) Tick(Time.deltaTime);
    }

    private void Tick(float dt)
    {
        if (!_facing) return;

        var c = cam ? cam : Camera.main;
        if (!c) return;

        // 1) mouse screen pos
        Vector3 mouseScreen;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return;
        mouseScreen = m.position.ReadValue();
#else
        mouseScreen = Input.mousePosition;
#endif

        // 2) screen -> world at z=0 plane (or magnitude along camera z)
        float z = Mathf.Abs(c.transform.position.z);
        Vector3 world = c.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, z));

        // 3) direction from origin
        Vector3 origin = lookFrom ? lookFrom.position : transform.position;
        Vector2 dir = (Vector2)(world - origin);
        if (dir.sqrMagnitude < deadZone * deadZone) return;

        // 4) normalize, apply vertical deadband + weighting
        dir.Normalize();
        if (Mathf.Abs(dir.y) < yDeadBand) dir.y = 0f; // avoid Up/Down flicker near horizontal
        dir.y *= verticalWeight;

        // 5) smooth
        if (!_hasDir)
        {
            _smoothDir = dir;
            _hasDir = true;
        }
        else if (faceSmoothing > 0f)
        {
            // Exponential smoothing: 1 - exp(-k*dt) is framerate-independent
            float a = 1f - Mathf.Exp(-faceSmoothing * dt);
            _smoothDir = Vector2.Lerp(_smoothDir, dir, a);
        }
        else
        {
            _smoothDir = dir;
        }

        // 6) apply to facing
        _facing.SetDirection(_smoothDir);
    }
}
