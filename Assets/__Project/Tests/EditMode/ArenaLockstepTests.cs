using System.Collections.Generic;
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
    /// Two independent client simulations (host + joiner) sharing one transport — the in-process
    /// equivalent of the networked session. Each client has its OWN controller, state, and seat
    /// objects (its own player is Human, the remote one is a NetworkPlayer); only commits and
    /// bundles cross the seam. Proves the lockstep contract end-to-end: both sims resolve the
    /// identical rounds and report identical state hashes (R10), and the joiner never assembles.
    /// </summary>
    [TestFixture]
    public class ArenaLockstepTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int AbilityId = 100;
        private const int BaseDamage = 10;
        private const int MaxHp = 30;

        private HexDirectionConfig _config;

        private sealed class ClientSim
        {
            public ArenaCombatController Controller;
            public IPlayer LocalPlayer;
            public IPlayer RemotePlayer;
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
                AbilityId, "Test Line", 2, AbilityShapeData.ForLine(2), BaseDamage));

        private const int StatusAbilityId = 101;

        // A hybrid blow that leaves a stacking poison — the status layer under lockstep.
        private static IAbilityInstance PoisonLineAbility() =>
            new AbilityInstance(new DataDrivenHybridAbility(
                StatusAbilityId, "Venom Line", 0, AbilityShapeData.ForLine(2), 5,
                new DataDrivenDamageOverTimeEffect(2, "Poison", 3, 1, StackRule.StackToCap, 3,
                    StatusEffectTriggerType.TurnEnd, 4, 4), 3));

        /// <summary>
        /// Builds one client's full simulation over the shared transport. localPlayerId owns the
        /// human seat on this machine; the other seat is a NetworkPlayer (like a real joiner).
        /// </summary>
        private ClientSim CreateClient(LoopbackArenaTransport transport, int localPlayerId, bool isHost)
        {
            var logger = new FakeLogger();
            var shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, shapeCalculator, _config);

            var controller = new ArenaCombatController(
                new ActionValidator(new CombatConfig(2f, HexOrientation.Flat, 3)),
                new ActionExecutor(abilityExecutor, logger),
                new TurnManager(logger),
                new RoundLifecycleProcessor(trigger),
                new EnemyIntentResolver(abilityExecutor, logger),
                new ArenaCommitBuilder(shapeCalculator),
                new RotatingInitiativeOrder(),
                new LastHeroStandingWinCondition(),
                transport,
                new ArenaMatchHost(new ArenaCommitCollector(), transport, logger),
                null,
                _config,
                logger);
            controller.SetHostRole(isHost);

            var sim = new ClientSim { Controller = controller };
            var players = new List<IPlayer>();
            for (int playerId = 1; playerId <= 2; playerId++)
            {
                IPlayer player = playerId == localPlayerId
                    ? new HumanPlayer(playerId, $"P{playerId}")
                    : (IPlayer)new NetworkPlayer(playerId, $"P{playerId}", (ulong)playerId);
                players.Add(player);
                if (playerId == localPlayerId)
                    sim.LocalPlayer = player;
                else
                    sim.RemotePlayer = player;
            }

            controller.Initialize(
                new CombatState(new List<IUnit>(), players, players[0], 1, CombatPhase.Combat),
                players);

            // The identical deterministic board both machines derive from the match setup.
            controller.AddUnit(new Unit(1, players[0], new HexCoordinates(0, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility(), PoisonLineAbility() },
                facingDirection: HexDirection.E));
            controller.AddUnit(new Unit(2, players[1], new HexCoordinates(4, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility(), PoisonLineAbility() },
                facingDirection: HexDirection.W));
            controller.ArmWinCondition();
            return sim;
        }

        private static void PumpResolve(ClientSim sim)
        {
            Assert.AreEqual(RoundPhase.EnemyResolve, sim.Controller.CombatState.RoundPhase,
                "the bundle must have reached this client");
            while (sim.Controller.ResolveNextEnemyIntent())
            {
            }
        }

        [Test]
        public void TwoClients_ResolveTheSameBundle_AndHashesMatch()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, localPlayerId: 1, isHost: true);
            var joiner = CreateClient(transport, localPlayerId: 2, isHost: false);

            host.Controller.BeginRounds();
            joiner.Controller.BeginRounds();

            // Round 1: each machine locks its OWN player's move; nothing resolves early.
            host.Controller.ProcessAction(new MoveAction(host.LocalPlayer, 1, new HexCoordinates(1, 0)));
            Assert.AreEqual(RoundPhase.PlayerAct, host.Controller.CombatState.RoundPhase,
                "half the lobby locked in — the round must wait");

            joiner.Controller.ProcessAction(new MoveAction(joiner.LocalPlayer, 2, new HexCoordinates(3, 0)));

            PumpResolve(host);
            PumpResolve(joiner);

            Assert.AreEqual(new HexCoordinates(1, 0), host.Controller.CombatState.GetUnit(1).Position);
            Assert.AreEqual(new HexCoordinates(1, 0), joiner.Controller.CombatState.GetUnit(1).Position,
                "the joiner's sim applied the host player's committed move identically");
            Assert.AreEqual(new HexCoordinates(3, 0), host.Controller.CombatState.GetUnit(2).Position,
                "the host's sim applied the joiner's committed move identically");

            Assert.AreNotEqual(0UL, host.Controller.LastRoundHash);
            Assert.AreEqual(host.Controller.LastRoundHash, joiner.Controller.LastRoundHash,
                "both sims report the identical lockstep hash (R10)");
        }

        [Test]
        public void CommittedBlow_ResolvesIdenticallyOnBothClients_AcrossTwoRounds()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, localPlayerId: 1, isHost: true);
            var joiner = CreateClient(transport, localPlayerId: 2, isHost: false);

            host.Controller.BeginRounds();
            joiner.Controller.BeginRounds();

            // Round 1: host schedules its ability (hidden, empty commitment); joiner sidesteps.
            host.Controller.ProcessAction(new ScheduleAbilityAction(host.LocalPlayer, 1, AbilityId));
            joiner.Controller.ProcessAction(new MoveAction(joiner.LocalPlayer, 2, new HexCoordinates(3, 0)));
            PumpResolve(host);
            PumpResolve(joiner);

            // Round 2: host turns east onto (1,0),(2,0)... nothing there — but the joiner walks
            // its unit INTO (2,0) this same round. Round-2 initiative belongs to player 2, so the
            // joiner moves first and the committed volley punishes the step-in — on both sims.
            host.Controller.ProcessAction(new ExecuteAbilityQueueAction(host.LocalPlayer, 1));
            joiner.Controller.ProcessAction(new MoveAction(joiner.LocalPlayer, 2, new HexCoordinates(2, 0)));
            PumpResolve(host);
            PumpResolve(joiner);

            Assert.AreEqual(MaxHp - BaseDamage, host.Controller.CombatState.GetUnit(2).CurrentHP);
            Assert.AreEqual(MaxHp - BaseDamage, joiner.Controller.CombatState.GetUnit(2).CurrentHP,
                "the volley landed identically on both simulations");
            Assert.AreEqual(host.Controller.LastRoundHash, joiner.Controller.LastRoundHash,
                "hashes still in lockstep after a damage round");
        }

        [Test]
        public void StatusApplication_TickAndExpiry_StayInLockstepAcrossRounds()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, localPlayerId: 1, isHost: true);
            var joiner = CreateClient(transport, localPlayerId: 2, isHost: false);

            host.Controller.BeginRounds();
            joiner.Controller.BeginRounds();

            // Round 1: host arms the poison line (hidden); joiner steps to (3,0).
            host.Controller.ProcessAction(new ScheduleAbilityAction(host.LocalPlayer, 1, StatusAbilityId));
            joiner.Controller.ProcessAction(new MoveAction(joiner.LocalPlayer, 2, new HexCoordinates(3, 0)));
            PumpResolve(host);
            PumpResolve(joiner);

            // Round 2: the volley fires along (1,0),(2,0); the joiner steps INTO (2,0) first
            // (round-2 initiative), takes the hit and the poison. The round-end tick then runs
            // identically on both sims.
            host.Controller.ProcessAction(new ExecuteAbilityQueueAction(host.LocalPlayer, 1));
            joiner.Controller.ProcessAction(new MoveAction(joiner.LocalPlayer, 2, new HexCoordinates(2, 0)));
            PumpResolve(host);
            PumpResolve(joiner);

            Assert.AreEqual(1, host.Controller.CombatState.GetUnit(2).StatusEffects.Count,
                "the poison rides the committed blow");
            Assert.AreEqual(host.Controller.LastRoundHash, joiner.Controller.LastRoundHash,
                "hashes agree on the round the status landed + first tick");

            // Rounds 3-4: both pass; the poison ticks down to expiry on both sims.
            for (int round = 0; round < 2; round++)
            {
                host.Controller.ProcessAction(new EndUnitTurnAction(host.LocalPlayer, 1));
                joiner.Controller.ProcessAction(new EndUnitTurnAction(joiner.LocalPlayer, 2));
                PumpResolve(host);
                PumpResolve(joiner);

                Assert.AreEqual(host.Controller.LastRoundHash, joiner.Controller.LastRoundHash,
                    "hashes stay in lockstep through every tick round");
            }

            // Applied at 5 (hybrid hit) + 4/turn over its 3-round life = 17 total.
            Assert.AreEqual(MaxHp - 17, host.Controller.CombatState.GetUnit(2).CurrentHP);
            Assert.AreEqual(
                host.Controller.CombatState.GetUnit(2).CurrentHP,
                joiner.Controller.CombatState.GetUnit(2).CurrentHP,
                "the whole DoT life resolved identically");
            CollectionAssert.IsEmpty(host.Controller.CombatState.GetUnit(2).StatusEffects,
                "expired and removed on the host sim");
            CollectionAssert.IsEmpty(joiner.Controller.CombatState.GetUnit(2).StatusEffects,
                "expired and removed on the joiner sim");
        }
    }
}
