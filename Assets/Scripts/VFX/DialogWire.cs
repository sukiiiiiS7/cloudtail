using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures dialog buttons are properly connected to the GuideDialog.
/// Binds backdrop and close buttons directly to the Close() method at runtime,
/// preventing issues with missing or broken UnityEvent assignments in the Inspector.
/// </summary>
[RequireComponent(typeof(GuideDialog))]
public class DialogWire : MonoBehaviour
{
    [Header("Button References")]
    [Tooltip("Backdrop button used to close the dialog when clicking outside the panel.")]
    public Button backdropButton;

    [Tooltip("Close button (X) in the top-right corner of the dialog.")]
    public Button closeButton;

    private GuideDialog dlg;

    private void Awake()
    {
        dlg = GetComponent<GuideDialog>();

        // Reset and bind backdrop button
        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveAllListeners();
            backdropButton.onClick.AddListener(() => dlg.Close());
        }

        // Reset and bind close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => dlg.Close());
        }
    }
}
