using System;
using System.Collections.Generic;
using Core.Logging;
using Mutation.Core;
using Mutation.Data;
using Mutation.Data.Definitions;
using Mutation.Presenter;
using Mutation.View;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class MutationChoicePresenterTests
    {
        private sealed class SilentLogger : IGameLogger
        {
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
        }

        private sealed class FakeChoiceView : IMutationChoiceView
        {
            public int ShowChoicesCalls { get; private set; }
            public IReadOnlyList<MutationChoiceViewData> LastShown { get; private set; }
            public bool Visible { get; private set; }

            public event Action<int> OnChoiceSelected;

            public void ShowChoices(IReadOnlyList<MutationChoiceViewData> options)
            {
                ShowChoicesCalls++;
                LastShown = options;
            }

            public void SetVisible(bool visible)
            {
                Visible = visible;
            }

            public void RaiseSelected(int index)
            {
                OnChoiceSelected?.Invoke(index);
            }
        }

        private sealed class FakeMutationCharacter : IMutationCharacter
        {
            private readonly Dictionary<string, string> _equipped = new Dictionary<string, string>();

            public bool SwapResult { get; set; } = true;
            public int SwapCalls { get; private set; }
            public string LastSlotId { get; private set; }
            public string LastPartId { get; private set; }

            public void Equip(string slotId, string partId)
            {
                _equipped[slotId] = partId;
            }

            public bool SwapPart(string slotId, string partId)
            {
                SwapCalls++;
                LastSlotId = slotId;
                LastPartId = partId;
                if (SwapResult)
                {
                    _equipped[slotId] = partId;
                }

                return SwapResult;
            }

            public bool TryGetEquippedPartId(string slotId, out string partId)
            {
                return _equipped.TryGetValue(slotId, out partId);
            }
        }

        private sealed class FakePartCatalog : IMutationPartCatalog
        {
            private readonly List<MutationCandidatePart> _candidates = new List<MutationCandidatePart>();

            public IReadOnlyList<MutationCandidatePart> AllCandidates => _candidates;

            public void Add(string slot, string part, params (string archetype, float weight)[] affinity)
            {
                var map = new Dictionary<string, float>();
                string dominant = null;
                var best = float.NegativeInfinity;
                foreach (var (archetype, weight) in affinity)
                {
                    map[archetype] = weight;
                    if (weight > best)
                    {
                        best = weight;
                        dominant = archetype;
                    }
                }

                _candidates.Add(new MutationCandidatePart(slot, part, part, map, 0, dominant));
            }

            public bool TryGetIcon(string partId, out Sprite icon)
            {
                icon = null;
                return false;
            }
        }

        private sealed class FakeArchetypeCatalog : IArchetypeCatalog
        {
            public IReadOnlyList<ArchetypeDefinition> All => Array.Empty<ArchetypeDefinition>();
            public bool Contains(string archetypeId) => false;
            public bool TryGet(string archetypeId, out ArchetypeDefinition definition)
            {
                definition = null;
                return false;
            }
        }

        private MutationTally _tally;
        private DigestionProgress _digestion;
        private FakePartCatalog _partCatalog;
        private FakeChoiceView _view;
        private FakeMutationCharacter _character;
        private MutationConfig _config;
        private MutationChoicePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _tally = new MutationTally();
            _digestion = new DigestionProgress(1);
            _partCatalog = new FakePartCatalog();
            _view = new FakeChoiceView();
            _character = new FakeMutationCharacter();
            _config = ScriptableObject.CreateInstance<MutationConfig>();

            _presenter = NewPresenter(_digestion);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private MutationChoicePresenter NewPresenter(DigestionProgress digestion)
        {
            return new MutationChoicePresenter(
                digestion,
                _tally,
                new MutationOptionBuilder(),
                _partCatalog,
                new FakeArchetypeCatalog(),
                _character,
                _config,
                _view,
                new SilentLogger());
        }

        private void Feed(string archetypeId, float weight = 1f)
        {
            _tally.Add(ArtifactArchetypeProfile.Create(
                new[] { new KeyValuePair<string, float>(archetypeId, weight) }));
        }

        [Test]
        public void NotReady_DoesNotShow()
        {
            var digestion = new DigestionProgress(2);
            var presenter = NewPresenter(digestion);
            presenter.Initialize();
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            Feed("reptile");

            digestion.AddArtifact(); // 1 of 2 -> not ready

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
            presenter.Dispose();
        }

        [Test]
        public void ReadyFlips_ShowsChoicesAndVisible()
        {
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            _partCatalog.Add("slot.tail", "part.tail.r", ("reptile", 1f));
            Feed("reptile");

            _digestion.AddArtifact(); // ready

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            Assert.AreEqual(2, _view.LastShown.Count);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void Selection_SwapsThenResetsAndHides()
        {
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            Feed("reptile");
            _digestion.AddArtifact();

            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
            Assert.AreEqual("slot.head", _character.LastSlotId);
            Assert.AreEqual("part.head.r", _character.LastPartId);
            Assert.IsTrue(_tally.IsEmpty);
            Assert.AreEqual(0, _digestion.Fed);
            Assert.IsFalse(_digestion.IsReadyToMutate);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void Selection_SwapFails_NoResetStaysVisible()
        {
            _character.SwapResult = false;
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            Feed("reptile");
            _digestion.AddArtifact();

            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
            Assert.IsFalse(_tally.IsEmpty);
            Assert.IsTrue(_digestion.IsReadyToMutate);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void Ready_NoScoringOptions_DoesNotShowOrReset()
        {
            // A candidate exists, but it has no affinity to what was fed, so it scores zero.
            _partCatalog.Add("slot.head", "part.head.a", ("aquatic", 1f));
            Feed("reptile");

            _digestion.AddArtifact(); // ready

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
            Assert.IsTrue(_digestion.IsReadyToMutate);
            Assert.IsFalse(_tally.IsEmpty);
        }

        [Test]
        public void Ready_ExcludesAlreadyEquippedPart()
        {
            // Equip (never swapped) stands in for a starting part: it must not be re-offered.
            _character.Equip("slot.head", "part.head.r");
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            _partCatalog.Add("slot.tail", "part.tail.r", ("reptile", 1f));
            Feed("reptile");

            _digestion.AddArtifact();

            Assert.AreEqual(1, _view.LastShown.Count);
        }

        [Test]
        public void FurtherFeedingWhileShown_DoesNotReshow()
        {
            var digestion = new DigestionProgress(1);
            var presenter = NewPresenter(digestion);
            presenter.Initialize();
            _partCatalog.Add("slot.head", "part.head.r", ("reptile", 1f));
            Feed("reptile");

            digestion.AddArtifact(); // ready -> shows
            digestion.AddArtifact(); // still ready, already showing

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            presenter.Dispose();
        }
    }
}
