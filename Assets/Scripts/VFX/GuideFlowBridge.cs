using System.Collections;
using UnityEngine;

namespace Cloudtail.UI
{
    /// <summary>
    /// Bridges gameplay events (save success, seal begin) to dialog visibility and guidance phases.
    /// Ensures dialog is visible before switching phase and guards against OnShown overrides by delaying one frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuideFlowBridge : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Dialog root GameObject instance in the scene.")]
        public GameObject dialogPrefab;

        [Tooltip("Guide controller attached on the dialog root.")]
        public NokaGuideController guide;

        [Tooltip("Optional show/hide controller that exposes Show()/Hide() methods (e.g., DialogWire).")]
        public MonoBehaviour dialogWire;

        [Header("Timing")]
        [Tooltip("If true, delays one frame before switching phase to avoid OnShown overriding Intro.")]
        public bool delayOneFrameBeforePhase = true;

        /// <summary>
        /// Shows dialog and switches to Intro phase.
        /// </summary>
        public void ShowIntro()
        {
            StartCoroutine(ShowThenPhase(NokaGuideController.GuidePhase.Intro));
        }

        /// <summary>
        /// Shows dialog and switches to SaveComplete phase.
        /// Call this after save succeeded (local write OK or backend 2xx).
        /// </summary>
        public void ShowSaveComplete()
        {
            StartCoroutine(ShowThenPhase(NokaGuideController.GuidePhase.SaveComplete));
        }

        /// <summary>
        /// Shows dialog and switches to Seal phase.
        /// Call this at the beginning of the sealing ritual.
        /// </summary>
        public void ShowSeal()
        {
            StartCoroutine(ShowThenPhase(NokaGuideController.GuidePhase.Seal));
        }

        /// <summary>
        /// Hides dialog via DialogWire.Hide() if available, otherwise SetActive(false).
        /// </summary>
        public void HideDialog()
        {
            if (dialogPrefab == null) return;

            if (dialogWire != null)
            {
                var m = dialogWire.GetType().GetMethod("Hide");
                if (m != null) { m.Invoke(dialogWire, null); return; }
            }
            dialogPrefab.SetActive(false);
        }

        /// <summary>
        /// Shows dialog via DialogWire.Show() if available, otherwise SetActive(true).
        /// </summary>
        public void ShowDialog()
        {
            if (dialogPrefab == null) return;

            if (dialogWire != null)
            {
                var m = dialogWire.GetType().GetMethod("Show");
                if (m != null) { m.Invoke(dialogWire, null); return; }
            }
            dialogPrefab.SetActive(true);
        }

        IEnumerator ShowThenPhase(NokaGuideController.GuidePhase phase)
        {
            if (dialogPrefab == null || guide == null) yield break;

            // Ensure visibility first.
            if (!dialogPrefab.activeSelf)
                ShowDialog();

            // Let OnEnable/OnShown complete to avoid race with Intro.
            if (delayOneFrameBeforePhase)
                yield return null;

            // Switch guidance phase.
            guide.SetPhase(phase);
        }
    }
}
