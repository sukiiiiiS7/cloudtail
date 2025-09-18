using System.Collections;
using UnityEngine;
using TMPro;

namespace Cloudtail.UI
{
    /// <summary>
    /// Guidance text controller for predefined phases.
    /// Phases: Intro (scene start), SaveComplete (after save), Seal (sealing ritual).
    /// Uses TextMeshProUGUI and an optional typewriter effect.
    /// </summary>
    [DisallowMultipleComponent]
    public class NokaGuideController : MonoBehaviour
    {
        public enum GuidePhase { Intro, SaveComplete, Seal }

        [Header("UI Target")]
        [Tooltip("Target TextMeshProUGUI component for rendering guidance text.")]
        public TextMeshProUGUI tmpText;

        [Header("Typing Effect")]
        [Tooltip("Enables typewriter effect when rendering lines.")]
        public bool typingEnabled = true;

        [Tooltip("Characters per second for typewriter effect.")]
        [Min(1f)]
        public float typingCps = 40f;

        [Header("Lines: Intro")]
        [TextArea(2, 6)]
        public string[] linesIntro =
        {
            "Welcome to Cloudtail.",
            "A new memory can be shaped into a gentle landmark.",
            "Tap an open space to place a keepsake."
        };

        [Header("Lines: Save Complete")]
        [TextArea(2, 6)]
        public string[] linesSaveComplete =
        {
            "The decoration has been saved.",
            "A planet has now been recorded in the archive."
        };

        [Header("Lines: Seal")]
        [TextArea(2, 6)]
        public string[] linesSeal =
        {
            "The sealing ritual is ready.",
            "Well done."
        };

        GuidePhase _currentPhase = GuidePhase.Intro;
        string[] _currentLines;
        int _lineIndex = 0;
        Coroutine _typingCo;

        void OnEnable()
        {
            SetPhase(GuidePhase.Intro);
        }

        /// <summary>
        /// Sets current phase and resets internal line index.
        /// </summary>
        /// <param name="phase">Target phase.</param>
        public void SetPhase(GuidePhase phase)
        {
            _currentPhase = phase;
            _currentLines = ResolveLines(phase);
            _lineIndex = 0;
            RenderCurrent();
        }

        /// <summary>
        /// Advances to next line within current phase.
        /// </summary>
        public void NextLine()
        {
            if (_currentLines == null || _currentLines.Length == 0) return;
            _lineIndex = Mathf.Min(_lineIndex + 1, _currentLines.Length - 1);
            RenderCurrent();
        }

        /// <summary>
        /// Advances rendering: if typing is in progress, completes the current line;
        /// otherwise advances to the next line.
        /// </summary>
        public void OnAdvance()
        {
            if (_typingCo != null)
            {
                StopCoroutine(_typingCo);
                _typingCo = null;

                if (_currentLines != null && _currentLines.Length > 0)
                    SetTextImmediate(_currentLines[_lineIndex]);

                return;
            }
            NextLine();
        }

        /// <summary>
        /// Switches to Intro phase.
        /// </summary>
        public void OnIntroPhase() => SetPhase(GuidePhase.Intro);

        /// <summary>
        /// Switches to SaveComplete phase.
        /// </summary>
        public void OnSaveCompletePhase() => SetPhase(GuidePhase.SaveComplete);

        /// <summary>
        /// Switches to Seal phase.
        /// </summary>
        public void OnSealPhase() => SetPhase(GuidePhase.Seal);

        /// <summary>
        /// Renders current line with optional typewriter effect.
        /// </summary>
        void RenderCurrent()
        {
            if (_currentLines == null || _currentLines.Length == 0) return;

            var text = _currentLines[_lineIndex];

            if (_typingCo != null)
            {
                StopCoroutine(_typingCo);
                _typingCo = null;
            }

            if (typingEnabled && typingCps > 0f)
                _typingCo = StartCoroutine(TypeRoutine(text));
            else
                SetTextImmediate(text);
        }

        IEnumerator TypeRoutine(string full)
        {
            SetTextImmediate(string.Empty);

            var len = string.IsNullOrEmpty(full) ? 0 : full.Length;
            if (len == 0) yield break;

            var delay = 1f / typingCps;
            for (int i = 1; i <= len; i++)
            {
                SetTextImmediate(full.Substring(0, i));
                yield return new WaitForSeconds(delay);
            }

            _typingCo = null;
        }

        void SetTextImmediate(string s)
        {
            if (tmpText != null) tmpText.text = s;
        }

        string[] ResolveLines(GuidePhase phase)
        {
            switch (phase)
            {
                case GuidePhase.Intro:        return linesIntro;
                case GuidePhase.SaveComplete: return linesSaveComplete;
                case GuidePhase.Seal:         return linesSeal;
                default:                      return linesIntro;
            }
        }
    }
}
