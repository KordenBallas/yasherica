using System.Collections.Generic;
using Core.Persistence;
using Hub.Core;
using Hub.Data;
using Hub.Presenter;
using Hub.View;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 cauldron voice on the Hub: the death-return greeting consumes the arrival marker and
    /// wins over the empty-offer remark; picks and the launch speak their pools; missing content
    /// keeps the plaque quiet.
    /// </summary>
    [TestFixture]
    public class CauldronVoicePresenterTests
    {
        private sealed class FakeVoiceView : IHubVoiceView
        {
            public string LastLine;
            public void ShowLine(string line) => LastLine = line;
        }

        private sealed class FakeArrival : IHubArrivalStore
        {
            public bool DeathMarked;
            public void MarkDeathReturn() => DeathMarked = true;

            public bool TryConsumeDeathReturn()
            {
                bool marked = DeathMarked;
                DeathMarked = false;
                return marked;
            }
        }

        private sealed class FakeMetaStore : IMetaMemoryStore
        {
            public MetaMemorySnapshot LoadOrEmpty() => new MetaMemorySnapshot();
            public void Save(MetaMemorySnapshot snapshot) { }
        }

        private static CauldronVoiceLines Lines() => new CauldronVoiceLines(
            new Dictionary<string, IReadOnlyList<string>> { ["fox"] = new[] { "sly pick" } },
            new[] { "generic pick" },
            new[] { "nothing to bolt on" },
            new[] { "down we go" },
            new[] { "back from the dead" });

        private static (CauldronVoicePresenter presenter, HubStagingModel model, FakeVoiceView view,
            FakeArrival arrival) Create(CauldronVoiceLines lines = null, bool deathMarked = false,
                int offerSize = 0)
        {
            var model = new HubStagingModel();
            if (offerSize > 0)
            {
                var offer = new List<StartingPartCandidate>();
                for (int i = 0; i < offerSize; i++)
                {
                    offer.Add(new StartingPartCandidate($"p{i}", "fox", "s", true));
                }

                model.SetOffer(offer);
            }

            var view = new FakeVoiceView();
            var arrival = new FakeArrival { DeathMarked = deathMarked };
            var presenter = new CauldronVoicePresenter(model, view, lines ?? Lines(), arrival,
                new HubMetaReader(new FakeMetaStore()));
            presenter.Initialize();
            return (presenter, model, view, arrival);
        }

        [Test]
        public void DeathReturn_SpeaksAndConsumesTheMarker()
        {
            var (_, _, view, arrival) = Create(deathMarked: true);

            Assert.AreEqual("back from the dead", view.LastLine);
            Assert.IsFalse(arrival.DeathMarked, "the marker is one-shot");
        }

        [Test]
        public void EmptyOffer_WithoutDeathReturn_SpeaksTheBareLine()
        {
            var (_, _, view, _) = Create(offerSize: 0);

            Assert.AreEqual("nothing to bolt on", view.LastLine);
        }

        [Test]
        public void DeathReturn_WinsOverTheEmptyOfferLine()
        {
            var (_, _, view, _) = Create(deathMarked: true, offerSize: 0);

            Assert.AreEqual("back from the dead", view.LastLine);
        }

        [Test]
        public void NonEmptyOffer_SaysNothingAtArrival()
        {
            var (_, _, view, _) = Create(offerSize: 3);

            Assert.IsNull(view.LastLine);
        }

        [Test]
        public void PartPick_SpeaksTheRacePool()
        {
            var (_, model, view, _) = Create(offerSize: 3);

            model.ChoosePart(0);

            Assert.AreEqual("sly pick", view.LastLine);
        }

        [Test]
        public void Launch_SpeaksTheLaunchPool()
        {
            var (_, model, view, _) = Create(offerSize: 3);

            model.NotifyLaunching();

            Assert.AreEqual("down we go", view.LastLine);
        }

        [Test]
        public void EmptyContent_StaysQuiet()
        {
            var (_, model, view, _) = Create(CauldronVoiceLines.Empty, deathMarked: true);

            model.NotifyLaunching();

            Assert.IsNull(view.LastLine, "no authored lines = a quiet cauldron, never an error");
        }

        [Test]
        public void Dispose_Unsubscribes()
        {
            var (presenter, model, view, _) = Create(offerSize: 3);
            presenter.Dispose();

            model.NotifyLaunching();

            Assert.IsNull(view.LastLine);
        }
    }
}
