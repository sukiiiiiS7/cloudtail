using UnityEngine;
using UnityEngine.EventSystems;

public class UiClickProbe : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        Debug.Log($"[Probe] Clicked {name}");
    }
}
