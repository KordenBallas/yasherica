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
    /// X2 end-to-end over the loopback bus: a forged envelope is substituted with a pass (loud
    /// error, the match continues, hashes converge), honest commits — the host's own included —
    /// flow through untouched, the schedule→volley credit dance works, and switching validation
    /// off restores the trusting MVP relay.
    /// </summary>
    [TestFixture]
    public class ArenaCommitValidationFlowTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int AbilityId = 100;
        private const int MaxHp = 30;
        private const int MatchSeed = 777;
        private const int MaxQueueSize = 3;

        private HexDirectionConfig _config;

        private sealed class ClientSim
        {
            public ArenaCombatController Controller;
            public ArenaMatchHost MatchHost;
            public Dictionary<int, IPlayer> Players;
        }

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

        private static IAbilityInstance LineAbility() =>
            new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 2, AbilityShapeData.ForLine(2), 10));

        private static List<ArenaRosterSlot> Roster() => new List<ArenaRosterSlot>
        {
            new ArenaRosterSlot(0, 1, 1),
            new ArenaRosterSlot(7, 2, 2)
        };

        private ClientSim CreateClient(
            LoopbackArenaTransport transport, int localPlayerId, bool isHost, bool validate)
        {
            var logger = new FakeLogger();
            var shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, shapeCalculator, _config);
            var builder = new ArenaCommitBuilder(shapeCalculator);
            var ledger = new ArenaSeatLedger();
            var matchHost = new ArenaMatchHost(new ArenaCommitCollector(), ledger, transport, logger);

            var controller = new ArenaCombatController(
                new ActionValidator(new CombatConfig(2f, HexOrientation.Flat, MaxQueueSize)),
                new ActionExecutor(abilityExecutor, logger),
                new TurnManager(logger),
                new RoundLifecycleProcessor(trigger),
                new EnemyIntentResolver(abilityExecutor, logger),
                builder,
                new RotatingInitiativeOrder(),
                new LastHeroStandingWinCondition(),
                transport,
                matchHost,
                null,
                _config,
                logger);
            controller.SetHostRole(isHost);

            var players = new Dictionary<int, IPlayer>();
            var seatList = new List<IPlayer>();
            for (int playerId = 1; playerId <= 2; playerId++)
            {
                IPlayer player = playerId == localPlayerId
                    ? new HumanPlayer(playerId, $"P{playerId}")
                    : (IPlayer)new NetworkPlayer(playerId, $"P{playerId}", (ulong)playerId);
                players[playerId] = player;
                seatList.Add(player);
            }

            controller.Initialize(
                new CombatState(new List<IUnit>(), seatList, seatList[0], 1, CombatPhase.Combat),
                seatList);
            controller.AddUnit(new Unit(1, players[1], new HexCoordinates(0, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.E));
            controller.AddUnit(new Unit(2, players[2], new HexCoordinates(4, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.W));
            controller.ArmWinCondition();

            if (isHost)
            {
                matchHost.SeedSeats(MatchSeed, Roster(), graceRounds: 3);
                if (validate)
                {
                    matchHost.EnableValidation(
                        new ArenaCommitValidator(builder), new ArenaQueueCreditLedger(),
                        controller, MaxQueueSize);
                }
            }

            return new ClientSim { Controller = controller, MatchHost = matchHost, Players = players };
        }

        private static void PumpResolve(params ClientSim[] sims)
        {
            foreach (var sim in sims)
            {
                Assert.AreEqual(RoundPhase.EnemyResolve, sim.Controller.CombatState.RoundPhase);
                while (sim.Controller.ResolveNextEnemyIntent())
                {
                }
            }
        }

        /// <summary>A teleporting move envelope (forged origin) for player 2, round 1.</summary>
        private static ArenaCommitEnvelope ForgedTeleport(IPlayer owner)
        {
            var commit = new ArenaCommit(2, 2, HexDirection.W, new List<EnemyIntent>
            {
                new EnemyIntent(2, new MoveAction(owner, 2, new HexCoordinates(1, 0)),
                    null, new HexCoordinates(0, 0), null)
            });
            return new ArenaCommitEnvelope(1, commit, 0);
        }

        [Test]
        public void ForgedEnvelope_SubstitutedWithPass_MatchContinues_HashesConverge()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, validate: true);
            var peer = CreateClient(transport, 2, isHost: false, validate: false);

            ArenaRoundBundle bundle = null;
            int rejectedPlayer = 0;
            transport.BundleReceived += b => bundle = b;
            host.MatchHost.CommitRejected += (playerId, reason) => rejectedPlayer = playerId;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            // The cheat: a hand-crafted teleport envelope straight onto the wire.
            transport.SubmitCommit(ForgedTeleport(peer.Players[2]));
            Assert.AreEqual(2, rejectedPlayer, "the forged commit was flagged");

            host.Controller.ProcessAction(new EndUnitTurnAction(host.Players[1], 1));

            Assert.IsNotNull(bundle, "the round completed without the cheater's action");
            CollectionAssert.AreEqual(new[] { 2 }, bundle.AutoPassedPlayerIds.ToArray(),
                "the invalid commit became a pass");
            Assert.IsFalse(bundle.Commits.Any(c => c.PlayerId == 2));

            PumpResolve(host, peer);
            Assert.AreEqual(new HexCoordinates(4, 0), host.Controller.CombatState.GetUnit(2).Position,
                "the teleport never happened");
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash);
        }

        [Test]
        public void HonestCommits_HostOwnIncluded_FlowThroughValidation()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, validate: true);
            var peer = CreateClient(transport, 2, isHost: false, validate: false);

            ArenaRoundBundle bundle = null;
            transport.BundleReceived += b => bundle = b;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            host.Controller.ProcessAction(new MoveAction(host.Players[1], 1, new HexCoordinates(1, 0)));
            peer.Controller.ProcessAction(new MoveAction(peer.Players[2], 2, new HexCoordinates(3, 0)));

            Assert.IsNotNull(bundle);
            Assert.AreEqual(2, bundle.Commits.Count, "both honest commits entered the bundle");
            CollectionAssert.IsEmpty(bundle.AutoPassedPlayerIds);

            PumpResolve(host, peer);
            Assert.AreEqual(new HexCoordinates(1, 0), peer.Controller.CombatState.GetUnit(1).Position);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash);
        }

        [Test]
        public void ScheduleThenVolley_BanksAndSpendsTheCredit()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, validate: true);
            var peer = CreateClient(transport, 2, isHost: false, validate: false);

            ArenaRoundBundle bundle = null;
            transport.BundleReceived += b => bundle = b;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            // Round 1: the host schedules (empty commit banks the credit); the peer sidesteps.
            host.Controller.ProcessAction(new ScheduleAbilityAction(host.Players[1], 1, AbilityId));
            peer.Controller.ProcessAction(new MoveAction(peer.Players[2], 2, new HexCoordinates(3, 0)));
            PumpResolve(host, peer);

            // Round 2: the banked volley fires and passes validation.
            host.Controller.ProcessAction(new ExecuteAbilityQueueAction(host.Players[1], 1));
            peer.Controller.ProcessAction(new EndUnitTurnAction(peer.Players[2], 2));

            Assert.IsTrue(bundle.Commits.Any(c => c.PlayerId == 1 && c.Steps.Any(s => s.IsAbility)),
                "the legitimately banked volley entered the bundle");
            CollectionAssert.IsEmpty(bundle.AutoPassedPlayerIds);

            PumpResolve(host, peer);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash);
        }

        [Test]
        public void ValidationOff_RelaysTheForgedCommitUnchecked()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true, validate: false);
            var peer = CreateClient(transport, 2, isHost: false, validate: false);

            ArenaRoundBundle bundle = null;
            transport.BundleReceived += b => bundle = b;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            transport.SubmitCommit(ForgedTeleport(peer.Players[2]));
            host.Controller.ProcessAction(new EndUnitTurnAction(host.Players[1], 1));

            Assert.IsNotNull(bundle);
            Assert.IsTrue(bundle.Commits.Any(c => c.PlayerId == 2),
                "the MVP relay trusts peers when the gate is off");
        }
    }
}
