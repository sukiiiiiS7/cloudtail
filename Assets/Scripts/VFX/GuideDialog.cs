using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System
#endif

/// Dialog controller that hides via CanvasGroup alpha instead of SetActive(false).
/// Keeps the panel active to avoid deactivation side effects (layout/anchors/raycast).
[DisallowMultipleComponent]
public class GuideDialog : MonoBehaviour
{
    [Header("UI Roots")]
    [Tooltip("Root object that holds the dialog. Should remain active at runtime.")]
    public GameObject root;                 // e.g., DialogRoot; stays active
    [Tooltip("Optional root for the backdrop (dim layer). Can be toggled on/off.")]
    public GameObject backdropRoot;         // dim layer container (Image + Button)

    [Header("Components")]
    [Tooltip("CanvasGroup on the dialog panel for alpha/interactable toggling.")]
    public CanvasGroup canvasGroup;         // attach the CanvasGroup on Panel_Dialog
    [Tooltip("Optional: Backdrop click to close.")]
    public Button backdropButton;           // Backdrop Button (optional)
    [Tooltip("Optional: Top-right close button.")]
    public Button closeButton;              // X Button (optional)

    [Header("Animation")]
    [Tooltip("Fade duration in seconds.")]
    public float fadeDuration = 0.15f;
    [Tooltip("Initial visibility on Start().")]
    public bool startVisible = false;

    [Header("Events (compatibility)")]
    [Tooltip("Invoked when the dialog has fully appeared (alpha≈1).")]
    public UnityEvent onShownUnity;
    [Tooltip("Invoked when the dialog has fully hidden (alpha≈0).")]
    public UnityEvent onHiddenUnity;

    /// <summary>C# event for code that subscribes via '+='.</summary>
    public event System.Action OnShown;
    /// <summary>C# event for code that subscribes via '+='.</summary>
    public event System.Action OnHidden;

    Coroutine co;

    void Awake()
    {
        // Ensure root stays active; visibility is controlled by CanvasGroup only.
        if (root && !root.activeSelf) root.SetActive(true);

        // Ensure CanvasGroup exists.
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(includeInactive: true);
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Wire buttons with zero-arg UnityAction via lambdas.
        if (backdropButton) backdropButton.onClick.AddListener(() => Close());
        if (closeButton)    closeButton.onClick.AddListener(() => Close());

        // Apply initial visibility.
        ApplyVisible(startVisible, instant: true);
        if (backdropRoot) backdropRoot.SetActive(startVisible);
    }

    void Update()
    {
        // ESC closes when visible. Supports both input backends.
        if (canvasGroup && canvasGroup.alpha > 0.99f)
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
#else
            // Old Input Manager
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
#endif
        }
    }

    // ------------------------ Public API ------------------------

    /// <summary>Opens the dialog. If instant=true, skips fading.</summary>
    public void Open(bool instant = false) => SetVisible(true, instant);

    /// <summary>Closes the dialog. If instant=true, skips fading.</summary>
    public void Close(bool instant = false) => SetVisible(false, instant);

    /// <summary>Sets target visibility without deactivating GameObjects.</summary>
    public void SetVisible(bool on, bool instant = false)
    {
        if (co != null) StopCoroutine(co);

        if (instant)
        {
            ApplyVisible(on, true);
            FireEvents(on);
        }
        else
        {
            co = StartCoroutine(on ? CoFade(1f) : CoFade(0f));
        }

        // Backdrop can toggle as a separate layer.
        if (backdropRoot) backdropRoot.SetActive(on);
    }

    // --- Compatibility shim (legacy API) -------------------------------------
    // Old style Show/Hide with no args
    public void Show() => SetVisible(true, false);
    public void Hide() => SetVisible(false, false);

    // With one bool arg = instant
    public void Show(bool instant) => SetVisible(true, instant);
    public void Hide(bool instant) => SetVisible(false, instant);

    // With two args (on, instant)
    public void Show(bool on, bool instant) => SetVisible(on, instant);

    // Wide-compat: accept any legacy signatures like Show(string[] lines, string title, ...)
    // Parameters are ignored here; actual content binding should be handled elsewhere.
    public void Show(params object[] _) => SetVisible(true, false);
    public void Hide(params object[] _) => SetVisible(false, false);

    /// <summary>Indicates whether the dialog is effectively visible (alpha≈1).</summary>
    public bool IsVisible => canvasGroup && canvasGroup.alpha > 0.99f;

    // ------------------------ Internal -------------------------

    IEnumerator CoFade(float target)
    {
        float start = canvasGroup ? canvasGroup.alpha : 0f;
        float t = 0f;

        // While fading in, enable interaction; while fading out, keep blocking to avoid leaks.
        if (canvasGroup && target > start)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable   = true;
        }

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        if (canvasGroup)
        {
            canvasGroup.alpha = target;
            bool visible = target > 0.99f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable   = visible;
        }

        FireEvents(target > 0.99f);
        co = null;
    }

    void ApplyVisible(bool on, bool instant)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = on ? 1f : 0f;
        canvasGroup.blocksRaycasts = on;
        canvasGroup.interactable   = on;

        // Root remains active to avoid SetActive(false) side effects.
        if (root && !root.activeSelf) root.SetActive(true);
    }

    void FireEvents(bool nowVisible)
    {
        if (nowVisible)
        {
            onShownUnity?.Invoke();
            OnShown?.Invoke();
        }
        else
        {
            onHiddenUnity?.Invoke();
            OnHidden?.Invoke();
        }
    }
}
