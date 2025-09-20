using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

/// Handles TMP input submission.
/// - Enter / NumpadEnter always submits.
/// - Space can optionally submit if enabled.
/// - Exposes SendNow() for external calls (e.g., button click).
[DisallowMultipleComponent]
public class InputSubmitter : MonoBehaviour
{
    [Header("Wiring")]
    public TMP_InputField inputField;      // Field to read user input
    public RecommendClient client;         // Target client for sending text

    [Header("Options")]
    public bool submitOnSpace = false;     // Allow spacebar to trigger submit

    void Update()
    {
        if (inputField == null || client == null) return;

        // Only react when this field is focused to avoid stealing keys
        if (!inputField.isFocused) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool enter = (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
        bool space = submitOnSpace && kb.spaceKey.wasPressedThisFrame;

        if (enter || space)
        {
            SendNow();
        }
    }

    /// Immediately sends current input text and clears field.
    public void SendNow()
    {
        if (inputField == null || client == null) return;

        string content = inputField.text;
        if (string.IsNullOrWhiteSpace(content)) return;

        client.Send(content);
        inputField.text = "";              
        inputField.ActivateInputField();   // Keep focus for next typing
    }
}
