using System.Collections.Generic;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class SocketAdjustedBlankSourceTests
    {
        private sealed class StubSource : IPartBlankDataSource
        {
            private readonly List<PartBlankData> _blanks;

            public StubSource(params PartBlankData[] blanks)
            {
                _blanks = new List<PartBlankData>(blanks);
            }

            public IReadOnlyList<PartBlankData> All => _blanks;

            public bool TryGet(string definitionId, out PartBlankData blank)
            {
                foreach (var candidate in _blanks)
                {
                    if (candidate.DefinitionId == definitionId)
                    {
                        blank = candidate;
                        return true;
                    }
                }

                blank = null;
                return false;
            }
        }

        private static PartBlankData Blank(string id, int sockets) =>
            new PartBlankData(id, id, "slot", "arch", sockets);

        [Test]
        public void CutZero_PassesTheInnerRecordsThrough()
        {
            var inner = new StubSource(Blank("b1", 3));
            var source = new SocketAdjustedBlankSource(inner, MutationRuleModifiers.Neutral);

            Assert.IsTrue(source.TryGet("b1", out var blank));
            Assert.AreSame(inner.All[0], blank, "Heat 0 zero-diff: the very same record");
            Assert.AreSame(inner.All, source.All);
        }

        [Test]
        public void Cut_ReducesEveryConsumerView_FlooredAtOne()
        {
            var source = new SocketAdjustedBlankSource(
                new StubSource(Blank("b3", 3), Blank("b1", 1)),
                new MutationRuleModifiers(variantOptionCut: 0, socketCut: 1));

            Assert.IsTrue(source.TryGet("b3", out var reduced));
            Assert.AreEqual(2, reduced.SocketCount);
            Assert.IsTrue(source.TryGet("b1", out var floored));
            Assert.AreEqual(1, floored.SocketCount, "never below one socket");
            Assert.AreEqual(2, source.All[0].SocketCount);
            Assert.AreEqual(1, source.All[1].SocketCount);
        }

        [Test]
        public void AdjustedRecord_KeepsEverythingElse()
        {
            var original = new PartBlankData("b", "Blank B", "slot.arm", "spider", 3, "fox");
            var source = new SocketAdjustedBlankSource(
                new StubSource(original), new MutationRuleModifiers(0, 1));

            source.TryGet("b", out var adjusted);

            Assert.AreEqual(original.DisplayName, adjusted.DisplayName);
            Assert.AreEqual(original.SlotId, adjusted.SlotId);
            Assert.AreEqual(original.SpeciesArchetypeId, adjusted.SpeciesArchetypeId);
            Assert.AreEqual(original.RaceId, adjusted.RaceId);
            Assert.AreSame(original.Gate, adjusted.Gate);
        }

        [Test]
        public void Misses_ForwardAsMisses()
        {
            var source = new SocketAdjustedBlankSource(new StubSource(), new MutationRuleModifiers(0, 1));

            Assert.IsFalse(source.TryGet("missing", out var blank));
            Assert.IsNull(blank);
        }
    }
}
