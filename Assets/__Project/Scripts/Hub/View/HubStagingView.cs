using TMPro;
using UnityEngine;

namespace Hub.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the Hub's staging readout: paints the chosen-part label.
    /// No logic — staging lives in <see cref="Hub.Presenter.HubStagingPresenter"/>; all pick
    /// interactions are world-side F-spots (O1 rework).
    /// </summary>
    public class HubStagingView : MonoBehaviour, IHubStagingView
    {
        [Tooltip("Readout of what the launch will install ('—' for a bare launch).")]
        [SerializeField] private TMP_Text _chosenPartLabel;

        public void SetChosenPartLabel(string label)
        {
            if (_chosenPartLabel != null)
            {
                _chosenPartLabel.text = label;
            }
        }
    }
}
