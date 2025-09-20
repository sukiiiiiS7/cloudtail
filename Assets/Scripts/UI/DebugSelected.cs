using UnityEngine;
using UnityEngine.EventSystems;
public class DebugSelected : MonoBehaviour
{
    void Update()
    {
        var go = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (go) Debug.Log("Selected: " + go.name);
    }
}
