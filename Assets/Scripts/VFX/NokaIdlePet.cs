using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Idle controller for a desktop-pet style sprite.
/// - Side view as idle. On click over the sprite: turn to front and show dialog (if assigned).
/// - When dialog hides: automatically return to side.
/// - If no dialog is assigned: optionally return to side immediately or after a short delay.
/// - Root object remains anchored; only the visual child bobs/breathes.
///
/// Hierarchy example:
///   NokaRoot (ViewportAnchor2D + NokaIdlePet)
///     └─ NokaSprite (SpriteRenderer + SpriteFacing2D [+ Collider2D])
///         └─ (auto) NokaGlow (SpriteRenderer) [optional]
[DisallowMultipleComponent]
public class NokaIdlePet : MonoBehaviour
{
    // -------------------- References --------------------
    [Header("References")]
    [Tooltip("Camera for hit testing. If null, Camera.main is used.")]
    public Camera cam;

    [Tooltip("Facing component on the visual child.")]
    public SpriteFacing2D facing;     // on NokaSprite

    [Tooltip("Visual child transform that receives bob/breathe (e.g., NokaSprite).")]
    public Transform spriteTransform; // NokaSprite

    // -------------------- Facing behavior --------------------
    [Header("Facing")]
    [Tooltip("Idle facing side.")]
    public SpriteFacing2D.Facing idleSide = SpriteFacing2D.Facing.Right;

    [Tooltip("When no dialog is assigned, return to side after click.")]
    public bool revertToIdleIfNoDialog = true;

    [Tooltip("Delay in seconds before returning to side when no dialog is assigned.")]
    [Min(0f)] public float revertDelayIfNoDialog = 0f;

    // -------------------- Dialog integration --------------------
    [Header("Guide Dialog (optional)")]
    [Tooltip("Guide dialog instance. If assigned, it is shown on click.")]
    public GuideDialog guideDialog;

    [Tooltip("Title override for the guide dialog.")]
    public string guideTitle = "Quick Guide";

    [Tooltip("Guide lines shown in the dialog.")]
    [TextArea(2, 6)]
    public string[] guideLines = {
        "Welcome! This is Noka.\nClick to switch between side and front.",
        "Idle motion parameters can be tuned later.",
        "Enjoy."
    };

    // -------------------- Idle motion --------------------
    [Header("Bob (floating)")]
    [Tooltip("Vertical bob amplitude in pixels.")]
    public float bobAmplitudePx = 3f;

    [Tooltip("Vertical bob frequency in Hz.")]
    public float bobFrequencyHz = 0.35f;

    [Header("Breathing (scale)")]
    [Tooltip("Uniform breathing scale amplitude, e.g., 0.02 = ±2%.")]
    [Range(0f, 0.2f)] public float breatheAmplitude = 0.018f;

    [Tooltip("Breathing frequency in Hz.")]
    public float breatheFrequencyHz = 0.22f;

    // -------------------- Optional glow --------------------
    [Header("Glow (auto if empty)")]
    [Tooltip("Glow child under the sprite. Auto-created if not assigned.")]
    public Transform glowTransform;

    [Tooltip("SpriteRenderer for the glow. Auto-added if not assigned.")]
    public SpriteRenderer glowRenderer;

    [Tooltip("Base scale multiplier for glow.")]
    public float glowBaseScale = 1.07f;

    [Tooltip("Glow breathing scale amplitude.")]
    [Range(0f, 0.3f)] public float glowBreatheAmplitude = 0.02f;

    [Tooltip("Glow breathing frequency in Hz.")]
    public float glowBreatheFrequencyHz = 0.10f;

    [Tooltip("Glow alpha pulse amplitude (0 disables alpha pulsing).")]
    [Range(0f, 0.5f)] public float glowAlphaAmplitude = 0.06f;

    // -------------------- Units --------------------
    [Header("Units")]
    [Tooltip("Pixels Per Unit of the artwork.")]
    public int pixelsPerUnit = 128;

    // -------------------- Runtime state --------------------
    private Vector3 _baseLocalPos, _baseLocalScale;
    private Vector3 _glowBaseLocalScale;
    private Color   _glowBaseColor;
    private bool    _hasGlow;
    private bool    _dialogWired;

    // -------------------- Lifecycle --------------------
    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!spriteTransform && facing != null) spriteTransform = facing.transform;

        if (spriteTransform != null)
        {
            _baseLocalPos   = spriteTransform.localPosition;
            _baseLocalScale = spriteTransform.localScale;
        }
    }

    private void OnEnable()
    {
        if (facing != null)
        {
            // Ensure facing does not move the child when swapping sprites.
            facing.lockLocalPosition = true;
            facing.rebaseEachApply   = false;
        }

        EnsureGlow();
        SetIdleSide();
        WireDialogCallbacks(true);
    }

    private void OnDisable()
    {
        WireDialogCallbacks(false);
    }

    // -------------------- Frame update --------------------
    private void Update()
    {
        if (!spriteTransform || !facing) return;

        // Click on sprite → front + dialog (if assigned)
        if (WasPrimaryPressedThisFrame() && IsPointerOverSprite())
        {
            SetFront(true);

            if (guideDialog != null)
            {
                guideDialog.Show(guideLines, guideTitle);
            }
            else if (revertToIdleIfNoDialog)
            {
                if (revertDelayIfNoDialog <= 0f) SetIdleSide();
                else StartCoroutine(CoRevertAfter(revertDelayIfNoDialog));
            }
        }

        // Idle motion: bob + breathe
        float t = Time.time;
        float worldAmp = bobAmplitudePx / Mathf.Max(1, pixelsPerUnit);
        float yBob = worldAmp * Mathf.Sin(2f * Mathf.PI * bobFrequencyHz * t);
        float s = 1f + breatheAmplitude * Mathf.Sin(2f * Mathf.PI * breatheFrequencyHz * t);

        spriteTransform.localPosition = _baseLocalPos + new Vector3(0f, yBob, 0f);
        spriteTransform.localScale    = _baseLocalScale * s;

        if (_hasGlow)
        {
            float gs = glowBaseScale * (1f + glowBreatheAmplitude * Mathf.Sin(2f * Mathf.PI * glowBreatheFrequencyHz * t));
            glowTransform.localScale = _glowBaseLocalScale * gs;

            if (glowRenderer && glowAlphaAmplitude > 0f)
            {
                float aPulse = glowAlphaAmplitude * Mathf.Sin(2f * Mathf.PI * glowBreatheFrequencyHz * t);
                var c = _glowBaseColor;
                c.a = Mathf.Clamp01(_glowBaseColor.a * (1f + aPulse));
                glowRenderer.color = c;
            }
        }
    }

    // -------------------- Facing helpers --------------------
    public void SetFront(bool front)
    {
        if (!facing) return;
        if (front) facing.SetFromAngle(270f); // Down = front
        else       SetIdleSide();
    }

    private void SetIdleSide()
    {
        if (!facing) return;
        if (idleSide == SpriteFacing2D.Facing.Left) facing.SetFromAngle(180f);
        else                                         facing.SetFromAngle(0f);
    }

    // -------------------- Dialog wiring --------------------
    private void WireDialogCallbacks(bool on)
    {
        if (!guideDialog) return;

        if (on && !_dialogWired)
        {
            guideDialog.OnHidden += HandleDialogHidden;
            _dialogWired = true;
        }
        else if (!on && _dialogWired)
        {
            guideDialog.OnHidden -= HandleDialogHidden;
            _dialogWired = false;
        }
    }

    private void HandleDialogHidden()
    {
        // Return to idle side once dialog is fully hidden.
        SetIdleSide();
    }

    private System.Collections.IEnumerator CoRevertAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        SetIdleSide();
    }

    // -------------------- Input & hit test --------------------
    private bool WasPrimaryPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool IsPointerOverSprite()
    {
        if (!cam || !spriteTransform) return false;

#if ENABLE_INPUT_SYSTEM
        Vector2 ms = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 ms = Input.mousePosition;
#endif
        float zDist = Mathf.Abs(cam.transform.position.z - spriteTransform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(ms.x, ms.y, zDist));

        var col = spriteTransform.GetComponent<Collider2D>();
        if (col) return col.OverlapPoint(world);

        var sr = spriteTransform.GetComponent<SpriteRenderer>();
        return (sr && sr.sprite != null) ? sr.bounds.Contains(world) : false;
    }

    // -------------------- Glow generation --------------------
    private void EnsureGlow()
    {
        if (glowTransform != null)
        {
            if (!glowRenderer) glowRenderer = glowTransform.GetComponent<SpriteRenderer>();
            CaptureGlowBaseline();
            return;
        }

        if (!spriteTransform) return;

        var go = new GameObject("NokaGlow");
        go.transform.SetParent(spriteTransform, false);
        glowTransform = go.transform;
        glowTransform.localPosition = Vector3.zero;
        glowTransform.localScale    = Vector3.one;

        glowRenderer = go.AddComponent<SpriteRenderer>();

        var sr = spriteTransform.GetComponent<SpriteRenderer>();
        if (sr)
        {
            glowRenderer.sortingLayerID = sr.sortingLayerID;
            glowRenderer.sortingOrder   = sr.sortingOrder - 1; // behind
        }

        var glowSprite = SoftGlowFactory.CreateRadialSprite(128, gamma: 2.2f, innerHardness: 0.15f);
        glowRenderer.sprite = glowSprite;
        glowRenderer.color  = new Color(1f, 1f, 1f, 0.22f);

        CaptureGlowBaseline();
    }

    private void CaptureGlowBaseline()
    {
        _hasGlow = glowTransform != null;
        if (_hasGlow)
        {
            _glowBaseLocalScale = glowTransform.localScale;
            _glowBaseColor      = glowRenderer ? glowRenderer.color : new Color(1,1,1,0.22f);
        }
    }
}
