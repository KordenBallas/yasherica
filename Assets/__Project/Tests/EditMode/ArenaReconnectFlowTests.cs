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
    /// X1 end-to-end over the loopback bus: a dropped seat is auto-passed (the match never
    /// stalls, the unit stays alive in place), grace expiry folds into a normal departure, a
    /// rejoiner is stood up from the host's snapshot and re-enters lockstep, and a diverged
    /// client heals off the targeted resync — hashes converge in every scenario.
    /// </summary>
    [TestFixture]
    public class ArenaReconnectFlowTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeSession : IArenaSessionControl
        {
            public bool IsHost { get; set; }
            public bool IsListening { get; set; } = true;
            public ulong LocalClientId { get; set; }
            public bool MatchStarted { get; set; } = true;
            public IReadOnlyList<ulong> ConnectedClientIds { get; set; } = new List<ulong>();
            public IArenaRejoinGate Gate { get; private set; }

            public event System.Action SessionStarted { add { } remove { } }
            public event System.Action<ulong> ClientConnected;
            public event System.Action<ulong> ClientDisconnected { add { } remove { } }

            public bool StartHost() => true;
            public bool StartClient(string address, byte[] connectPayload = null) => true;
            public void Shutdown() { }
            public void SetRejoinGate(IArenaRejoinGate gate) => Gate = gate;

            public void RaiseClientConnected(ulong clientId) => ClientConnected?.Invoke(clientId);
        }

        /// <summary>Rebuilds the hand-authored poison by id (the test twin of the catalog path).</summary>
        private sealed class FakeStatusReconstructor : IArenaStatusReconstructor
        {
            public bool TryRebuild(int effectId, int duration, int stackCount, out IStatusEffect effect)
            {
                effect = effectId == PoisonId
                    ? new DataDrivenDamageOverTimeEffect(
                        PoisonId, "Poison", duration, stackCount, StackRule.StackToCap, 3,
                        StatusEffectTriggerType.TurnEnd, 4, 4)
                    : null;
                return effect != null;
            }
        }

        private const int AbilityId = 100;
        private const int PoisonId = 2;
        private const int MaxHp = 30;
        private const int MatchSeed = 4242;
        private const int GraceRounds = 2;

        private HexDirectionConfig _config;

        private sealed class ClientSim
        {
            public ArenaCombatController Controller;
            public ArenaMatchHost MatchHost;
            public ArenaSeatLedger Ledger;
            public ArenaMatchContext Context;
            public ArenaReconnectHost ReconnectHost;
            public FakeSession Session;
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
            new ArenaRosterSlot(7, 2, 2),
            new ArenaRosterSlot(9, 3, 3)
        };

        /// <summary>
        /// One machine's full simulation (three seats, its own controller/ledger/match host) over
        /// the shared bus. The host sim seeds its seat book and activates the reconnect host.
        /// </summary>
        private ClientSim CreateClient(LoopbackArenaTransport transport, int localPlayerId, bool isHost)
        {
            var logger = new FakeLogger();
            var shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, shapeCalculator, _config);
            var ledger = new ArenaSeatLedger();
            var matchHost = new ArenaMatchHost(new ArenaCommitCollector(), ledger, transport, logger);

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
                matchHost,
                null,
                _config,
                logger);
            controller.SetHostRole(isHost);

            var players = new Dictionary<int, IPlayer>();
            var seatList = new List<IPlayer>();
            for (int playerId = 1; playerId <= 3; playerId++)
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
            SpawnBoard(controller, players);
            controller.ArmWinCondition();

            var context = new ArenaMatchContext();
            context.SetSetup(MatchSeed, Roster());
            context.SetLocalPlayerId(localPlayerId);

            var session = new FakeSession { IsHost = isHost, LocalClientId = isHost ? 0UL : (ulong)localPlayerId };
            var reconnectHost = new ArenaReconnectHost(
                ledger, matchHost, context, controller, transport, session, logger);

            if (isHost)
            {
                matchHost.SeedSeats(MatchSeed, Roster(), GraceRounds);
                reconnectHost.Activate();
            }

            return new ClientSim
            {
                Controller = controller,
                MatchHost = matchHost,
                Ledger = ledger,
                Context = context,
                ReconnectHost = reconnectHost,
                Session = session,
                Players = players
            };
        }

        /// <summary>The identical deterministic board every machine derives from the setup.</summary>
        private static void SpawnBoard(ArenaCombatController controller, Dictionary<int, IPlayer> players)
        {
            controller.AddUnit(new Unit(1, players[1], new HexCoordinates(0, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.E));
            controller.AddUnit(new Unit(2, players[2], new HexCoordinates(4, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.W));
            controller.AddUnit(new Unit(3, players[3], new HexCoordinates(0, 4), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.E));
        }

        private static void PumpResolve(params ClientSim[] sims)
        {
            foreach (var sim in sims)
            {
                Assert.AreEqual(RoundPhase.EnemyResolve, sim.Controller.CombatState.RoundPhase,
                    "the bundle must have reached this client");
                while (sim.Controller.ResolveNextEnemyIntent())
                {
                }
            }
        }

        private static void Pass(ClientSim sim, int unitId)
        {
            sim.Controller.ProcessAction(new EndUnitTurnAction(sim.Players[unitId], unitId));
        }

        [Test]
        public void DisconnectedSeat_AutoPasses_MatchContinues_HashesConverge()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            ArenaRoundBundle observed = null;
            transport.BundleReceived += b => observed = b;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            // Player 3 drops mid-planning; the two survivors lock in.
            transport.SimulateDeparture(3);
            Pass(host, 1);
            Pass(peer, 2);

            Assert.IsNotNull(observed, "the round completed without player 3's commit");
            CollectionAssert.AreEqual(new[] { 3 }, observed.AutoPassedPlayerIds.ToArray(),
                "the drop rides the bundle as an auto-pass, not a departure");
            CollectionAssert.IsEmpty(observed.DepartedPlayerIds);

            PumpResolve(host, peer);

            Assert.IsTrue(host.Controller.CombatState.GetUnit(3).IsAlive,
                "the gracing unit stays alive in place");
            Assert.AreEqual(new HexCoordinates(0, 4), host.Controller.CombatState.GetUnit(3).Position);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash,
                "the survivors stay in lockstep");
        }

        [Test]
        public void GraceExpiry_FoldsIntoDeparture_UnitDiesOnAllClients()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            ArenaRoundBundle observed = null;
            transport.BundleReceived += b => observed = b;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            transport.SimulateDeparture(3);

            // The drop round + GraceRounds auto-passed rounds, then the seat expires.
            for (int round = 0; round <= GraceRounds; round++)
            {
                Pass(host, 1);
                Pass(peer, 2);
                CollectionAssert.AreEqual(new[] { 3 }, observed.AutoPassedPlayerIds.ToArray(),
                    $"round {round + 1} still auto-passes the gracing seat");
                PumpResolve(host, peer);
            }

            Pass(host, 1);
            Pass(peer, 2);
            CollectionAssert.AreEqual(new[] { 3 }, observed.DepartedPlayerIds.ToArray(),
                "grace ran out — the seat departs for good");
            CollectionAssert.IsEmpty(observed.AutoPassedPlayerIds);
            PumpResolve(host, peer);

            Assert.IsFalse(host.Controller.CombatState.GetUnit(3).IsAlive);
            Assert.IsFalse(peer.Controller.CombatState.GetUnit(3).IsAlive);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash);
        }

        /// <summary>Stands a fresh sim up from a rejoin package the way the reconnect client does.</summary>
        private ClientSim RejoinAsFreshSim(
            LoopbackArenaTransport transport, int playerId, ulong newClientId, ClientSim hostSim)
        {
            ArenaRejoinPackage package = null;
            transport.RejoinPackageReceived += p => package = p;

            var token = ArenaRejoinToken.For(MatchSeed, playerId);
            Assert.IsTrue(hostSim.Session.Gate.TryApproveRejoin(playerId, token, newClientId),
                "the derived token claims the gracing seat");
            hostSim.ReconnectHost.HandleClientConnected(newClientId);
            Assert.IsNotNull(package, "the host ships the stand-up package on connect");
            Assert.AreEqual(playerId, package.TargetPlayerId);

            // The rejoiner spawns the deterministic board (seed + loadouts), then overlays.
            var rejoiner = CreateClient(transport, playerId, isHost: false);
            var restorer = new ArenaSnapshotRestorer(new FakeStatusReconstructor(), new FakeLogger());
            var restored = restorer.Restore(rejoiner.Controller.CombatState, package.Snapshot);
            Assert.IsNotNull(restored, "the snapshot must restore over the fresh board");
            rejoiner.Controller.AdoptState(restored, package.Snapshot.LastRoundHash);
            rejoiner.Controller.ReopenCurrentRound();
            if (package.CurrentRoundBundleOrNull != null)
            {
                rejoiner.Controller.ReplayBundle(package.CurrentRoundBundleOrNull);
            }

            transport.SubmitResyncAck(new ArenaResyncAck(playerId, package.Snapshot.RoundNumber));
            return rejoiner;
        }

        [Test]
        public void Rejoin_WithinGrace_RestoresLockstep_HashesConvergeNextRound()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();

            // Round 1 plays with player 3 present-but-idle on both live sims? No — player 3
            // DROPS before round 1 completes; one full auto-passed round goes by.
            transport.SimulateDeparture(3);
            Pass(host, 1);
            Pass(peer, 2);
            PumpResolve(host, peer);

            // Mid round 2, the player returns on a new connection and re-enters the open round.
            var rejoiner = RejoinAsFreshSim(transport, 3, newClientId: 42, host);

            Assert.AreEqual(host.Controller.CombatState.TurnNumber,
                rejoiner.Controller.CombatState.TurnNumber, "the rejoiner adopted the open round");

            // All three commit round 2; the rejoiner is reinstated (owes a commit again).
            Pass(host, 1);
            Pass(peer, 2);
            Assert.AreEqual(RoundPhase.PlayerAct, host.Controller.CombatState.RoundPhase,
                "the reinstated seat holds the round open until it locks");
            Pass(rejoiner, 3);

            PumpResolve(host, peer, rejoiner);

            Assert.AreNotEqual(0UL, host.Controller.LastRoundHash);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash);
            Assert.AreEqual(host.Controller.LastRoundHash, rejoiner.Controller.LastRoundHash,
                "the transferred sim resolves the next round in lockstep");
        }

        [Test]
        public void Rejoin_AfterBundleBroadcast_ReplaysTheBundle_AndConverges()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            transport.SimulateDeparture(3);

            // The survivors lock round 1 — the bundle broadcasts BEFORE the rejoiner attaches.
            Pass(host, 1);
            Pass(peer, 2);

            var rejoiner = RejoinAsFreshSim(transport, 3, newClientId: 42, host);
            Assert.AreEqual(RoundPhase.EnemyResolve, rejoiner.Controller.CombatState.RoundPhase,
                "the replayed bundle drove the rejoiner into the resolve phase");

            PumpResolve(host, peer, rejoiner);

            Assert.AreEqual(host.Controller.LastRoundHash, rejoiner.Controller.LastRoundHash,
                "snapshot + replayed bundle land the rejoiner on the identical end-of-round state");
        }

        [Test]
        public void Desync_TargetedResync_HealsTheDivergedClient()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            ArenaResyncCommand resync = null;
            transport.ResyncReceived += c => resync = c;

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            transport.SimulateDeparture(3);

            // Round 1 resolves in lockstep.
            Pass(host, 1);
            Pass(peer, 2);
            PumpResolve(host, peer);

            // Corrupt the peer's sim (a rogue local write) — its next commit reports a bad hash.
            var corrupted = (peer.Controller.CombatState as CombatState).WithUpdatedUnit(
                (peer.Controller.CombatState.GetUnit(1) as Unit).WithHP(1));
            peer.Controller.AdoptState(corrupted, ArenaStateHash.Compute(corrupted));

            Pass(peer, 2);

            Assert.IsNotNull(resync, "the host healed the diverged client on detection");
            Assert.AreEqual(2, resync.TargetPlayerId);

            // The peer applies the heal the way the reconnect client does.
            var restorer = new ArenaSnapshotRestorer(new FakeStatusReconstructor(), new FakeLogger());
            var restored = restorer.Restore(peer.Controller.CombatState, resync.Snapshot);
            Assert.IsNotNull(restored);
            peer.Controller.AdoptState(restored, resync.Snapshot.LastRoundHash);
            peer.Controller.ReopenCurrentRound();

            Assert.AreEqual(MaxHp, peer.Controller.CombatState.GetUnit(1).CurrentHP,
                "the corrupted board is gone");

            // The peer's round-2 commit was already accepted (first one wins) — the host closes
            // the round once player 1 locks; both sims converge again.
            Pass(host, 1);
            PumpResolve(host, peer);
            Assert.AreEqual(host.Controller.LastRoundHash, peer.Controller.LastRoundHash,
                "hashes converge after the heal");
        }
    }
}
