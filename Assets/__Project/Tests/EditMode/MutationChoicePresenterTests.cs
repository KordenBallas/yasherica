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

        private sealed class FakeOptionCatalog : IMutationOptionCatalog
        {
            private readonly Dictionary<string, IReadOnlyList<MutationOption>> _byArchetype =
                new Dictionary<string, IReadOnlyList<MutationOption>>();

            public void Set(string archetypeId, params MutationOption[] options)
            {
                _byArchetype[archetypeId] = options;
            }

            public IReadOnlyList<MutationOption> OptionsFor(string archetypeId)
            {
                return _byArchetype.TryGetValue(archetypeId, out var options)
                    ? options
                    : Array.Empty<MutationOption>();
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
        private FakeOptionCatalog _optionCatalog;
        private FakeChoiceView _view;
        private FakeMutationCharacter _character;
        private MutationConfig _config;
        private MutationChoicePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _tally = new MutationTally();
            _digestion = new DigestionProgress(1);
            _optionCatalog = new FakeOptionCatalog();
            _view = new FakeChoiceView();
            _character = new FakeMutationCharacter();
            _config = ScriptableObject.CreateInstance<MutationConfig>();

            _presenter = new MutationChoicePresenter(
                _digestion,
                _tally,
                new MutationOptionBuilder(),
                _optionCatalog,
                new FakeArchetypeCatalog(),
                _character,
                _config,
                _view,
                new SilentLogger());
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void GiveDominant(string archetypeId)
        {
            _tally.Add(ArtifactArchetypeProfile.Create(
                new[] { new KeyValuePair<string, float>(archetypeId, 1f) }));
        }

        private static MutationOption Option(string slot, string part, string archetype)
        {
            return new MutationOption(slot, part, archetype, part);
        }

        [Test]
        public void NotReady_DoesNotShow()
        {
            var digestion = new DigestionProgress(2);
            var presenter = new MutationChoicePresenter(
                digestion, _tally, new MutationOptionBuilder(), _optionCatalog,
                new FakeArchetypeCatalog(), _character, _config, _view, new SilentLogger());
            presenter.Initialize();
            GiveDominant("reptile");
            _optionCatalog.Set("reptile", Option("slot.head", "part.head.r", "reptile"));

            digestion.AddArtifact(); // 1 of 2 -> not ready

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
            presenter.Dispose();
        }

        [Test]
        public void ReadyFlips_ShowsChoicesAndVisible()
        {
            GiveDominant("reptile");
            _optionCatalog.Set("reptile",
                Option("slot.head", "part.head.r", "reptile"),
                Option("slot.tail", "part.tail.r", "reptile"));

            _digestion.AddArtifact(); // ready

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            Assert.AreEqual(2, _view.LastShown.Count);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void Selection_SwapsThenResetsAndHides()
        {
            GiveDominant("reptile");
            _optionCatalog.Set("reptile", Option("slot.head", "part.head.r", "reptile"));
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
            GiveDominant("reptile");
            _optionCatalog.Set("reptile", Option("slot.head", "part.head.r", "reptile"));
            _digestion.AddArtifact();

            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
            Assert.IsFalse(_tally.IsEmpty);
            Assert.IsTrue(_digestion.IsReadyToMutate);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void Ready_NoOptions_DoesNotShowOrReset()
        {
            GiveDominant("reptile"); // dominant exists, but no part set authored for it

            _digestion.AddArtifact(); // ready

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
            Assert.IsTrue(_digestion.IsReadyToMutate);
            Assert.IsFalse(_tally.IsEmpty);
        }

        [Test]
        public void Ready_ExcludesAlreadyEquippedPart()
        {
            _character.Equip("slot.head", "part.head.r");
            GiveDominant("reptile");
            _optionCatalog.Set("reptile",
                Option("slot.head", "part.head.r", "reptile"),
                Option("slot.tail", "part.tail.r", "reptile"));

            _digestion.AddArtifact();

            Assert.AreEqual(1, _view.LastShown.Count);
        }

        [Test]
        public void FurtherFeedingWhileShown_DoesNotReshow()
        {
            var digestion = new DigestionProgress(1);
            var presenter = new MutationChoicePresenter(
                digestion, _tally, new MutationOptionBuilder(), _optionCatalog,
                new FakeArchetypeCatalog(), _character, _config, _view, new SilentLogger());
            presenter.Initialize();
            GiveDominant("reptile");
            _optionCatalog.Set("reptile", Option("slot.head", "part.head.r", "reptile"));

            digestion.AddArtifact(); // ready -> shows
            digestion.AddArtifact(); // still ready, already showing

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            presenter.Dispose();
        }
    }
}
