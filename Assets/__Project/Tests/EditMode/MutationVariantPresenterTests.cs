using System;
using System.Collections.Generic;
using Core.Logging;
using Inventory.Core;
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
    public class MutationVariantPresenterTests
    {
        private sealed class SilentLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeRackView : IBlankRackView
        {
            public event Action<int, int> OnArtifactDroppedOnBlank;
            public event Action<int, int> OnFilledSocketClicked;
            public event Action<int> OnUnsealClicked;

            public void ShowBlanks(IReadOnlyList<BlankEntryViewData> blanks) { }

            public void RaiseUnsealClicked(int blankInstanceId)
            {
                OnUnsealClicked?.Invoke(blankInstanceId);
            }
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
            private readonly HashSet<string> _notInstallable = new HashSet<string>();

            public SwapRequestOutcome RequestOutcome { get; set; } = SwapRequestOutcome.Applied;
            public int SwapCalls { get; private set; }
            public string LastSlotId { get; private set; }
            public string LastPartId { get; private set; }

            public event Action<bool> SwapRequestResolved;

            public void Equip(string slotId, string partId)
            {
                _equipped[slotId] = partId;
            }

            public void MarkNotInstallable(string partId)
            {
                _notInstallable.Add(partId);
            }

            public SwapRequestOutcome RequestSwapPart(string slotId, string partId)
            {
                SwapCalls++;
                LastSlotId = slotId;
                LastPartId = partId;
                if (RequestOutcome == SwapRequestOutcome.Applied)
                {
                    _equipped[slotId] = partId;
                }

                return RequestOutcome;
            }

            public bool CanInstall(string partId)
            {
                return !_notInstallable.Contains(partId);
            }

            /// <summary>Resolves a pending body-plan confirmation (the coordinator's role).</summary>
            public void ResolvePending(bool installed)
            {
                if (installed && LastSlotId != null)
                {
                    _equipped[LastSlotId] = LastPartId;
                }

                SwapRequestResolved?.Invoke(installed);
            }

            public bool TryGetEquippedPartId(string slotId, out string partId)
            {
                return _equipped.TryGetValue(slotId, out partId);
            }
        }

        private sealed class FakePartCatalog : IMutationPartCatalog
        {
            private readonly List<MutationCandidatePart> _candidates = new List<MutationCandidatePart>();
            private readonly Dictionary<string, MutationPartCardData> _cards =
                new Dictionary<string, MutationPartCardData>();

            public IReadOnlyList<MutationCandidatePart> AllCandidates => _candidates;

            public void Add(string slot, string part, params (string traitId, float weight)[] traitAffinity)
            {
                Add(slot, part, 0, null, traitAffinity);
            }

            public void Add(
                string slot,
                string part,
                int rarityTier,
                MutationAbilityInfo[] abilities,
                params (string traitId, float weight)[] traitAffinity)
            {
                var traits = new Dictionary<string, float>();
                foreach (var (traitId, weight) in traitAffinity)
                {
                    traits[traitId] = weight;
                }

                _candidates.Add(new MutationCandidatePart(slot, part, part, rarityTier, traits));
                _cards[part] = new MutationPartCardData(part, null, rarityTier, abilities);
            }

            // A candidate the scoring can offer but with no card data - the content-gap
            // fallback path.
            public void AddCardless(string slot, string part, params (string traitId, float weight)[] traitAffinity)
            {
                var traits = new Dictionary<string, float>();
                foreach (var (traitId, weight) in traitAffinity)
                {
                    traits[traitId] = weight;
                }

                _candidates.Add(new MutationCandidatePart(slot, part, part, 0, traits));
            }

            // Card data for a part that is not offered as a candidate (e.g. the equipped
            // part the back face resolves).
            public void AddCardOnly(string part, params MutationAbilityInfo[] abilities)
            {
                _cards[part] = new MutationPartCardData(part, null, 0, abilities);
            }

            public bool TryGetIcon(string partId, out Sprite icon)
            {
                icon = null;
                return false;
            }

            public bool TryGetCardData(string partId, out MutationPartCardData cardData)
            {
                if (string.IsNullOrEmpty(partId))
                {
                    cardData = null;
                    return false;
                }

                return _cards.TryGetValue(partId, out cardData);
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

        private InventoryModel _inventory;
        private BlankRack _rack;
        private SocketingModel _socketing;
        private FakeArtifactTraitSource _traits;
        private FakePartCatalog _partCatalog;
        private FakeChoiceView _view;
        private FakeRackView _rackView;
        private FakeMutationCharacter _character;
        private MutationConfig _config;
        private MutationVariantPresenter _presenter;
        private BlankInstance _skull; // slot.head, reptile, 2 sockets

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
            _rack = new BlankRack(3);
            var blanks = new FakePartBlankDataSource()
                .Add("blank.skull", "slot.head", "reptile", 2)
                .Add("blank.arm", "slot.arm.left", "insect", 1);
            _socketing = new SocketingModel(_inventory, _rack, blanks);
            _traits = new FakeArtifactTraitSource()
                .Add("mace", 2, "stone", "heavy")
                .Add("stinger", 2, "sharp", "toxic")
                .Add("pebble", 0);
            _partCatalog = new FakePartCatalog();
            _view = new FakeChoiceView();
            _rackView = new FakeRackView();
            _character = new FakeMutationCharacter();
            _config = ScriptableObject.CreateInstance<MutationConfig>();

            _presenter = new MutationVariantPresenter(
                _socketing,
                _rack,
                blanks,
                _traits,
                new BlankVariantBuilder(
                    new EmergentFusionCalculator(),
                    TraitFusionRuleSet.Empty,
                    new FusionSettings(1, 1f, 0.25f, 0.5f)),
                _partCatalog,
                new FakeArchetypeCatalog(),
                _character,
                _config,
                _view,
                _rackView,
                new SilentLogger());
            _presenter.Initialize();

            _rack.TryAdd("blank.skull", out _skull);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void SocketBoth()
        {
            var mace = _inventory.Add("mace");
            var stinger = _inventory.Add("stinger");
            _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, stinger.InstanceId);
        }

        /// <summary>Fills the skull and confirms its unseal (the Track F medallion beat).</summary>
        private void SocketBothAndUnseal()
        {
            SocketBoth();
            _rackView.RaiseUnsealClicked(_skull.InstanceId);
        }

        [Test]
        public void FillingTheLastSocket_DoesNotAutoShow_TheConfirmIsTheTrigger()
        {
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));

            SocketBoth();

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void UnsealClick_OnPartiallyFilledBlank_IsRejected()
        {
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            var mace = _inventory.Add("mace");
            _socketing.TrySocket(_skull.InstanceId, mace.InstanceId);

            _rackView.RaiseUnsealClicked(_skull.InstanceId);

            Assert.AreEqual(0, _view.ShowChoicesCalls);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void UnsealClick_OnReadyBlank_ShowsVariantCards()
        {
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f), ("toxic", 0.5f));
            _partCatalog.Add("slot.head", "part.head.club", ("heavy", 0.9f));
            _partCatalog.Add("slot.tail", "part.tail.x", ("sharp", 1f)); // other slot - filtered

            SocketBothAndUnseal();

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            Assert.AreEqual(2, _view.LastShown.Count);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void Selection_SwapsConsumesReagentsAndRemovesBlank()
        {
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            SocketBothAndUnseal();

            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
            Assert.AreEqual("slot.head", _character.LastSlotId);
            Assert.AreEqual("part.head.fang", _character.LastPartId);
            // Commit-on-unseal: reagents are consumed (not returned), the blank is spent.
            Assert.AreEqual(0, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(0, _inventory.Items.Count);
            Assert.AreEqual(0, _rack.Blanks.Count);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void Selection_SwapFails_KeepsCardsBlankAndReagents()
        {
            _character.RequestOutcome = SwapRequestOutcome.Rejected;
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            SocketBothAndUnseal();

            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
            Assert.AreEqual(2, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(1, _rack.Blanks.Count);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void PendingConfirmation_ConfirmCommitsTheUnseal()
        {
            _character.RequestOutcome = SwapRequestOutcome.PendingConfirmation;
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            SocketBothAndUnseal();

            _view.RaiseSelected(0);

            // The confirm dialog is up: the blank and reagents are untouched, cards stay.
            Assert.AreEqual(2, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(1, _rack.Blanks.Count);
            Assert.IsTrue(_view.Visible);

            _character.ResolvePending(true);

            // Confirmed: commit-on-unseal fires as in the synchronous path.
            Assert.AreEqual(0, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(0, _rack.Blanks.Count);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void PendingConfirmation_DeclineLeavesBlankAndCardsUntouched()
        {
            _character.RequestOutcome = SwapRequestOutcome.PendingConfirmation;
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            SocketBothAndUnseal();

            _view.RaiseSelected(0);
            _character.ResolvePending(false);

            // FR7 decline: body, blank, and sockets untouched; the cards stay up.
            Assert.AreEqual(2, _socketing.SocketedArtifacts(_skull.InstanceId).Count);
            Assert.AreEqual(1, _rack.Blanks.Count);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void PendingConfirmation_CardClicksBehindTheModalAreIgnored()
        {
            _character.RequestOutcome = SwapRequestOutcome.PendingConfirmation;
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            SocketBothAndUnseal();

            _view.RaiseSelected(0);
            _view.RaiseSelected(0);

            Assert.AreEqual(1, _character.SwapCalls);
        }

        [Test]
        public void NotInstallablePart_IsNotOffered()
        {
            // Body plans: a part whose bones have no home on the governing frame is
            // filtered out of the offers entirely.
            _character.MarkNotInstallable("part.head.fang");
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            _partCatalog.Add("slot.head", "part.head.club", ("heavy", 0.9f));

            SocketBothAndUnseal();

            Assert.AreEqual(1, _view.LastShown.Count);
            Assert.AreEqual("part.head.club", _view.LastShown[0].Front.PartName);
        }

        [Test]
        public void EquippedPart_IsNotOfferedAsVariant()
        {
            _character.Equip("slot.head", "part.head.fang");
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            _partCatalog.Add("slot.head", "part.head.club", ("heavy", 0.9f));

            SocketBothAndUnseal();

            Assert.AreEqual(1, _view.LastShown.Count);
            Assert.AreEqual("part.head.club", _view.LastShown[0].Front.PartName);
        }

        [Test]
        public void RawTraitlessReagents_StillYieldAMenu()
        {
            // "Raw-only is weak, never dead": zero-scoring parts are still offered.
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            var a = _inventory.Add("pebble");
            var b = _inventory.Add("pebble");

            _socketing.TrySocket(_skull.InstanceId, a.InstanceId);
            _socketing.TrySocket(_skull.InstanceId, b.InstanceId);
            _rackView.RaiseUnsealClicked(_skull.InstanceId);

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            Assert.AreEqual(1, _view.LastShown.Count);
        }

        [Test]
        public void UnsealClickWhileShowing_IsIgnored_SecondMedallionOpensAfterThePick()
        {
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            _partCatalog.Add("slot.arm.left", "part.arm.claw", ("sharp", 0.6f));
            _rack.TryAdd("blank.arm", out var arm);
            var stinger = _inventory.Add("stinger");
            _socketing.TrySocket(arm.InstanceId, stinger.InstanceId); // arm ready too

            SocketBothAndUnseal(); // skull cards up

            // A second confirm behind the open cards is ignored, not queued.
            _rackView.RaiseUnsealClicked(arm.InstanceId);
            Assert.AreEqual(1, _view.ShowChoicesCalls);

            _view.RaiseSelected(0); // resolve the skull
            Assert.IsFalse(_view.Visible);

            // The other medallion still offers its own confirm afterwards.
            _rackView.RaiseUnsealClicked(arm.InstanceId);
            Assert.AreEqual(2, _view.ShowChoicesCalls);
            Assert.AreEqual("part.arm.claw", _view.LastShown[0].Front.PartName);
            Assert.IsTrue(_view.Visible);
        }

        [Test]
        public void FrontFace_CarriesCardAbilitiesAndTier()
        {
            _partCatalog.Add("slot.head", "part.head.fang", rarityTier: 3,
                abilities: new[]
                {
                    new MutationAbilityInfo("Bite", "A venomous bite.", null, isPassive: false),
                    new MutationAbilityInfo("Scales", "Thick hide.", null, isPassive: true)
                },
                ("sharp", 0.7f));

            SocketBothAndUnseal();

            var card = _view.LastShown[0];
            Assert.AreEqual("slot.head", card.SlotId);
            Assert.AreEqual("part.head.fang", card.PartId);
            Assert.AreEqual(3, card.RarityTier);
            Assert.AreEqual("part.head.fang", card.Front.PartName);
            Assert.AreEqual(2, card.Front.Abilities.Count);
            Assert.AreEqual("Bite", card.Front.Abilities[0].Name);
            Assert.AreEqual("A venomous bite.", card.Front.Abilities[0].Description);
            Assert.IsFalse(card.Front.Abilities[0].IsPassive);
            Assert.AreEqual("Scales", card.Front.Abilities[1].Name);
            Assert.IsTrue(card.Front.Abilities[1].IsPassive);
        }

        [Test]
        public void OccupiedSlot_BackFaceCarriesReplacedPart()
        {
            _character.Equip("slot.head", "part.head.old");
            _partCatalog.AddCardOnly("part.head.old",
                new MutationAbilityInfo("Headbutt", "A dull blow.", null, isPassive: false));
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));

            SocketBothAndUnseal();

            var card = _view.LastShown[0];
            Assert.IsTrue(card.HasReplacedPart);
            Assert.AreEqual("part.head.old", card.Back.PartName);
            Assert.AreEqual(1, card.Back.Abilities.Count);
            Assert.AreEqual("Headbutt", card.Back.Abilities[0].Name);
        }

        [Test]
        public void EmptySlot_ReadsAsNothingReplaced()
        {
            // Nothing equipped in slot.head; the rig-not-assembled case takes the same
            // path (TryGetEquippedPartId returns false for both).
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));

            SocketBothAndUnseal();

            Assert.IsFalse(_view.LastShown[0].HasReplacedPart);
        }

        [Test]
        public void ReplacedPartWithoutCardData_ReadsAsNothingReplaced()
        {
            _character.Equip("slot.head", "part.head.unknown"); // no card data authored
            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));

            SocketBothAndUnseal();

            Assert.IsFalse(_view.LastShown[0].HasReplacedPart);
        }

        [Test]
        public void OfferedPartWithoutCardData_FallsBackToOptionLabel()
        {
            _partCatalog.AddCardless("slot.head", "part.head.raw", ("sharp", 0.7f));

            SocketBothAndUnseal();

            var card = _view.LastShown[0];
            Assert.AreEqual("part.head.raw", card.Front.PartName);
            Assert.AreEqual(0, card.Front.Abilities.Count);
            Assert.AreEqual(0, card.RarityTier);
        }

        [Test]
        public void StingyCauldronCut_NarrowsTheOffer_FlooredAtOne()
        {
            // Track Y "the cauldron skimps": the cut narrows the variant menu but can never
            // empty it — an absurd cut still leaves one take-it-or-leave-it form.
            _presenter.Dispose();
            var blanks = new FakePartBlankDataSource().Add("blank.skull", "slot.head", "reptile", 2);
            _presenter = new MutationVariantPresenter(
                _socketing, _rack, blanks, _traits,
                new BlankVariantBuilder(
                    new EmergentFusionCalculator(),
                    TraitFusionRuleSet.Empty,
                    new FusionSettings(1, 1f, 0.25f, 0.5f)),
                _partCatalog, new FakeArchetypeCatalog(), _character, _config, _view, _rackView,
                new SilentLogger(),
                rules: new MutationRuleModifiers(variantOptionCut: 99, socketCut: 0));
            _presenter.Initialize();

            _partCatalog.Add("slot.head", "part.head.fang", ("sharp", 0.7f));
            _partCatalog.Add("slot.head", "part.head.club", ("heavy", 0.9f));
            _partCatalog.Add("slot.head", "part.head.horn", ("stone", 0.5f));

            SocketBothAndUnseal();

            Assert.AreEqual(1, _view.ShowChoicesCalls);
            Assert.AreEqual(1, _view.LastShown.Count, "floor 1: narrowed, never empty");
        }
    }
}
