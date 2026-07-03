using System;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the boot menu: forwards the two mode buttons' clicks
    /// as events. No logic — routing lives in <see cref="MainMenuPresenter"/>.
    /// </summary>
    public class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField] private Button _journeyButton;
        [SerializeField] private Button _arenaButton;

        public event Action JourneyClicked;
        public event Action ArenaClicked;

        private void Awake()
        {
            _journeyButton.onClick.AddListener(HandleJourneyClicked);
            _arenaButton.onClick.AddListener(HandleArenaClicked);
        }

        private void OnDestroy()
        {
            _journeyButton.onClick.RemoveListener(HandleJourneyClicked);
            _arenaButton.onClick.RemoveListener(HandleArenaClicked);
        }

        private void HandleJourneyClicked() => JourneyClicked?.Invoke();

        private void HandleArenaClicked() => ArenaClicked?.Invoke();
    }
}
