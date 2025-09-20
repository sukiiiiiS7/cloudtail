using UnityEngine;
using UnityEngine.EventSystems;

/// Toggles a help panel from a world-space sprite button.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HelpButton2D : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject clickCatcher; // optional

    public void OnPointerClick(PointerEventData eventData)
    {
        if (helpPanel == null) return;

        bool next = !helpPanel.activeSelf;
        helpPanel.SetActive(next);
        if (clickCatcher != null) clickCatcher.SetActive(next);

        // Modal gate sync (optional but recommended)
        if (next) UiModalGate.Push();
        else      UiModalGate.Pop();
    }
}
