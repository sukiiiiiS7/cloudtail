using UnityEngine;

[DisallowMultipleComponent]
public class DialogPanelSwitcher : MonoBehaviour
{
    public GameObject[] panels;

    public void ShowIndex(int idx)
    {
        if (panels == null || panels.Length == 0) return;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].SetActive(i == idx);
        }
    }

    void Awake()
    {
        ShowIndex(0); 
    }
}
