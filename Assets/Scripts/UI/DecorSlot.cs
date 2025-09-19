using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DecorSlot : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("The prefab this slot represents.")]
    public GameObject prefab;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DecorPlacer.Instance != null && prefab != null)
        {
            DecorPlacer.Instance.Select(prefab);
            Debug.Log("[Slot] Click -> Select " + prefab.name);
        }
        else
        {
            Debug.LogWarning("[Slot] Click but no DecorPlacer or prefab not set.");
        }
    }
}
