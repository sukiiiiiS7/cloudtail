using UnityEngine;
using UnityEngine.EventSystems;
using Cloudtail.IO;

[RequireComponent(typeof(BoxCollider2D))]
public class SaveClickArea : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("PlacementSave reference in the scene (e.g., on SaveManager).")]
    public PlacementSave save;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (save != null) save.SaveNow();
    }
}
