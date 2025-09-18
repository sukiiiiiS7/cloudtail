using UnityEngine;

/// Chooses a sprite for a 2D character based on facing direction (4-way + optional 45°).
/// Supports per-facing pixel offsets to visually align "feet" across sprites—without editing pivots.
///
/// Attach to the child (e.g., "NokaSprite") that has the SpriteRenderer.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFacing2D : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite spriteDown;          // front
    public Sprite spriteUp;            // back
    public Sprite spriteRight;         // right (left is mirrored from this)
    public Sprite spriteDownRight45;   // optional 45° (left mirrored)

    [Header("Options")]
    [Tooltip("Use the 45° sprite around 45°/315° if assigned.")]
    public bool useDiagonal45 = true;

    [Range(5f, 60f)]
    [Tooltip("Angular window (degrees) for using the 45° sprite.")]
    public float diagonalWindow = 26f;

    [Range(0f, 1f)]
    [Tooltip("Hysteresis to reduce Left/Right flip jitter near 0°/180°. Higher = more stable.")]
    public float flipHysteresis = 0.22f;

    [Header("Offsets (pixels)")]
    [Tooltip("Offsets applied AFTER choosing sprite. Use to keep feet aligned visually.")]
    public Vector2 offsetDownPx = Vector2.zero;
    public Vector2 offsetUpPx = Vector2.zero;
    public Vector2 offsetRightPx = Vector2.zero;
    public Vector2 offsetDownRight45Px = Vector2.zero;

    [Header("Units")]
    [Tooltip("Pixels Per Unit of your art (e.g., 128).")]
    public int pixelsPerUnit = 128;

    [Tooltip("When enabled, treat current localPosition as new baseline each Apply.")]
    public bool rebaseEachApply = false;

    [Header("Movement Lock")]
    [Tooltip("When true, NEVER move localPosition; only swap sprite/flip.")]
    public bool lockLocalPosition = true;

    [Header("Startup")]
    [Tooltip("If ON, Awake() sets sprite to 'Down' once. Leave OFF to avoid start-facing flash.")]
    public bool setSpriteInAwake = false; 

    // --- Runtime ---
    public enum Facing { Down, Up, Right, Left, DownRight45, DownLeft45 }
    public Facing Current { get; private set; } = Facing.Down;

    private SpriteRenderer _sr;
    private Vector3 _baseLocal;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseLocal = transform.localPosition;
        
        if (_sr)
        {
            
            _sr.flipX = false;
            _sr.flipY = false; // hard lock: never vertical-flip
        }
    }


    void OnEnable()
    {
        _baseLocal = transform.localPosition;
    }

    /// External API: set facing using a direction vector.
    public void SetDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 1e-9f) return;
        float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (a < 0) a += 360f;
        SetFromAngle(a);
    }

    /// External API: set facing using an absolute angle (0=Right, 90=Up, 180=Left, 270=Down).
    public void SetFromAngle(float deg)
    {
        // Optional diagonals
        bool canDiag = useDiagonal45 && spriteDownRight45;
        if (canDiag && AngleDist(deg, 315f) <= diagonalWindow) { Apply(Facing.DownRight45, false); return; }
        if (canDiag && AngleDist(deg, 225f) <= diagonalWindow) { Apply(Facing.DownLeft45,  true); return; }

        // Cardinals
        float dR = AngleDist(deg,   0f);
        float dU = AngleDist(deg,  90f);
        float dL = AngleDist(deg, 180f);
        float dD = AngleDist(deg, 270f);

        Facing pick =
            (dR <= dU && dR <= dL && dR <= dD) ? Facing.Right :
            (dU <= dL && dU <= dD)            ? Facing.Up    :
            (dL <= dD)                        ? Facing.Left  :
                                                Facing.Down;

        // L/R hysteresis near 0° vs 180°
        if ((Current == Facing.Left || Current == Facing.Right) &&
            (pick    == Facing.Left || pick    == Facing.Right))
        {
            float margin = Mathf.Lerp(0f, 25f, flipHysteresis); // up to ~25°
            if (AngleDist(deg,   0f) < margin) pick = Facing.Right;
            if (AngleDist(deg, 180f) < margin) pick = Facing.Left;
        }

        Apply(pick, pick == Facing.Left || pick == Facing.DownLeft45);
    }

    // Core: choose sprite/flip, then (optionally) apply per-facing pixel offsets
    private void Apply(Facing f, bool flipX)
    {
        Current = f;
        if (!_sr) _sr = GetComponent<SpriteRenderer>();

        Sprite s = spriteDown;
        Vector2 px = Vector2.zero;

        switch (f)
        {
            case Facing.Down:
                if (spriteDown) s = spriteDown;
                px = offsetDownPx;
                flipX = false;
                break;

            case Facing.Up:
                if (spriteUp) s = spriteUp;
                px = offsetUpPx;
                flipX = false;
                break;

            case Facing.Right:
                if (spriteRight) s = spriteRight;
                px = offsetRightPx;
                flipX = false;
                break;

            case Facing.Left:
                if (spriteRight) s = spriteRight; // mirror from Right
                px = offsetRightPx;
                flipX = true;
                break;

            case Facing.DownRight45:
                if (spriteDownRight45) s = spriteDownRight45;
                px = offsetDownRight45Px;
                flipX = false;
                break;

            case Facing.DownLeft45:
                if (spriteDownRight45) s = spriteDownRight45; // mirror from DR45
                px = offsetDownRight45Px;
                flipX = true;
                break;
        }

        // Apply sprite + flip
        if (_sr)
        {
            _sr.sprite = s;
            _sr.flipX  = flipX;
            _sr.flipY  = false; // hard lock: prevent any script/anim from flipping Y
        }

        // --- Movement handling ---
        if (lockLocalPosition)
        {
            // Hard lock: never move the child. Keep baseline localPosition.
            transform.localPosition = _baseLocal;
            return;
        }

        // Optional: rebase so offsets are added on the current local zero
        if (rebaseEachApply) _baseLocal = transform.localPosition;

        // Mirror-aware X offset (so left/right feet align when mirroring)
        if (flipX) px.x = -px.x;

        // Convert pixel offsets to world units and apply to child only
        float ppu = Mathf.Max(1, pixelsPerUnit);
        Vector3 worldOff = new Vector3(px.x / ppu, px.y / ppu, 0f);
        transform.localPosition = _baseLocal + worldOff;
    }

    private static float AngleDist(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b));
    }
}

