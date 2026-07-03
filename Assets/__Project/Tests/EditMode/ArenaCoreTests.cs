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
    /// Pure-domain suites for the Arena round pieces: resolution order rotation (R1), commit
    /// gathering, the deterministic spawn planner (R6), the lockstep state hash (R10), and the
    /// last-hero-standing win/draw rule (R8, R14).
    /// </summary>
    [TestFixture]
    public class ArenaCoreTests
    {
        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private static ArenaCommit Commit(int playerId, int unitId, params EnemyIntent[] steps) =>
            new ArenaCommit(playerId, unitId, HexDirection.E, steps.ToList());

        private static EnemyIntent MoveIntent(int unitId, int q, int r)
        {
            var owner = new HumanPlayer(unitId, $"P{unitId}");
            var move = new MoveAction(owner, unitId, new HexCoordinates(q, r));
            return new EnemyIntent(unitId, move, null, new HexCoordinates(0, 0), null);
        }

        // ---- RotatingInitiativeOrder (R1) ----

        [Test]
        public void RotatingOrder_Round1_StartsWithLowestPlayerId()
        {
            var bundle = new ArenaRoundBundle(1, new[]
            {
                Commit(2, 2, MoveIntent(2, 0, 1)),
                Commit(1, 1, MoveIntent(1, 1, 0))
            });

            var intents = new RotatingInitiativeOrder().Order(bundle);

            Assert.AreEqual(new[] { 1, 2 }, intents.Select(i => i.UnitId).ToArray());
        }

        [Test]
        public void RotatingOrder_Round2_RotatesInitiativeToTheNextPlayer()
        {
            var bundle = new ArenaRoundBundle(2, new[]
            {
                Commit(1, 1, MoveIntent(1, 1, 0)),
                Commit(2, 2, MoveIntent(2, 0, 1))
            });

            var intents = new RotatingInitiativeOrder().Order(bundle);

            Assert.AreEqual(new[] { 2, 1 }, intents.Select(i => i.UnitId).ToArray());
        }

        [Test]
        public void RotatingOrder_CyclesBackAfterAllPlayersHeldInitiative()
        {
            var commits = new[]
            {
                Commit(1, 1, MoveIntent(1, 1, 0)),
                Commit(2, 2, MoveIntent(2, 0, 1)),
                Commit(3, 3, MoveIntent(3, 1, 1))
            };

            var round4 = new RotatingInitiativeOrder().Order(new ArenaRoundBundle(4, commits));

            Assert.AreEqual(new[] { 1, 2, 3 }, round4.Select(i => i.UnitId).ToArray(),
                "round 4 with 3 players wraps back to player 1");
        }

        [Test]
        public void RotatingOrder_KeepsAUnitsStepsInCommittedOrder()
        {
            var first = MoveIntent(1, 1, 0);
            var second = MoveIntent(1, 2, 0);
            var bundle = new ArenaRoundBundle(1, new[] { Commit(1, 1, first, second) });

            var intents = new RotatingInitiativeOrder().Order(bundle);

            Assert.AreSame(first, intents[0]);
            Assert.AreSame(second, intents[1]);
        }

        // ---- ArenaCommitCollector ----

        [Test]
        public void Collector_CompletesOnlyWhenEveryRequiredPlayerCommitted()
        {
            var collector = new ArenaCommitCollector();
            collector.BeginRound(1, new[] { 1, 2 });

            Assert.IsTrue(collector.TryAccept(1, Commit(1, 1)));
            Assert.IsFalse(collector.AllCommitted);

            Assert.IsTrue(collector.TryAccept(1, Commit(2, 2)));
            Assert.IsTrue(collector.AllCommitted);
        }

        [Test]
        public void Collector_IgnoresDuplicatesStaleRoundsAndUninvitedPlayers()
        {
            var collector = new ArenaCommitCollector();
            collector.BeginRound(3, new[] { 1, 2 });

            Assert.IsTrue(collector.TryAccept(3, Commit(1, 1)));
            Assert.IsFalse(collector.TryAccept(3, Commit(1, 1)), "duplicate");
            Assert.IsFalse(collector.TryAccept(2, Commit(2, 2)), "stale round");
            Assert.IsFalse(collector.TryAccept(3, Commit(99, 99)), "player not required this round");
        }

        [Test]
        public void Collector_RemovePlayer_CanCompleteTheRound()
        {
            var collector = new ArenaCommitCollector();
            collector.BeginRound(1, new[] { 1, 2 });
            collector.TryAccept(1, Commit(1, 1));

            collector.RemovePlayer(2);

            Assert.IsTrue(collector.AllCommitted, "departed player no longer owes a commit");
        }

        [Test]
        public void Collector_CanonicalOrder_IsAscendingPlayerId()
        {
            var collector = new ArenaCommitCollector();
            collector.BeginRound(1, new[] { 1, 2, 3 });
            collector.TryAccept(1, Commit(3, 3));
            collector.TryAccept(1, Commit(1, 1));
            collector.TryAccept(1, Commit(2, 2));

            Assert.AreEqual(new[] { 1, 2, 3 },
                collector.CommitsInCanonicalOrder().Select(c => c.PlayerId).ToArray());
        }

        // ---- ArenaSpawnPlanner (R6) ----

        [Test]
        public void SpawnPlanner_IsDeterministic_AndCellsAreDistinct()
        {
            var cells = new List<HexCoordinates>();
            for (int q = -3; q <= 3; q++)
            for (int r = -3; r <= 3; r++)
                cells.Add(new HexCoordinates(q, r));

            var planner = new ArenaSpawnPlanner();
            var first = planner.Plan(cells, new HexCoordinates(0, 0), 4);
            var second = planner.Plan(cells, new HexCoordinates(0, 0), 4);

            CollectionAssert.AreEqual(first, second, "same surface → same spawns");
            Assert.AreEqual(4, first.Distinct().Count(), "all spawn cells distinct");
        }

        [Test]
        public void SpawnPlanner_SpreadsSpawnsApart()
        {
            var cells = new List<HexCoordinates>();
            for (int q = -3; q <= 3; q++)
            for (int r = -3; r <= 3; r++)
                cells.Add(new HexCoordinates(q, r));

            var spawns = new ArenaSpawnPlanner().Plan(cells, new HexCoordinates(0, 0), 2);

            int dq = System.Math.Abs(spawns[0].Q - spawns[1].Q);
            int dr = System.Math.Abs(spawns[0].R - spawns[1].R);
            int ds = System.Math.Abs((spawns[0].Q + spawns[0].R) - (spawns[1].Q + spawns[1].R));
            Assert.GreaterOrEqual((dq + dr + ds) / 2, 4, "two players spawn far apart");
        }

        // ---- ArenaStateHash (R10) ----

        [Test]
        public void StateHash_IsStable_AndSensitiveToSimState()
        {
            var owner = new HumanPlayer(1, "P1");
            var unit = new Unit(1, owner, new HexCoordinates(0, 0), 30, 30, new List<IAbilityInstance>());
            var state = new CombatState(new List<IUnit> { unit }, new List<IPlayer> { owner }, owner,
                phase: CombatPhase.Combat);

            var baseline = ArenaStateHash.Compute(state);

            Assert.AreEqual(baseline, ArenaStateHash.Compute(state), "same state → same hash");

            var moved = state.WithUpdatedUnit(unit.WithPosition(new HexCoordinates(1, 0)));
            Assert.AreNotEqual(baseline, ArenaStateHash.Compute(moved), "position feeds the hash");

            var hurt = state.WithUpdatedUnit(unit.WithHP(10));
            Assert.AreNotEqual(baseline, ArenaStateHash.Compute(hurt), "HP feeds the hash");
        }

        // ---- LastHeroStandingWinCondition (R8 / R14) ----

        private static CombatState TwoPlayerState(int hp1, int hp2, out IPlayer p1, out IPlayer p2)
        {
            p1 = new HumanPlayer(1, "P1");
            p2 = new AIPlayer(2, "P2", new NoopAI());
            var u1 = new Unit(1, p1, new HexCoordinates(0, 0), hp1, 30, new List<IAbilityInstance>());
            var u2 = new Unit(2, p2, new HexCoordinates(2, 0), hp2, 30, new List<IAbilityInstance>());
            return new CombatState(new List<IUnit> { u1, u2 }, new List<IPlayer> { p1, p2 }, p1,
                phase: CombatPhase.Combat);
        }

        [Test]
        public void WinCondition_DoesNotFire_BeforeArming()
        {
            var condition = new LastHeroStandingWinCondition();
            var state = TwoPlayerState(30, 0, out _, out _);

            Assert.IsFalse(condition.Check(state, out _), "unarmed — the roster is still spawning");
        }

        [Test]
        public void WinCondition_LastAlivePlayerWins()
        {
            var condition = new LastHeroStandingWinCondition();
            condition.Arm();
            var state = TwoPlayerState(30, 0, out var p1, out _);

            Assert.IsTrue(condition.Check(state, out var winner));
            Assert.AreSame(p1, winner);
        }

        [Test]
        public void WinCondition_NobodyAlive_IsADraw()
        {
            var condition = new LastHeroStandingWinCondition();
            condition.Arm();
            var state = TwoPlayerState(0, 0, out _, out _);

            Assert.IsTrue(condition.Check(state, out var winner), "match over");
            Assert.IsNull(winner, "draw reports a null winner");
        }

        [Test]
        public void WinCondition_TwoAlivePlayers_MatchGoesOn()
        {
            var condition = new LastHeroStandingWinCondition();
            condition.Arm();
            var state = TwoPlayerState(30, 30, out _, out _);

            Assert.IsFalse(condition.Check(state, out _));
        }
    }
}
