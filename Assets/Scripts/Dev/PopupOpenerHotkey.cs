using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PopupOpenerHotkey : MonoBehaviour
{
    [Tooltip("PlanetSelectPopup_KeyboardOnly instance in the scene.")]
    public PlanetSelectPopup_KeyboardOnly planetSelectPopup;

    [Tooltip("Which index to suggest when opening.")]
    public int suggestionIndex = 0;

    [ContextMenu("Open Planet Popup (suggestionIndex)")]
    void ContextOpen()
    {
        if (!planetSelectPopup) { Debug.LogError("[Hotkey] planetSelectPopup is NULL."); return; }
        planetSelectPopup.OpenWithSuggestionIndex(suggestionIndex);
    }

    void Update()
    {
        if (!planetSelectPopup) return;

        var kb = Keyboard.current;
        if (kb != null && kb.pKey.wasPressedThisFrame)
        {
            if (planetSelectPopup.IsOpen)
                planetSelectPopup.Close();
            else
                planetSelectPopup.OpenWithSuggestionIndex(suggestionIndex);
        }
    }
}
