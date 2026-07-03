using System;
using Core.Logging;
using Core.SceneFlow;
using Zenject;

namespace MainMenu
{
    /// <summary>
    /// Routes the boot menu's mode choice to a scene load: Journey → the unchanged Area scene,
    /// Arena → the networked Arena scene. Pure C# presenter (MVP); the view is a thin adapter.
    /// </summary>
    public class MainMenuPresenter : IInitializable, IDisposable
    {
        private readonly IMainMenuView _view;
        private readonly ISceneLoader _sceneLoader;
        private readonly IGameLogger _logger;

        public MainMenuPresenter(IMainMenuView view, ISceneLoader sceneLoader, IGameLogger logger)
        {
            _view = view;
            _sceneLoader = sceneLoader;
            _logger = logger;
        }

        public void Initialize()
        {
            _view.JourneyClicked += HandleJourneyClicked;
            _view.ArenaClicked += HandleArenaClicked;
        }

        public void Dispose()
        {
            _view.JourneyClicked -= HandleJourneyClicked;
            _view.ArenaClicked -= HandleArenaClicked;
        }

        private void HandleJourneyClicked()
        {
            _logger.Info(LogCategory.Core, "[MainMenu] Journey selected");
            _sceneLoader.Load(SceneNames.Area);
        }

        private void HandleArenaClicked()
        {
            _logger.Info(LogCategory.Core, "[MainMenu] Arena selected");
            _sceneLoader.Load(SceneNames.Arena);
        }
    }
}
