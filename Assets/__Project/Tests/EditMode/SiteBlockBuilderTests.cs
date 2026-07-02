using System.Collections.Generic;
using Narrative.Director.Core;
using NUnit.Framework;
using World.Sites.Core;

namespace Tests.EditMode
{
    [TestFixture]
    public class SiteBlockBuilderTests
    {
        private static SiteDefinitionData Site(
            string id = "city",
            int footprintMin = 4,
            int footprintMax = 5,
            ContentBeat[] anchors = null,
            int fillMin = 2,
            int fillMax = 3,
            WeightedBeat[] fill = null,
            string dressing = "city-kit")
        {
            return new SiteDefinitionData(id, "settlement", footprintMin, footprintMax, 1,
                anchors ?? new[] { new ContentBeat(ContentBaseKind.Npc, "quest-bearer") },
                fillMin, fillMax,
                fill ?? new[]
                {
                    new WeightedBeat(new ContentBeat(ContentBaseKind.Npc, "townsfolk"), 3),
                    new WeightedBeat(new ContentBeat(ContentBaseKind.Loot, "market"), 2)
                },
                dressing);
        }

        private static IReadOnlyList<SiteSlot> Build(SiteDefinitionData site, ulong seed = 7, int instanceId = 1) =>
            new SiteBlockBuilder().Build(site, new DeterministicRandom(seed), instanceId);

        [Test]
        public void FootprintWithinRange_AndAnchorEmittedFirst()
        {
            for (ulong seed = 0; seed < 20; seed++)
            {
                var block = Build(Site(), seed);

                Assert.That(block.Count, Is.InRange(4, 5), $"Footprint out of range at seed {seed}.");
                Assert.AreEqual(ContentBaseKind.Npc, block[0].Beat.Kind, $"Anchor not first at seed {seed}.");
                Assert.AreEqual("quest-bearer", block[0].Beat.Flavor);
            }
        }

        [Test]
        public void FillBudget_IsRespected_AndClampedToRemainingFootprint()
        {
            // Footprint fixed at 2 with 1 anchor: at most 1 fill even though the budget asks for 3.
            var site = Site(footprintMin: 2, footprintMax: 2, fillMin: 3, fillMax: 3);
            var block = Build(site);

            Assert.AreEqual(2, block.Count);
            Assert.AreNotEqual(ContentBaseKind.Empty, block[1].Beat.Kind, "The single fill slot should carry a beat.");
        }

        [Test]
        public void AnchorList_IsClampedToFootprint()
        {
            var site = Site(footprintMin: 1, footprintMax: 1, anchors: new[]
            {
                new ContentBeat(ContentBaseKind.Combat, "bandit"),
                new ContentBeat(ContentBaseKind.Loot, "stash")
            });

            var block = Build(site);

            Assert.AreEqual(1, block.Count);
            Assert.AreEqual(ContentBaseKind.Combat, block[0].Beat.Kind);
        }

        [Test]
        public void FootprintBeyondAnchorsAndFill_StaysEmptyConnective()
        {
            var site = Site(footprintMin: 5, footprintMax: 5, fillMin: 1, fillMax: 1);
            var block = Build(site);

            for (int i = 2; i < block.Count; i++)
            {
                Assert.AreEqual(ContentBaseKind.Empty, block[i].Beat.Kind, $"Slot {i} should be connective.");
            }
        }

        [Test]
        public void EmptyFillTable_ProducesAnchorOnlyBlock()
        {
            var site = Site(footprintMin: 3, footprintMax: 3, fillMin: 2, fillMax: 2,
                fill: new WeightedBeat[0]);
            var block = Build(site);

            Assert.AreEqual(ContentBaseKind.Empty, block[1].Beat.Kind);
            Assert.AreEqual(ContentBaseKind.Empty, block[2].Beat.Kind);
        }

        [Test]
        public void Stamps_AreSequential_AndShareInstanceAndFootprint()
        {
            var block = Build(Site(), instanceId: 42);

            for (int i = 0; i < block.Count; i++)
            {
                var stamp = block[i].Stamp;
                Assert.AreEqual("city", stamp.SiteId);
                Assert.AreEqual(42, stamp.InstanceId);
                Assert.AreEqual(i, stamp.Index);
                Assert.AreEqual(block.Count, stamp.Footprint);
                Assert.AreEqual("city-kit", stamp.DressingThemeId);
                Assert.IsFalse(stamp.IsWild);
            }
        }

        [Test]
        public void SameSeed_ProducesIdenticalBlock()
        {
            var a = Build(Site(), seed: 99);
            var b = Build(Site(), seed: 99);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Beat.Kind, b[i].Beat.Kind, $"Kind diverged at slot {i}.");
                Assert.AreEqual(a[i].Beat.Flavor, b[i].Beat.Flavor, $"Flavor diverged at slot {i}.");
            }
        }

        [Test]
        public void FillDraws_ComeFromTheWeightedTable()
        {
            var block = Build(Site(footprintMin: 5, footprintMax: 5, fillMin: 3, fillMax: 3));

            var allowed = new HashSet<string> { "townsfolk", "market" };
            for (int i = 1; i < 4; i++)
            {
                Assert.IsTrue(allowed.Contains(block[i].Beat.Flavor),
                    $"Fill slot {i} drew an unexpected beat '{block[i].Beat.Flavor}'.");
            }
        }
    }
}
