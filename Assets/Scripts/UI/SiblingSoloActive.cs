using UnityEngine;
[DisallowMultipleComponent]
public class SiblingSoloActive : MonoBehaviour
{
    void OnEnable()
    {
        var p = transform.parent; if (p == null) return;
        for (int i = 0; i < p.childCount; i++)
        {
            var t = p.GetChild(i); if (t == transform) continue;
            var go = t.gameObject; if (go.activeSelf) go.SetActive(false);
        }
    }
}
