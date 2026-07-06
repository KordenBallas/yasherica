using System;
using System.Collections.Generic;
using Core.Logging;
using Core.Persistence;
using Core.SceneFlow;
using Hub.Core;
using Hub.Data;
using Hub.Presenter;
using Hub.View;
using LevelGeneration;
using Mutation.Core;
using Mutation.Data;
using Mutation.View;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;
using UnityEngine;
using World.Races.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 Hub staging orchestration (walkable-platform rework): the offer is dealt at init but
    /// shown only when the junk-keeper is talked to (re-showable until launch); homelands derive
    /// from the roster for the portals; a portal launch is the commit point (setup saved,
    /// abandoned run consumed, Area loaded, re-entry guarded). Bare launch and a missing card
    /// panel are first-class states, not errors.
    /// </summary>
    [TestFixture]
    public class HubStagingPresenterTests
    {
        // ---- fakes ---------------------------------------------------------------------------

        private sealed class FakeStagingView : IHubStagingView
        {
            public string ChosenPartLabel;
            public void SetChosenPartLabel(string label) => ChosenPartLabel = label;
        }

        private sealed class FakeChoiceView : IMutationChoiceView
        {
            public IReadOnlyList<MutationChoiceViewData> Shown;
            public bool? Visible;
            public int ShowCalls;

            public event Action<int> OnChoiceSelected;

            public void ShowChoices(IReadOnlyList<MutationChoiceViewData> options)
            {
                Shown = options;
                ShowCalls++;
            }

            public void SetVisible(bool visible) => Visible = visible;
            public void Pick(int index) => OnChoiceSelected?.Invoke(index);
        }

        private sealed class FakePoolSource : IStartingPartPoolSource
        {
            public IReadOnlyList<StartingPartCandidate> Pool = Array.Empty<StartingPartCandidate>();
            public IReadOnlyList<StartingPartCandidate> BuildPool() => Pool;
        }

        private sealed class FakeCardCatalog : IMutationPartCatalog
        {
            private readonly Dictionary<string, MutationPartCardData> _cards =
                new Dictionary<string, MutationPartCardData>();

            public FakeCardCatalog With(string partId, string displayName)
            {
                _cards[partId] = new MutationPartCardData(displayName, null, 0, null);
                return this;
            }

            public IReadOnlyList<MutationCandidatePart> AllCandidates =>
                Array.Empty<MutationCandidatePart>();

            public bool TryGetIcon(string partId, out Sprite icon)
            {
                icon = null;
                return false;
            }

            public bool TryGetCardData(string partId, out MutationPartCardData cardData) =>
                _cards.TryGetValue(partId ?? string.Empty, out cardData);
        }

        private sealed class FakeTints : IRaceTintCatalog
        {
            public Color TintFor(string raceId) =>
                raceId == "fox" ? Color.red : Color.white;
        }

        private sealed class FakeMetaStore : IMetaMemoryStore
        {
            public MetaMemorySnapshot LoadOrEmpty() => new MetaMemorySnapshot();
            public void Save(MetaMemorySnapshot snapshot) { }
        }

        private sealed class FakeRoster : IRaceRoster
        {
            private readonly List<RaceData> _races;
            public FakeRoster(params RaceData[] races) => _races = new List<RaceData>(races);

            public IReadOnlyList<RaceData> All => _races;
            public bool Contains(string raceId) => _races.Exists(r => r.Id == raceId);

            public bool TryGet(string raceId, out RaceData race)
            {
                race = _races.Find(r => r.Id == raceId);
                return race != null;
            }
        }

        private sealed class FakeSetupStore : IRunSetupStore
        {
            public RunSetupSnapshot Saved;
            public int SaveCalls;
            public bool TryLoad(out RunSetupSnapshot snapshot) { snapshot = Saved; return Saved != null; }
            public void Save(RunSetupSnapshot snapshot) { Saved = snapshot; SaveCalls++; }
            public void Delete() => Saved = null;
        }

        private sealed class FakeRunStore : IRunSaveStore
        {
            public RunSaveSnapshot Saved;
            public bool Exists() => Saved != null;
            public bool TryLoad(out RunSaveSnapshot snapshot) { snapshot = Saved; return Saved != null; }
            public void Save(RunSaveSnapshot snapshot) => Saved = snapshot;
            public void Delete() => Saved = null;
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public readonly List<string> Loaded = new List<string>();
            public void Load(string sceneName) => Loaded.Add(sceneName);
        }

        private sealed class NullLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        // ---- harness ---------------------------------------------------------------------------

        private sealed class Harness
        {
            public readonly HubStagingModel Model = new HubStagingModel();
            public readonly FakeStagingView View = new FakeStagingView();
            public readonly FakeChoiceView ChoiceView = new FakeChoiceView();
            public readonly FakePoolSource Pool = new FakePoolSource();
            public readonly FakeCardCatalog Cards = new FakeCardCatalog();
            public readonly FakeSetupStore SetupStore = new FakeSetupStore();
            public readonly FakeRunStore RunStore = new FakeRunStore();
            public readonly FakeLoader Loader = new FakeLoader();
            public HubStagingPresenter Presenter;

            public Harness Start(bool withChoiceView = true, IRaceRoster roster = null)
            {
                Presenter = new HubStagingPresenter(
                    Model, View, Pool, new StartingPartSelector(), Cards, new FakeTints(),
                    new HubMetaReader(new FakeMetaStore()),
                    roster ?? new FakeRoster(
                        new RaceData("fox", "Fox-folk", LevelTheme.Forest),
                        new RaceData("lizard", "Lizard-folk", LevelTheme.Desert),
                        new RaceData("ibex", "Ibex-folk", LevelTheme.Mountain)),
                    SetupStore, RunStore, Loader, new NullLogger(),
                    withChoiceView ? ChoiceView : null);
                Presenter.Initialize();
                return this;
            }
        }

        private static StartingPartCandidate Part(string id, string race = "", string slot = "s") =>
            new StartingPartCandidate(id, race, slot, hasActiveAbility: true);

        // ---- offer via the keeper ----------------------------------------------------------------

        [Test]
        public void Init_DealsTheOffer_ButKeepsThePanelHidden()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox") };
            h.Cards.With("p1", "Fox Leg");
            h.Start();

            Assert.AreEqual(1, h.Model.Offer.Count, "the offer is dealt at init");
            Assert.IsNull(h.ChoiceView.Shown, "the cards wait for the keeper talk");
            Assert.IsFalse(h.ChoiceView.Visible);
        }

        [Test]
        public void ShowOffer_OpensThePanel_WithTintedCards()
        {
            var h = new Harness();
            h.Pool.Pool = new[]
            {
                Part("p1", "fox", "s1"), Part("p2", "ibex", "s2"),
                Part("p3", "lizard", "s3"), Part("p4", "", "s4")
            };
            h.Cards.With("p1", "Fox Leg").With("p2", "Ibex Horn")
                .With("p3", "Lizard Tail").With("p4", "Scrap");
            h.Start();

            h.Presenter.ShowOffer();

            Assert.AreEqual(3, h.ChoiceView.Shown.Count, "the canonical 1-of-3 deal");
            Assert.IsTrue(h.ChoiceView.Visible);
            Assert.IsFalse(h.ChoiceView.Shown[0].HasReplacedPart,
                "a launch installs into a fresh body — no back face");
        }

        [Test]
        public void PartPick_UpdatesModelAndReadout_HidesThePanel_AndCanBeRevised()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox", "s1"), Part("p2", "ibex", "s2") };
            h.Cards.With("p1", "Fox Leg").With("p2", "Ibex Horn");
            h.Start();

            // The dealt order is the selector's (deterministic, not pool order) — pick by index
            // and assert against the dealt offer.
            var labels = new Dictionary<string, string> { ["p1"] = "Fox Leg", ["p2"] = "Ibex Horn" };
            h.Presenter.ShowOffer();
            h.ChoiceView.Pick(0);

            Assert.AreEqual(h.Model.Offer[0].PartId, h.Model.ChosenPartId);
            Assert.AreEqual(labels[h.Model.ChosenPartId], h.View.ChosenPartLabel);
            Assert.IsFalse(h.ChoiceView.Visible);

            // Talking to the keeper again re-opens the deal; a new pick revises the choice.
            h.Presenter.ShowOffer();
            Assert.IsTrue(h.ChoiceView.Visible);
            h.ChoiceView.Pick(1);
            Assert.AreEqual(h.Model.Offer[1].PartId, h.Model.ChosenPartId);
        }

        [Test]
        public void EmptyPool_MakesShowOfferANoOp()
        {
            var h = new Harness().Start();

            h.Presenter.ShowOffer();

            Assert.IsNull(h.ChoiceView.Shown, "a cold start has nothing to deal");
        }

        // ---- homelands for the portals ---------------------------------------------------------

        [Test]
        public void Homelands_DeriveFromTheRoster_InOrder()
        {
            var h = new Harness().Start();

            Assert.AreEqual(3, h.Presenter.Homelands.Count);
            Assert.AreEqual(LevelTheme.Forest, h.Presenter.Homelands[0].Theme);
            Assert.AreEqual("Forest — Fox-folk homeland", h.Presenter.Homelands[0].Label);
            Assert.AreEqual("Desert", h.Presenter.Homelands[1].PromptName);
        }

        [Test]
        public void EmptyRoster_FallsBackToTheThreeStartingBiomes()
        {
            var h = new Harness().Start(roster: new FakeRoster());

            CollectionAssert.AreEqual(
                new[] { LevelTheme.Forest, LevelTheme.Desert, LevelTheme.Mountain },
                new List<HubHomeland>(h.Presenter.Homelands).ConvertAll(x => x.Theme));
        }

        // ---- portal launch -----------------------------------------------------------------------

        [Test]
        public void LaunchInto_SavesTheSetup_ConsumesTheOldRun_LoadsArea()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox") };
            h.Cards.With("p1", "Fox Leg");
            h.RunStore.Saved = new RunSaveSnapshot(); // an abandoned in-progress run
            h.Start();

            bool launched = false;
            h.Model.Launching += () => launched = true;
            h.Presenter.ShowOffer();
            h.ChoiceView.Pick(0);
            h.Presenter.LaunchInto(LevelTheme.Mountain);

            Assert.AreEqual("p1", h.SetupStore.Saved.StartingPartId);
            Assert.AreEqual("Mountain", h.SetupStore.Saved.StartingBiome);
            Assert.AreEqual(LevelTheme.Mountain, h.Model.ChosenBiome);
            Assert.IsFalse(h.RunStore.Exists(), "the portal is the new-run commit point (revised P2-2)");
            Assert.AreEqual(new[] { SceneNames.Area }, h.Loader.Loaded);
            Assert.IsTrue(launched);
        }

        [Test]
        public void BareLaunch_IsAlwaysAllowed()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox") };
            h.Cards.With("p1", "Fox Leg");
            h.Start();

            h.Presenter.LaunchInto(LevelTheme.Forest); // no part picked

            Assert.AreEqual(string.Empty, h.SetupStore.Saved.StartingPartId);
            Assert.AreEqual(new[] { SceneNames.Area }, h.Loader.Loaded);
        }

        [Test]
        public void SecondLaunch_IsGuarded()
        {
            var h = new Harness().Start();

            h.Presenter.LaunchInto(LevelTheme.Forest);
            h.Presenter.LaunchInto(LevelTheme.Desert);
            h.Presenter.ShowOffer();

            Assert.AreEqual(1, h.SetupStore.SaveCalls, "one launch, one commit");
            Assert.AreEqual("Forest", h.SetupStore.Saved.StartingBiome);
            Assert.AreEqual(1, h.Loader.Loaded.Count);
            Assert.IsNull(h.ChoiceView.Shown, "no re-deal mid-launch");
        }

        [Test]
        public void MissingChoiceView_IsTolerated_BareLaunchOnly()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox") };
            h.Start(withChoiceView: false);

            h.Presenter.ShowOffer(); // no panel — a quiet no-op
            h.Presenter.LaunchInto(LevelTheme.Desert);

            Assert.AreEqual(string.Empty, h.SetupStore.Saved.StartingPartId);
            Assert.AreEqual(new[] { SceneNames.Area }, h.Loader.Loaded);
        }

        [Test]
        public void Dispose_UnsubscribesFromTheCardPanel()
        {
            var h = new Harness();
            h.Pool.Pool = new[] { Part("p1", "fox") };
            h.Cards.With("p1", "Fox Leg");
            h.Start();

            h.Presenter.Dispose();
            h.ChoiceView.Pick(0);

            Assert.AreEqual(string.Empty, h.Model.ChosenPartId);
        }
    }
}
