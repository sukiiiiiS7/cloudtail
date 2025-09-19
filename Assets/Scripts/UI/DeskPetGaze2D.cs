using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Desk-pet style facing (no movement).
/// Safe version: waits for the first real mouse move; ignores input when mouse is outside the Game view;
/// dual-threshold (Schmitt trigger) to avoid flapping.
[DisallowMultipleComponent]
public class DeskPetGaze2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform lookFrom;
    public SpriteFacing2D facing;

    [Header("Zones (viewport units)")]
    [Range(0.0f, 0.45f)] public float sideThreshold = 0.10f; // enter L/R
    [Range(0.5f, 2.0f)]  public float sideDominance = 1.2f;
    [Range(0.0f, 0.2f)]  public float centerHysteresis = 0.03f; // exit to center

    [Header("Timing (anti-jitter)")]
    [Range(0.0f, 1.0f)]  public float glanceHold = 0.45f;
    [Range(0.0f, 1.0f)]  public float minSwitchInterval = 0.35f;

    [Header("Behavior")]
    public bool allowBackView = false;

    [Header("Smoothing")]
    [Range(0f, 30f)]     public float dirSmoothing = 14f;

    [Header("Fine control")]
    [Tooltip("Vertical dominance before logic. 1 = neutral; >1 exaggerates Up/Down.")]
    public float verticalWeight = 0.6f;
    [Tooltip("Ignore tiny cursor motion near origin (world units).")]
    public float deadZone = 0.012f;

    [Header("Safety")]
    [Tooltip("Require the mouse to move at least this many viewport units before we start (prevents auto-switch at startup).")]
    public float wakeThresholdViewport = 0.01f; // ~1% of screen
    [Tooltip("Keep facing Down until the first valid mouse move is observed.")]
    public bool waitForFirstMove = true;

    // internals
    private float _smoothedUx = 0f;
    private int _current = 0, _pending = 0;
    private float _pendingSince = -1f, _lastSwitch = -10f;
    private bool _awakeToMouse = false;
    private Vector2 _lastViewport = new Vector2(0.5f, 0.5f);

    // angles for SpriteFacing2D
    private const float ANG_RIGHT = 0f, ANG_UP = 90f, ANG_LEFT = 180f, ANG_DOWN = 270f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!lookFrom) lookFrom = transform;
    }

    void Update()
    {
        if (!facing || !cam) return;

        // read mouse (both input systems)
        Vector3 mouseScreen;
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse == null) return;
        mouseScreen = mouse.position.ReadValue();
#else
        mouseScreen = Input.mousePosition;
#endif

        // viewport pos
        Vector3 uv3 = cam.ScreenToViewportPoint(mouseScreen); // (0..1, 0..1)
        Vector2 uv = new Vector2(uv3.x, uv3.y);

        // ignore when mouse is outside game view
        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
        {
            // do nothing; keep last facing
            return;
        }

        // world dir for deadZone & vertical weighting
        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, z));
        Vector2 dir = (Vector2)(world - lookFrom.position);

        // wait for first meaningful move (prevents auto-switch at startup)
        if (waitForFirstMove && !_awakeToMouse)
        {
            if (Vector2.Distance(uv, _lastViewport) >= wakeThresholdViewport)
                _awakeToMouse = true;
            else
                return; // keep Down, no switching
        }
        _lastViewport = uv;

        // tiny jitter guard (world)
        if (dir.sqrMagnitude < deadZone * deadZone) return;

        // vertical dominance
        dir.y *= Mathf.Max(0f, verticalWeight);

        // smoothed horizontal value [-0.5..+0.5]
        float ux = uv.x - 0.5f;
        float k = Mathf.Clamp01(1f - Mathf.Exp(-dirSmoothing * Time.deltaTime));
        _smoothedUx = Mathf.Lerp(_smoothedUx, ux, k);

        // Schmitt trigger thresholds
        float thrEnter = sideThreshold / Mathf.Max(0.0001f, sideDominance);
        float thrExit  = Mathf.Max(0f, thrEnter - centerHysteresis);

        int want = _current;
        if (_current == +1)      want = (_smoothedUx <=  thrExit) ? 0 : +1;
        else if (_current == -1) want = (_smoothedUx >= -thrExit) ? 0 : -1;
        else
        {
            if      (_smoothedUx >  thrEnter) want = +1;
            else if (_smoothedUx < -thrEnter) want = -1;
            else                              want =  0;
        }

        if (allowBackView && want == 0)
        {
            bool nearCenter = Mathf.Abs(_smoothedUx) < thrExit * 0.6f;
            bool highEnough = uv.y > 0.85f;
            if (nearCenter && highEnough) want = 2;
        }

        // debounce + min interval
        if (want != _current)
        {
            if (want != _pending)
            {
                _pending = want;
                _pendingSince = Time.time;
            }
            else
            {
                bool stableEnough = (Time.time - _pendingSince) >= glanceHold;
                bool spacedEnough = (Time.time - _lastSwitch)   >= minSwitchInterval;
                if (stableEnough && spacedEnough)
                {
                    _current = _pending;
                    _lastSwitch = Time.time;
                    ApplyFacing(_current);
                }
            }
        }
        else if (_lastSwitch < -1f) // first-time apply after scene load
        {
            _lastSwitch = Time.time;
            ApplyFacing(_current);
        }
    }

    private void ApplyFacing(int mode)
    {
        switch (mode)
        {
            case -1: facing.SetFromAngle(ANG_LEFT);  break; // Left
            case +1: facing.SetFromAngle(ANG_RIGHT); break; // Right
            case  2: facing.SetFromAngle(ANG_UP);    break; // Up (back)
            default: facing.SetFromAngle(ANG_DOWN);  break; // Down (front)
        }
    }
}
