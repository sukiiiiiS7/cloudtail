using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HelpToggleUI : MonoBehaviour
{
    [SerializeField] GameObject panel;                // Panel_Help
    [SerializeField] MiniVerticalScroller scroller;   
    [SerializeField] KeyScrollRelay keyRelay;        
    [SerializeField] bool resetToTopOnOpen = true;

    public void Toggle()
    {
        bool show = !panel.activeSelf;
        panel.SetActive(show);

        if (show)
        {
            
            EventSystem.current?.SetSelectedGameObject(null);

            
            scroller?.RecalcAndClamp();
            if (resetToTopOnOpen) scroller?.SnapTop();

            if (keyRelay) keyRelay.enabled = true;
        }
        else
        {
            if (keyRelay) keyRelay.enabled = false;
        }
    }
}
