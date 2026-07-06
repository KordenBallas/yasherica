using System;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the boot menu: forwards the mode buttons' clicks
    /// as events and toggles the Continue button. No logic — routing lives in
    /// <see cref="MainMenuPresenter"/>.
    /// </summary>
    public class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField] private Button _journeyButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _arenaButton;

        public event Action JourneyClicked;
        public event Action ContinueClicked;
        public event Action ArenaClicked;

        private void Awake()
        {
            _journeyButton.onClick.AddListener(HandleJourneyClicked);
            _arenaButton.onClick.AddListener(HandleArenaClicked);
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(HandleContinueClicked);
            }
        }

        private void OnDestroy()
        {
            _journeyButton.onClick.RemoveListener(HandleJourneyClicked);
            _arenaButton.onClick.RemoveListener(HandleArenaClicked);
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinueClicked);
            }
        }

        public void SetContinueVisible(bool visible)
        {
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(visible);
            }
        }

        private void HandleJourneyClicked() => JourneyClicked?.Invoke();

        private void HandleContinueClicked() => ContinueClicked?.Invoke();

        private void HandleArenaClicked() => ArenaClicked?.Invoke();
    }
}
