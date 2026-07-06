using System.Collections.Generic;
using System.Linq;
using Hub.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 starting-offer draw: deterministic per (pool, seed), independent of the caller's pool
    /// order, variety-greedy (unseen race → unseen slot → flavored beats plain), and graceful on
    /// small/empty pools.
    /// </summary>
    [TestFixture]
    public class StartingPartSelectorTests
    {
        private static StartingPartCandidate Part(
            string id, string race = "", string slot = "", bool ability = false) =>
            new StartingPartCandidate(id, race, slot, ability);

        private static List<string> Ids(IReadOnlyList<StartingPartCandidate> picks) =>
            picks.Select(p => p.PartId).ToList();

        private readonly StartingPartSelector _selector = new StartingPartSelector();

        [Test]
        public void SamePoolAndSeed_SameDrawInSameOrder()
        {
            var pool = new[]
            {
                Part("a", "fox", "slot_leg"), Part("b", "ibex", "slot_head"),
                Part("c", "lizard", "slot_torso"), Part("d"), Part("e", "", "slot_arm", ability: true)
            };

            var first = Ids(_selector.Draw(pool, 3, seed: 7));
            var second = Ids(_selector.Draw(pool, 3, seed: 7));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void PoolOrder_DoesNotChangeTheDraw()
        {
            var pool = new[]
            {
                Part("a", "fox", "slot_leg"), Part("b", "ibex", "slot_head"),
                Part("c", "lizard", "slot_torso"), Part("d"), Part("e", "", "slot_arm", ability: true)
            };
            var shuffled = new[] { pool[3], pool[0], pool[4], pool[2], pool[1] };

            CollectionAssert.AreEqual(
                Ids(_selector.Draw(pool, 3, seed: 5)),
                Ids(_selector.Draw(shuffled, 3, seed: 5)),
                "the draw must be a function of the pool CONTENT, not its order");
        }

        [Test]
        public void DifferentSeeds_EventuallyDiverge()
        {
            var pool = new[]
            {
                Part("a", "fox", "s1"), Part("b", "fox", "s2"), Part("c", "fox", "s3"),
                Part("d", "fox", "s4"), Part("e", "fox", "s5"), Part("f", "fox", "s6")
            };

            var draws = new HashSet<string>();
            for (int seed = 0; seed < 16; seed++)
            {
                draws.Add(string.Join(",", Ids(_selector.Draw(pool, 3, seed))));
            }

            Assert.Greater(draws.Count, 1, "the seed must actually vary the offer");
        }

        [Test]
        public void ThreeRacesInPool_OfferCoversThreeRaces()
        {
            var pool = new[]
            {
                Part("fox1", "fox", "s1"), Part("fox2", "fox", "s2"),
                Part("ibex1", "ibex", "s3"), Part("lizard1", "lizard", "s4"),
                Part("plain1"), Part("plain2")
            };

            for (int seed = 0; seed < 8; seed++)
            {
                var races = _selector.Draw(pool, 3, seed).Select(p => p.RaceId).ToList();
                CollectionAssert.AreEquivalent(new[] { "fox", "ibex", "lizard" }, races,
                    $"seed {seed}: three distinct races available must yield three distinct leans");
            }
        }

        [Test]
        public void RaceStarvedPool_PrefersSlotVariety()
        {
            var pool = new[]
            {
                Part("a", "fox", "slot_leg"), Part("b", "fox", "slot_leg"),
                Part("c", "fox", "slot_head"), Part("d", "fox", "slot_torso")
            };

            for (int seed = 0; seed < 8; seed++)
            {
                var slots = _selector.Draw(pool, 3, seed).Select(p => p.SlotId).Distinct().Count();
                Assert.AreEqual(3, slots, $"seed {seed}: three slots available must yield three slots");
            }
        }

        [Test]
        public void FlavoredCandidates_BeatPlainScrap()
        {
            // 3 flavored (race or ability) + 3 plain: the offer must be the flavored ones.
            var pool = new[]
            {
                Part("plain1"), Part("plain2"), Part("plain3"),
                Part("raced", "fox", "s1"), Part("abled", "", "s2", ability: true),
                Part("both", "ibex", "s3", ability: true)
            };

            for (int seed = 0; seed < 8; seed++)
            {
                var ids = Ids(_selector.Draw(pool, 3, seed));
                CollectionAssert.AreEquivalent(new[] { "raced", "abled", "both" }, ids,
                    $"seed {seed}: flavored candidates must outrank plain scrap");
            }
        }

        [Test]
        public void SmallPool_ReturnsWhatExists()
        {
            Assert.AreEqual(2, _selector.Draw(new[] { Part("a"), Part("b") }, 3, 1).Count);
            Assert.AreEqual(0, _selector.Draw(new StartingPartCandidate[0], 3, 1).Count);
            Assert.AreEqual(0, _selector.Draw(null, 3, 1).Count);
        }

        [Test]
        public void InvalidCandidates_AreDropped()
        {
            var pool = new[] { Part("a"), null, Part(""), Part("b") };
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, Ids(_selector.Draw(pool, 3, 1)));
        }
    }
}
