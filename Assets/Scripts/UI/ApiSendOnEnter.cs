using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// Triggers CloudtailApiClient.Send() when Enter/NumpadEnter is pressed.
/// Works with Unity Input System (no legacy Input).
[DisallowMultipleComponent]
public class ApiSendOnEnter : MonoBehaviour
{
    [Header("Refs")]
    public CloudtailApiClient api;      // drag the object with CloudtailApiClient
    public TMP_InputField input;        // optional: assign the input field

    [Header("Options")]
    public bool useNumpadEnter = true;      // also accept numpad Enter
    public bool onlyWhenFocused = true;     // submit only if the input is focused

    void Reset()
    {
        if (api == null) api = FindObjectOfType<CloudtailApiClient>();
        if (input == null) input = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        bool pressed = Keyboard.current.enterKey.wasPressedThisFrame
                       || (useNumpadEnter && Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        if (!pressed) return;

        if (onlyWhenFocused && input != null && !input.isFocused) return;

        api?.Send();
    }
}
