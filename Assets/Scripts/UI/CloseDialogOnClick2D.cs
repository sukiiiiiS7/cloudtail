using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Closes a world-space 2D dialog when this sprite is clicked.
/// Works with Physics2DRaycaster + EventSystem.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CloseDialogOnClick2D : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Dialog root GameObject to hide (scene instance).")]
    public GameObject dialogRoot;

    [Tooltip("Optional controller component exposing Hide() (e.g., DialogWire).")]
    public MonoBehaviour dialogWire;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dialogWire != null)
        {
            var hide = dialogWire.GetType().GetMethod("Hide");
            if (hide != null) { hide.Invoke(dialogWire, null); return; }
        }
        if (dialogRoot) dialogRoot.SetActive(false);
    }
}
