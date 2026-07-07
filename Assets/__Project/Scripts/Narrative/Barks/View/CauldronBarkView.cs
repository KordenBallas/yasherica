using System.Collections;
using TMPro;
using UnityEngine;

namespace Narrative.Barks.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the cauldron's in-run bark bubble (P1-10): paints one short
    /// line, hides itself after the display window. No logic — line selection lives in
    /// <c>CauldronBarkService</c>; a new bark simply restarts the window.
    /// </summary>
    public class CauldronBarkView : MonoBehaviour, ICauldronBarkView
    {
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("A CHILD visual root toggled with the line. Never this component's own object — " +
                 "the view must stay injectable while the bubble is hidden.")]
        [SerializeField] private GameObject _bubbleRoot;

        [Tooltip("Seconds the bark stays on screen before hiding.")]
        [SerializeField] private float _displaySeconds = 4f;

        private Coroutine _hideRoutine;

        private void Awake()
        {
            SetShown(false);
        }

        public void ShowBark(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            if (_lineText != null)
            {
                _lineText.text = line;
            }

            SetShown(true);
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            _hideRoutine = StartCoroutine(HideAfterWindow());
        }

        private IEnumerator HideAfterWindow()
        {
            yield return new WaitForSeconds(_displaySeconds);
            SetShown(false);
            _hideRoutine = null;
        }

        private void SetShown(bool shown)
        {
            if (_bubbleRoot != null)
            {
                _bubbleRoot.SetActive(shown);
            }
        }
    }
}
