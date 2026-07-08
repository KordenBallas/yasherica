using System.Collections.Generic;
using System.Linq;
using Combat.Arena;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Networking;
using Combat.Player;
using Combat.TurnManagement;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// P4-3b per-step simultaneous damage batching (config-gated, default off): a mutual lethal
    /// exchange kills both (a real draw), the win check waits for the batch boundary. With
    /// batching off the shipped sequential skip-dead behavior is unchanged. Two independent
    /// client sims share one transport (the lockstep pattern), each configured the same way.
    /// </summary>
    [TestFixture]
    public class ArenaDamageBatchingTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int AbilityId = 100;
        private const int HeavyDamage = 30;
        private const int MaxHp = 30;

        private HexDirectionConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        // A one-cell line that one-shots the adjacent enemy in the facing direction.
        private static IAbilityInstance OneShotLine() =>
            new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Executioner", 2, AbilityShapeData.ForLine(1), HeavyDamage));

        private sealed class ClientSim
        {
            public ArenaCombatController Controller;
            public IPlayer LocalPlayer;
            public int LocalUnitId;
            public bool Ended;
            public IPlayer Winner;
            public CombatPhase EndPhase;
        }

        private ClientSim CreateClient(
            LoopbackArenaTransport transport, int localPlayerId, bool isHost, bool batching)
        {
            var logger = new FakeLogger();
            var shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, shapeCalculator, _config);
            var batchEligibility = new ArenaBatchEligibility();

            var controller = new ArenaCombatController(
                new ActionValidator(new CombatConfig(2f, HexOrientation.Flat, 3)),
                new ActionExecutor(abilityExecutor, logger),
                new TurnManager(logger),
                new RoundLifecycleProcessor(trigger),
                new EnemyIntentResolver(abilityExecutor, logger, batchEligibility),
                new ArenaCommitBuilder(shapeCalculator),
                new RotatingInitiativeOrder(),
                new LastHeroStandingWinCondition(),
                transport,
                new ArenaMatchHost(new ArenaCommitCollector(), new ArenaSeatLedger(), transport, logger),
                null,
                _config,
                logger);
            controller.SetHostRole(isHost);

            var players = new List<IPlayer>();
            IPlayer localPlayer = null;
            for (int playerId = 1; playerId <= 2; playerId++)
            {
                IPlayer player = playerId == localPlayerId
                    ? new HumanPlayer(playerId, $"P{playerId}")
                    : (IPlayer)new NetworkPlayer(playerId, $"P{playerId}", (ulong)playerId);
                players.Add(player);
                if (playerId == localPlayerId)
                    localPlayer = player;
            }

            controller.Initialize(
                new CombatState(new List<IUnit>(), players, players[0], 1, CombatPhase.Combat),
                players);

            // Adjacent, facing each other — each one-shots the other.
            controller.AddUnit(new Unit(1, players[0], new HexCoordinates(0, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { OneShotLine() }, facingDirection: HexDirection.E));
            controller.AddUnit(new Unit(2, players[1], new HexCoordinates(1, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { OneShotLine() }, facingDirection: HexDirection.W));
            controller.ArmWinCondition();

            if (batching)
            {
                controller.EnableStepBatching(batchEligibility);
            }

            var sim = new ClientSim
            {
                Controller = controller,
                LocalPlayer = localPlayer,
                LocalUnitId = localPlayerId
            };
            controller.OnGameEnded += (winner, phase) =>
            {
                sim.Ended = true;
                sim.Winner = winner;
                sim.EndPhase = phase;
            };
            return sim;
        }

        private static void PumpResolve(params ClientSim[] sims)
        {
            foreach (var sim in sims)
            {
                if (sim.Controller.CombatState.RoundPhase == RoundPhase.EnemyResolve)
                {
                    while (sim.Controller.ResolveNextEnemyIntent())
                    {
                    }
                }
            }
        }

        /// <summary>Both seats schedule (round 1) then execute their one-shot volley (round 2).</summary>
        private static void RunMutualVolley(ClientSim host, ClientSim peer)
        {
            host.Controller.ProcessAction(new ScheduleAbilityAction(host.LocalPlayer, host.LocalUnitId, AbilityId));
            peer.Controller.ProcessAction(new ScheduleAbilityAction(peer.LocalPlayer, peer.LocalUnitId, AbilityId));
            PumpResolve(host, peer);

            host.Controller.ProcessAction(new ExecuteAbilityQueueAction(host.LocalPlayer, host.LocalUnitId));
            peer.Controller.ProcessAction(new ExecuteAbilityQueueAction(peer.LocalPlayer, peer.LocalUnitId));
            PumpResolve(host, peer);
        }

        [Test]
        public void MutualLethal_WithBatching_BothDie_MatchIsDraw()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, batching: true);
            var peer = CreateClient(transport, 2, isHost: false, batching: true);
            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            RunMutualVolley(host, peer);

            Assert.IsFalse(host.Controller.CombatState.GetUnit(1).IsAlive, "unit 1 died");
            Assert.IsFalse(host.Controller.CombatState.GetUnit(2).IsAlive,
                "the batch resolves both blows before the win check — unit 2 died too");
            Assert.IsTrue(host.Ended);
            Assert.IsNull(host.Winner, "a mutual kill is a draw");
            Assert.AreEqual(CombatPhase.Defeat, host.EndPhase);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash,
                "both sims batched identically");
        }

        [Test]
        public void MutualLethal_WithoutBatching_EarlierInitiativeSurvives()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, batching: false);
            var peer = CreateClient(transport, 2, isHost: false, batching: false);
            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            RunMutualVolley(host, peer);

            // Sequential skip-dead: the earlier in initiative kills the later before it can fire.
            int alive = new[] { 1, 2 }.Count(id => host.Controller.CombatState.GetUnit(id).IsAlive);
            Assert.AreEqual(1, alive, "exactly one survivor under the shipped sequential rule");
            Assert.IsTrue(host.Ended);
            Assert.IsNotNull(host.Winner, "the survivor wins — not a draw");
        }

        // ---- the batcher + eligibility units ----

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

        [Test]
        public void Batcher_GroupsKthStepsAcrossCommits_WithBoundaries()
        {
            var bundle = new ArenaRoundBundle(1, new List<ArenaCommit>
            {
                Commit(1, 2), // two steps
                Commit(2, 1), // one step
                Commit(3, 2)  // two steps
            });

            var batched = ArenaStepBatcher.Batch(bundle, new RotatingInitiativeOrder());

            // Batch 0: all three units' step 0 (3 intents); batch 1: only 1 & 3 (2 intents).
            Assert.AreEqual(new[] { 3, 5 }, batched.BatchEndIndices.ToArray());
            Assert.AreEqual(5, batched.Intents.Count);
            // Every unit's own steps keep their order (unit 1: Q0 then Q1).
            var unit1 = batched.Intents.Where(i => i.UnitId == 1)
                .Select(i => i.MoveDestination.Value.Q).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1 }, unit1);
        }

        [Test]
        public void BatchEligibility_OutsideBatchMode_MirrorsDefault()
        {
            var eligibility = new ArenaBatchEligibility();
            var alive = new Unit(1, new HumanPlayer(1, "P"), new HexCoordinates(0, 0), 10, 10,
                new List<IAbilityInstance>());
            var dead = new Unit(2, new HumanPlayer(2, "P"), new HexCoordinates(1, 0), 0, 10,
                new List<IAbilityInstance>());

            Assert.IsTrue(eligibility.CanAct(alive));
            Assert.IsFalse(eligibility.CanAct(dead),
                "with no batch active it behaves as the plain alive check");
        }

        [Test]
        public void BatchEligibility_InBatchMode_SnapshotSurvivesAMidBatchDeath()
        {
            var eligibility = new ArenaBatchEligibility();
            var players = new List<IPlayer> { new HumanPlayer(1, "P1"), new HumanPlayer(2, "P2") };
            var state = new CombatState(new List<IUnit>
            {
                new Unit(1, players[0], new HexCoordinates(0, 0), 10, 10, new List<IAbilityInstance>()),
                new Unit(2, players[1], new HexCoordinates(1, 0), 10, 10, new List<IAbilityInstance>())
            }, players, players[0]);

            eligibility.BeginBatchMode();
            eligibility.RefreshFrom(state);

            // Unit 1 dies mid-batch, but the snapshot still says it may act (its committed blow lands).
            var afterDeath = state.WithUpdatedUnit((state.GetUnit(1) as Unit).WithHP(0));
            Assert.IsTrue(eligibility.CanAct(afterDeath.GetUnit(1)),
                "a unit killed mid-batch still fires its same-batch step");

            eligibility.EndBatchMode();
            Assert.IsFalse(eligibility.CanAct(afterDeath.GetUnit(1)),
                "outside the batch the plain alive check resumes");
        }
    }
}
