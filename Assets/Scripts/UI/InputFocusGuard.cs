using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class InputFocusGuard : MonoBehaviour
{
    public TMP_InputField field;
    [Tooltip("Keep re-focusing for this duration after enabled.")]
    public float guardSeconds = 0.4f;

    void OnEnable() { StartCoroutine(FocusAndGuard()); }

    System.Collections.IEnumerator FocusAndGuard()
    {
        if (field == null) field = GetComponent<TMP_InputField>();
        if (field == null) yield break;

        yield return null;

        float t = 0f;
        while (t < guardSeconds && isActiveAndEnabled)
        {
            field.interactable = true;
            field.readOnly = false;

            EventSystem.current?.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
            field.caretPosition = field.text?.Length ?? 0;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
