using System;
using System.Collections.Generic;
using Core.Logging;
using Core.SceneFlow;
using MainMenu;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MainMenuPresenterTests
    {
        private sealed class FakeView : IMainMenuView
        {
            public event Action JourneyClicked;
            public event Action ArenaClicked;

            public void ClickJourney() => JourneyClicked?.Invoke();
            public void ClickArena() => ArenaClicked?.Invoke();
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public List<string> Loaded = new();
            public void Load(string sceneName) => Loaded.Add(sceneName);
        }

        private sealed class NullLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private static (MainMenuPresenter presenter, FakeView view, FakeSceneLoader loader) Create()
        {
            var view = new FakeView();
            var loader = new FakeSceneLoader();
            var presenter = new MainMenuPresenter(view, loader, new NullLogger());
            presenter.Initialize();
            return (presenter, view, loader);
        }

        [Test]
        public void JourneyClick_LoadsAreaScene()
        {
            var (_, view, loader) = Create();

            view.ClickJourney();

            Assert.AreEqual(new[] { SceneNames.Area }, loader.Loaded);
        }

        [Test]
        public void ArenaClick_LoadsArenaScene()
        {
            var (_, view, loader) = Create();

            view.ClickArena();

            Assert.AreEqual(new[] { SceneNames.Arena }, loader.Loaded);
        }

        [Test]
        public void Dispose_Unsubscribes_SoClicksNoLongerLoad()
        {
            var (presenter, view, loader) = Create();

            presenter.Dispose();
            view.ClickJourney();
            view.ClickArena();

            Assert.IsEmpty(loader.Loaded);
        }
    }
}
