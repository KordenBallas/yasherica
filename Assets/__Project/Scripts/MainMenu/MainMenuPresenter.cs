using System;
using Core.Logging;
using Core.Persistence;
using Core.SceneFlow;
using Zenject;

namespace MainMenu
{
    /// <summary>
    /// Routes the boot menu's mode choice to a scene load: Continue → resume the in-progress run
    /// (the run save on disk is the carrier — the Area scene restores whatever run.json holds),
    /// Journey → the Hub staging ground (O1; the new run commits — and consumes any old save — at
    /// the Hub's LAUNCH, so backing out of the Hub keeps Continue alive), Arena → the networked
    /// Arena scene. Pure C# presenter (MVP); the view is a thin adapter.
    /// </summary>
    public class MainMenuPresenter : IInitializable, IDisposable
    {
        private readonly IMainMenuView _view;
        private readonly ISceneLoader _sceneLoader;
        private readonly IRunSaveStore _runSaveStore;
        private readonly IGameLogger _logger;

        public MainMenuPresenter(IMainMenuView view, ISceneLoader sceneLoader,
            IRunSaveStore runSaveStore, IGameLogger logger)
        {
            _view = view;
            _sceneLoader = sceneLoader;
            _runSaveStore = runSaveStore;
            _logger = logger;
        }

        public void Initialize()
        {
            _view.JourneyClicked += HandleJourneyClicked;
            _view.ContinueClicked += HandleContinueClicked;
            _view.ArenaClicked += HandleArenaClicked;
            _view.SetContinueVisible(_runSaveStore.Exists());
        }

        public void Dispose()
        {
            _view.JourneyClicked -= HandleJourneyClicked;
            _view.ContinueClicked -= HandleContinueClicked;
            _view.ArenaClicked -= HandleArenaClicked;
        }

        private void HandleJourneyClicked()
        {
            // PO decision (P2-2, revised by O1): Journey is always a NEW run, but the commit point
            // — deleting the old save — moved to the Hub's launch action. Entering the Hub to look
            // around costs nothing; an in-progress run survives until an actual launch.
            _logger.Info(LogCategory.Core, "[MainMenu] Journey selected (staging on the Hub)");
            _sceneLoader.Load(SceneNames.Hub);
        }

        private void HandleContinueClicked()
        {
            _logger.Info(LogCategory.Core, "[MainMenu] Continue selected (resuming the saved run)");
            _sceneLoader.Load(SceneNames.Area);
        }

        private void HandleArenaClicked()
        {
            _logger.Info(LogCategory.Core, "[MainMenu] Arena selected");
            _sceneLoader.Load(SceneNames.Arena);
        }
    }
}
