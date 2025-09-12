using UnityEngine;
using UnityEngine.EventSystems;

/// Attach to an inventory slot/button. On click, selects the prefab to place.
[DisallowMultipleComponent]
public class DecorSelectOnClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Prefab to spawn when this slot is clicked")]
    public GameObject decorPrefab;

    public void OnPointerClick(PointerEventData e)
    {
        if (decorPrefab != null && DecorPlacer.Instance != null)
            DecorPlacer.Instance.Select(decorPrefab);
    }
}
