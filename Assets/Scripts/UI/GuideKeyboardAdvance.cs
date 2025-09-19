using UnityEngine;
using UnityEngine.InputSystem;

namespace Cloudtail.UI
{
    /// <summary>
    /// Keyboard/gamepad controls for the guide dialog (new Input System).
    /// Space = advance once.
    /// Enter = smart advance (complete typing then go next).
    /// Esc/Backspace = close dialog (hard SetActive false).
    /// Gamepad: A = smart advance, B = close.
    /// </summary>
    public class GuideKeyboardAdvance : MonoBehaviour
    {
        [Header("Refs")]
        public NokaGuideController guide;   // drag Dialog_Prefab's controller
        public GameObject dialogRoot;       // drag Dialog_Prefab instance

        void OnEnable()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame)
                    guide?.OnAdvance();

                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    StartCoroutine(SmartAdvance());

                if ((kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) && dialogRoot)
                    dialogRoot.SetActive(false);
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.buttonSouth.wasPressedThisFrame) // A / Cross
                    StartCoroutine(SmartAdvance());

                if (gp.buttonEast.wasPressedThisFrame)  // B / Circle
                    dialogRoot?.SetActive(false);
            }
        }

        System.Collections.IEnumerator SmartAdvance()
        {
            if (guide == null) yield break;
            guide.OnAdvance();   
            yield return null;   
            guide.OnAdvance();
        }
    }
}
