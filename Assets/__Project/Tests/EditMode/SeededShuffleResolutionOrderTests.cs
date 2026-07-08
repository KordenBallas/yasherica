using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// P4-3a: the seeded-shuffle resolution order must be identical across independent instances
    /// (that is the lockstep contract — every client shuffles alone and must agree) while
    /// actually varying between rounds, with step order inside a commit untouched.
    /// </summary>
    [TestFixture]
    public class SeededShuffleResolutionOrderTests
    {
        private const int MatchSeed = 987;

        private static ArenaMatchContext Context(int seed = MatchSeed)
        {
            var context = new ArenaMatchContext();
            context.SetSetup(seed, new List<ArenaRosterSlot>
            {
                new ArenaRosterSlot(0, 1, 1),
                new ArenaRosterSlot(7, 2, 2),
                new ArenaRosterSlot(9, 3, 3),
                new ArenaRosterSlot(11, 4, 4)
            });
            return context;
        }

        private static ArenaCommit Commit(int playerId, int steps)
        {
            var owner = new HumanPlayer(playerId, $"P{playerId}");
            var list = new List<EnemyIntent>();
            for (int i = 0; i < steps; i++)
            {
                list.Add(new EnemyIntent(playerId, new MoveAction(owner, playerId,
                    new HexCoordinates(i, playerId)), null, new HexCoordinates(0, playerId), null));
            }

            return new ArenaCommit(playerId, playerId, HexDirection.E, list);
        }

        private static ArenaRoundBundle Bundle(int round) => new ArenaRoundBundle(round,
            new List<ArenaCommit> { Commit(1, 2), Commit(2, 1), Commit(3, 1), Commit(4, 2) });

        [Test]
        public void SameSeedAndRound_TwoIndependentInstances_ProduceTheIdenticalOrder()
        {
            var a = new SeededShuffleResolutionOrder(Context());
            var b = new SeededShuffleResolutionOrder(Context());

            var orderA = a.Order(Bundle(3)).Select(i => i.UnitId).ToArray();
            var orderB = b.Order(Bundle(3)).Select(i => i.UnitId).ToArray();

            CollectionAssert.AreEqual(orderA, orderB,
                "every client shuffles alone and must agree — the lockstep contract");
        }

        [Test]
        public void DifferentRounds_ShuffleDifferently()
        {
            var order = new SeededShuffleResolutionOrder(Context());

            var perRound = Enumerable.Range(1, 6)
                .Select(round => string.Join(",", order.Order(Bundle(round)).Select(i => i.UnitId)))
                .Distinct()
                .Count();

            Assert.Greater(perRound, 1, "the per-round derivation must actually reshuffle");
        }

        [Test]
        public void DifferentSeeds_ShuffleDifferently()
        {
            var ordersA = Enumerable.Range(1, 4)
                .Select(r => string.Join(",",
                    new SeededShuffleResolutionOrder(Context(1)).Order(Bundle(r)).Select(i => i.UnitId)));
            var ordersB = Enumerable.Range(1, 4)
                .Select(r => string.Join(",",
                    new SeededShuffleResolutionOrder(Context(2)).Order(Bundle(r)).Select(i => i.UnitId)));

            CollectionAssert.AreNotEqual(ordersA.ToList(), ordersB.ToList(),
                "a different match rolls different orders");
        }

        [Test]
        public void StepsInsideACommit_KeepCommittedOrder()
        {
            var order = new SeededShuffleResolutionOrder(Context());

            var intents = order.Order(Bundle(2));

            // Player 1's two steps: destination R identifies the owner, Q the step index —
            // whatever the shuffle did, step 0 precedes step 1.
            var p1Steps = intents
                .Where(i => i.UnitId == 1)
                .Select(i => i.MoveDestination.Value.Q)
                .ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1 }, p1Steps);
        }

        [Test]
        public void EmptyBundle_YieldsNoIntents()
        {
            var order = new SeededShuffleResolutionOrder(Context());
            CollectionAssert.IsEmpty(order.Order(new ArenaRoundBundle(1, new List<ArenaCommit>())));
        }
    }
}
