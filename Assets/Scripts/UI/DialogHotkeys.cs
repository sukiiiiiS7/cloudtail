using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class DialogHotkeys : MonoBehaviour
{
    public GameObject body;            // Body
    public GameObject panelInput;      // Panel_Input
    public TMPro.TMP_InputField autoFocusField; // Panel_Input/InputField_Content

    void Update()
    {
        var kb = Keyboard.current; if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
        {
            if (panelInput) panelInput.SetActive(true);   
            if (autoFocusField != null)
                StartCoroutine(FocusNextFrame());
        }


        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (body) body.SetActive(true);              
        }
    }

    System.Collections.IEnumerator FocusNextFrame()
    {
        yield return null;
        autoFocusField.ActivateInputField();
        autoFocusField.caretPosition = autoFocusField.text.Length;
    }
}
