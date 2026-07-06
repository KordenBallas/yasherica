using TMPro;
using UnityEngine;

namespace Hub.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the cauldron-voice plaque: paints one line, hides on empty.
    /// No logic — line selection lives in <see cref="Hub.Presenter.CauldronVoicePresenter"/>.
    /// </summary>
    public class HubVoicePlaqueView : MonoBehaviour, IHubVoiceView
    {
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("A CHILD visual root toggled with the line. Never this component's own object — " +
                 "the view must stay findable/injectable while the plaque is hidden.")]
        [SerializeField] private GameObject _plaqueRoot;

        private void Awake()
        {
            ShowLine(string.Empty);
        }

        public void ShowLine(string line)
        {
            bool hasLine = !string.IsNullOrEmpty(line);
            if (_plaqueRoot != null)
            {
                _plaqueRoot.SetActive(hasLine);
            }

            if (_lineText != null)
            {
                _lineText.text = hasLine ? line : string.Empty;
            }
        }
    }
}
