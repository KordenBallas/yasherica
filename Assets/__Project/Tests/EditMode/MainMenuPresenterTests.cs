using System;
using System.Collections.Generic;
using Core.Logging;
using Core.Persistence;
using Core.SceneFlow;
using MainMenu;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Menu routing plus the P2-2 Continue entry point (A1): visible iff an in-progress run save
    /// exists; Continue resumes (loads Area with the save intact); Journey stages on the Hub (O1)
    /// and must NOT touch the save — the new run commits at the Hub's launch, so backing out of
    /// the Hub keeps Continue alive. The world memory is untouched either way.
    /// </summary>
    [TestFixture]
    public class MainMenuPresenterTests
    {
        private sealed class FakeView : IMainMenuView
        {
            public bool? ContinueVisible;
            public event Action JourneyClicked;
            public event Action ContinueClicked;
            public event Action ArenaClicked;

            public void SetContinueVisible(bool visible) => ContinueVisible = visible;
            public void ClickJourney() => JourneyClicked?.Invoke();
            public void ClickContinue() => ContinueClicked?.Invoke();
            public void ClickArena() => ArenaClicked?.Invoke();
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public List<string> Loaded = new();
            public void Load(string sceneName) => Loaded.Add(sceneName);
        }

        private sealed class FakeRunSaveStore : IRunSaveStore
        {
            public RunSaveSnapshot Saved;
            public bool Exists() => Saved != null;
            public bool TryLoad(out RunSaveSnapshot snapshot) { snapshot = Saved; return Saved != null; }
            public void Save(RunSaveSnapshot snapshot) => Saved = snapshot;
            public void Delete() => Saved = null;
        }

        private sealed class NullLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private static (MainMenuPresenter presenter, FakeView view, FakeSceneLoader loader, FakeRunSaveStore store)
            Create(bool withSave = false)
        {
            var view = new FakeView();
            var loader = new FakeSceneLoader();
            var store = new FakeRunSaveStore { Saved = withSave ? new RunSaveSnapshot() : null };
            var presenter = new MainMenuPresenter(view, loader, store, new NullLogger());
            presenter.Initialize();
            return (presenter, view, loader, store);
        }

        [Test]
        public void JourneyClick_LoadsTheHubScene_O1()
        {
            var (_, view, loader, _) = Create();

            view.ClickJourney();

            Assert.AreEqual(new[] { SceneNames.Hub }, loader.Loaded);
        }

        [Test]
        public void ArenaClick_LoadsArenaScene()
        {
            var (_, view, loader, store) = Create(withSave: true);

            view.ClickArena();

            Assert.AreEqual(new[] { SceneNames.Arena }, loader.Loaded);
            Assert.IsTrue(store.Exists(), "Arena must leave the run save alone");
        }

        [Test]
        public void Continue_IsVisible_OnlyWhenARunSaveExists()
        {
            Assert.IsTrue(Create(withSave: true).view.ContinueVisible);
            Assert.IsFalse(Create(withSave: false).view.ContinueVisible);
        }

        [Test]
        public void ContinueClick_LoadsArea_WithTheSaveIntact()
        {
            var (_, view, loader, store) = Create(withSave: true);

            view.ClickContinue();

            Assert.AreEqual(new[] { SceneNames.Area }, loader.Loaded);
            Assert.IsTrue(store.Exists(), "Continue must not consume the save — the Area boot reads it");
        }

        [Test]
        public void JourneyClick_LeavesTheSaveAlone_TheHubLaunchIsTheCommitPoint_O1()
        {
            var (_, view, loader, store) = Create(withSave: true);

            view.ClickJourney();

            Assert.AreEqual(new[] { SceneNames.Hub }, loader.Loaded);
            Assert.IsTrue(store.Exists(),
                "backing out of the Hub must keep Continue alive; the delete happens at launch");
        }

        [Test]
        public void Dispose_Unsubscribes_SoClicksNoLongerLoad()
        {
            var (presenter, view, loader, _) = Create();

            presenter.Dispose();
            view.ClickJourney();
            view.ClickContinue();
            view.ClickArena();

            Assert.IsEmpty(loader.Loaded);
        }
    }
}
