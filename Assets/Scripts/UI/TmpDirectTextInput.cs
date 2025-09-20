using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

[DisallowMultipleComponent]
public class TmpDirectTextInput : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField field;           
    public InputSubmitter submitter;       

    [Header("Behavior")]
    public bool submitOnEnter = true;

    void Awake()
    {
        if (field == null) field = GetComponent<TMP_InputField>();
    }

    void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnText;  
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnText;
    }

    void Update()
    {
        if (field == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backspaceKey.wasPressedThisFrame && field.text.Length > 0)
        {
            field.text = field.text.Substring(0, field.text.Length - 1);
            field.caretPosition = field.text.Length;
        }

        if (submitOnEnter && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            if (submitter != null)
                submitter.SendNow(); 
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            field.DeactivateInputField();
        }
    }

    void OnText(char c)
    {
        if (field == null || !field.interactable) return;

        if (!field.isFocused) field.ActivateInputField();
        if (char.IsControl(c)) return;

        field.text += c;
        field.caretPosition = field.text.Length;
    }
}
