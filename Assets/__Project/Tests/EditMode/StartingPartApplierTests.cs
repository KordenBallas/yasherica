using System;
using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Integration;
using CharacterSystem.Runtime;
using Core.Persistence;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 starting-part install: the pure decision (fresh run + a chosen part only) and the pure
    /// apply (resolve via the catalog, one SwapPart; missing/failing parts degrade to a bare
    /// launch). The assembled-hero timing itself is the HeroBodyRestorer discipline, exercised in
    /// play mode.
    /// </summary>
    [TestFixture]
    public class StartingPartApplierTests
    {
        private sealed class FakePartCatalog : IPartCatalog
        {
            private readonly Dictionary<string, PartDefinition> _parts =
                new Dictionary<string, PartDefinition>();

            public FakePartCatalog With(string partId, PartDefinition part)
            {
                _parts[partId] = part;
                return this;
            }

            public IReadOnlyList<PartDefinition> All => new List<PartDefinition>(_parts.Values);

            public bool TryGet(string partId, out PartDefinition definition) =>
                _parts.TryGetValue(partId ?? string.Empty, out definition);
        }

#pragma warning disable 67 // PartsChanged is part of the interface; the fake never raises it.
        private sealed class RecordingCharacter : IModularCharacter
        {
            public readonly List<PartDefinition> Swapped = new List<PartDefinition>();
            public bool SwapSucceeds = true;

            public bool SwapPart(string slotId, string partId) => SwapSucceeds;

            public bool SwapPart(PartDefinition part)
            {
                Swapped.Add(part);
                return SwapSucceeds;
            }

            public AttachmentHandle AttachToSocket(string socketId, GameObject prefab) => null;
            public AttachmentHandle AttachToSocket(AttachmentDefinition attachment) => null;
            public bool DetachFromSocket(AttachmentHandle handle) => false;
            public bool DetachFromSocket(string socketId) => false;
            public IReadOnlyDictionary<string, string> EquippedParts { get; } =
                new Dictionary<string, string>();
            public IReadOnlyCollection<PartDefinition> EquippedPartDefinitions { get; } =
                new List<PartDefinition>();
            public IReadOnlyCollection<PartDefinition> DormantParts { get; } = new List<PartDefinition>();
            public string SkeletonId => "skeleton_base";
            public bool EquipDormant(PartDefinition part) => false;
            public event Action PartsChanged;
            public IReadOnlyList<SocketInfo> GetAvailableSockets() => new List<SocketInfo>();
            public Transform GetSocketTransform(string socketId) => null;
        }
#pragma warning restore 67

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private PartDefinition NewPart()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            _created.Add(part);
            return part;
        }

        // ---- ShouldApply -------------------------------------------------------------------

        [Test]
        public void ShouldApply_FreshRunWithChosenPart_True()
        {
            Assert.IsTrue(StartingPartApplier.ShouldApply(
                new RunRestoreContext(null), new RunStartConditions("part_x", "")));
        }

        [Test]
        public void ShouldApply_RestoringRun_False()
        {
            // The restored body snapshot already carries the installed part.
            Assert.IsFalse(StartingPartApplier.ShouldApply(
                new RunRestoreContext(new RunSaveSnapshot()), new RunStartConditions("part_x", "")));
        }

        [Test]
        public void ShouldApply_BareLaunch_False()
        {
            Assert.IsFalse(StartingPartApplier.ShouldApply(
                new RunRestoreContext(null), new RunStartConditions(string.Empty, "Forest")));
            Assert.IsFalse(StartingPartApplier.ShouldApply(new RunRestoreContext(null), null));
        }

        // ---- Apply ---------------------------------------------------------------------------

        [Test]
        public void Apply_ResolvesAndSwapsExactlyOnce()
        {
            var part = NewPart();
            var catalog = new FakePartCatalog().With("part_x", part);
            var character = new RecordingCharacter();

            StartingPartApplier.Apply("part_x", catalog, character);

            Assert.AreEqual(1, character.Swapped.Count);
            Assert.AreSame(part, character.Swapped[0]);
        }

        [Test]
        public void Apply_UnknownPartId_NoOp()
        {
            var character = new RecordingCharacter();

            StartingPartApplier.Apply("part_gone", new FakePartCatalog(), character);

            Assert.IsEmpty(character.Swapped, "a missing part must degrade to a bare launch");
        }

        [Test]
        public void Apply_EmptyIdOrNulls_NoOp()
        {
            var character = new RecordingCharacter();
            StartingPartApplier.Apply(string.Empty, new FakePartCatalog(), character);
            StartingPartApplier.Apply("part_x", null, character);
            StartingPartApplier.Apply("part_x", new FakePartCatalog(), null);

            Assert.IsEmpty(character.Swapped);
        }

        [Test]
        public void Apply_FailedSwap_DoesNotThrow()
        {
            var catalog = new FakePartCatalog().With("part_x", NewPart());
            var character = new RecordingCharacter { SwapSucceeds = false };

            Assert.DoesNotThrow(() => StartingPartApplier.Apply("part_x", catalog, character));
        }
    }
}
