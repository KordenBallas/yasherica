using System.Collections.Generic;
using System.Linq;
using Combat.Arena;
using Combat.Arena.Core;
using Combat.Arena.Data;
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
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// X1 host migration: the deterministic election over the seat mirror, and the full
    /// drop → retry-window → elect → promote-self path of the reconnect state machine — the
    /// promoted peer re-hosts, re-seeds the seat book, rolls the round back to its planning
    /// anchor, and the match continues to a real outcome.
    /// </summary>
    [TestFixture]
    public class ArenaHostMigrationTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeClock : IArenaReconnectClock
        {
            public float Now { get; set; }
        }

        private sealed class FakeEndpointSource : IArenaLocalEndpointSource
        {
            public string Address;
            public string GetLocalAddress() => Address;
        }

        private sealed class FakeSession : IArenaSessionControl
        {
            public bool IsHost { get; set; }
            public bool IsListening { get; set; } = true;
            public ulong LocalClientId { get; set; }
            public bool MatchStarted { get; set; } = true;
            public IReadOnlyList<ulong> ConnectedClientIds { get; set; } = new List<ulong>();
            public IArenaRejoinGate Gate { get; private set; }
            public List<string> DialedAddresses { get; } = new List<string>();
            public int ShutdownCount { get; private set; }
            public bool StartHostResult = true;

            public event System.Action SessionStarted { add { } remove { } }
            public event System.Action<ulong> ClientConnected { add { } remove { } }
            public event System.Action<ulong> ClientDisconnected;

            public bool StartHost()
            {
                if (StartHostResult)
                {
                    IsHost = true;
                }

                return StartHostResult;
            }

            public bool StartClient(string address, byte[] connectPayload = null)
            {
                DialedAddresses.Add(address);
                return true;
            }

            public void Shutdown() => ShutdownCount++;
            public void SetRejoinGate(IArenaRejoinGate gate) => Gate = gate;

            public void RaiseClientDisconnected(ulong clientId) => ClientDisconnected?.Invoke(clientId);
        }

        private sealed class NoStatusReconstructor : IArenaStatusReconstructor
        {
            public bool TryRebuild(int effectId, int duration, int stackCount, out IStatusEffect effect)
            {
                effect = null;
                return false;
            }
        }

        private const int AbilityId = 100;
        private const int MaxHp = 30;
        private const int MatchSeed = 555;
        private const string HostAddress = "192.168.1.5";

        private HexDirectionConfig _config;
        private ArenaMatchConfig _matchConfig;

        private sealed class ClientSim
        {
            public ArenaCombatController Controller;
            public ArenaMatchHost MatchHost;
            public ArenaSeatLedger Ledger;
            public ArenaMatchContext Context;
            public ArenaReconnectHost ReconnectHost;
            public ArenaReconnectClient ReconnectClient;
            public FakeSession Session;
            public FakeClock Clock;
            public Dictionary<int, IPlayer> Players;
        }

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _matchConfig = ScriptableObject.CreateInstance<ArenaMatchConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_matchConfig);
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

            var context = new ArenaMatchContext();
            context.SetSetup(MatchSeed, Roster());
            context.SetLocalPlayerId(localPlayerId);
            context.SetHostAddress(isHost ? string.Empty : HostAddress);

            var session = new FakeSession { IsHost = isHost, LocalClientId = isHost ? 0UL : 7UL };
            var clock = new FakeClock();
            var mirror = new ArenaSeatStatusMirror();
            var restorer = new ArenaSnapshotRestorer(new NoStatusReconstructor(), logger);
            var reconnectHost = new ArenaReconnectHost(
                ledger, matchHost, context, controller, transport, session, logger);
            var reconnectClient = new ArenaReconnectClient(
                session, transport, controller, restorer, context, mirror, ledger, matchHost,
                reconnectHost, _matchConfig, clock, new FakeEndpointSource(), logger);

            if (isHost)
            {
                matchHost.SeedSeats(MatchSeed, Roster(), _matchConfig.DisconnectGraceRounds);
                reconnectHost.Activate();
            }

            return new ClientSim
            {
                Controller = controller,
                MatchHost = matchHost,
                Ledger = ledger,
                Context = context,
                ReconnectHost = reconnectHost,
                ReconnectClient = reconnectClient,
                Session = session,
                Clock = clock,
                Players = players
            };
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

        private static void Pass(ClientSim sim, int unitId)
        {
            sim.Controller.ProcessAction(new EndUnitTurnAction(sim.Players[unitId], unitId));
        }

        // ---- the pure rules ----

        [Test]
        public void Election_PicksLowestConnected_ExcludingLostHostAndGracing()
        {
            var mirror = new ArenaSeatStatusMirror();
            mirror.Reset(new List<ArenaRosterSlot>
            {
                new ArenaRosterSlot(0, 1, 1),
                new ArenaRosterSlot(5, 2, 2),
                new ArenaRosterSlot(7, 3, 3),
                new ArenaRosterSlot(9, 4, 4)
            });

            Assert.AreEqual(1, mirror.HostPlayerId, "the lowest connection id hosts");

            // Player 2 is riding grace, player 4 departed — the latest bundle says so.
            mirror.ApplyBundle(new ArenaRoundBundle(3,
                new List<ArenaCommit>(),
                departedPlayerIds: new List<int> { 4 },
                autoPassedPlayerIds: new List<int> { 2 }));

            var candidates = mirror.ElectionCandidates(mirror.HostPlayerId);
            CollectionAssert.AreEqual(new[] { 3 }, candidates.ToArray(),
                "gracing and departed seats cannot take over");
            Assert.AreEqual(3, ArenaHostElection.Elect(candidates));
            Assert.AreEqual(0, ArenaHostElection.Elect(new List<int>()), "nobody left = no host");
        }

        [Test]
        public void Mirror_TracksAddresses_FromTheBook()
        {
            var mirror = new ArenaSeatStatusMirror();
            mirror.Reset(Roster());
            mirror.ApplyAddressBook(new ArenaAddressBook(new List<ArenaEndpoint>
            {
                new ArenaEndpoint(2, "10.0.0.2")
            }));

            Assert.IsTrue(mirror.TryGetAddress(2, out var address));
            Assert.AreEqual("10.0.0.2", address);
            Assert.IsFalse(mirror.TryGetAddress(1, out _));
        }

        // ---- the drop state machine ----

        [Test]
        public void Drop_RetriesTheKnownHost_BeforeAnyElection()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);
            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            peer.ReconnectClient.ArmForMatch();

            peer.Session.RaiseClientDisconnected(peer.Session.LocalClientId);
            Assert.AreEqual(ArenaReconnectPhase.RetryingHost, peer.ReconnectClient.Phase);

            // Two retry intervals inside the window: both dial the ORIGINAL host address with a
            // rejoin payload.
            peer.Clock.Now = 0.1f;
            peer.ReconnectClient.Tick();
            peer.Clock.Now += _matchConfig.ReconnectRetryIntervalSeconds + 0.1f;
            peer.ReconnectClient.Tick();

            Assert.GreaterOrEqual(peer.Session.DialedAddresses.Count, 2);
            Assert.IsTrue(peer.Session.DialedAddresses.All(a => a == HostAddress),
                "a live host must be retried before anyone is elected");
        }

        [Test]
        public void HostLoss_SurvivorPromotes_RoundReopens_AndWinsAfterGrace()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            peer.ReconnectClient.ArmForMatch();

            // Round 1 resolves in lockstep; the drop hits mid round 2.
            Pass(host, 1);
            Pass(peer, 2);
            PumpResolve(host, peer);
            int roundAtDrop = peer.Controller.CombatState.TurnNumber;

            // The host machine dies: its assembler goes silent, the peer's connection drops.
            host.MatchHost.Deactivate();
            peer.Session.RaiseClientDisconnected(peer.Session.LocalClientId);
            Assert.AreEqual(ArenaReconnectPhase.RetryingHost, peer.ReconnectClient.Phase);

            // The retry window burns out with nobody answering → election → self-promotion
            // (player 1 is the lost host; player 2 is the only candidate).
            peer.Clock.Now = _matchConfig.ReconnectAttemptSeconds + 0.5f;
            peer.ReconnectClient.Tick();

            Assert.AreEqual(ArenaReconnectPhase.InMatch, peer.ReconnectClient.Phase,
                "the survivor promoted itself and resumed");
            Assert.IsTrue(peer.Session.IsHost);
            Assert.IsTrue(peer.Session.MatchStarted, "the re-hosted session admits only rejoiners");
            Assert.AreSame(peer.ReconnectHost, peer.Session.Gate, "the promoted host validates rejoins");
            Assert.AreEqual(roundAtDrop, peer.Controller.CombatState.TurnNumber,
                "the round rolled back to its own planning start — no round was skipped");
            Assert.AreEqual(RoundPhase.PlayerAct, peer.Controller.CombatState.RoundPhase);

            // The old host's seat rides grace on the new authority; the survivor plays on alone
            // until grace expires, the seat departs, and last-hero-standing settles the match.
            for (int round = 0;
                round < _matchConfig.DisconnectGraceRounds + 3
                    && peer.Controller.CombatState.Phase == CombatPhase.Combat;
                round++)
            {
                Pass(peer, 2);
                if (peer.Controller.CombatState.RoundPhase == RoundPhase.EnemyResolve)
                {
                    while (peer.Controller.ResolveNextEnemyIntent())
                    {
                    }
                }
            }

            Assert.AreEqual(CombatPhase.Victory, peer.Controller.CombatState.Phase,
                "the departed host's unit died and the survivor won");
        }

        [Test]
        public void HostLoss_OldHostSeat_CanRejoinTheNewHost()
        {
            var transport = new LoopbackArenaTransport();
            var host = CreateClient(transport, 1, isHost: true);
            var peer = CreateClient(transport, 2, isHost: false);

            host.Controller.BeginRounds();
            peer.Controller.BeginRounds();
            peer.ReconnectClient.ArmForMatch();

            host.MatchHost.Deactivate();
            peer.Session.RaiseClientDisconnected(peer.Session.LocalClientId);
            peer.Clock.Now = _matchConfig.ReconnectAttemptSeconds + 0.5f;
            peer.ReconnectClient.Tick();

            // The dead host returns like any rejoiner: its derived token claims its gracing seat
            // on the NEW authority.
            var token = ArenaRejoinToken.For(MatchSeed, 1);
            Assert.IsTrue(peer.Session.Gate.TryApproveRejoin(1, token, newClientId: 42),
                "the old host's seat was seeded into grace, not foreclosed");
            Assert.AreEqual(ArenaSeatConnection.Resyncing, peer.Ledger.ConnectionOf(1));
        }

        [Test]
        public void HostLoss_ElectedPeerUnreachable_FallsBackToConnectionLost()
        {
            var transport = new LoopbackArenaTransport();
            // Three seats: the local machine is player 3, so player 2 wins the election — but
            // nobody ever broadcast an address book entry for it.
            var roster = new List<ArenaRosterSlot>
            {
                new ArenaRosterSlot(0, 1, 1),
                new ArenaRosterSlot(7, 2, 2),
                new ArenaRosterSlot(9, 3, 3)
            };
            var logger = new FakeLogger();
            var context = new ArenaMatchContext();
            context.SetSetup(MatchSeed, roster);
            context.SetLocalPlayerId(3);
            context.SetHostAddress(HostAddress);

            var session = new FakeSession { IsHost = false, LocalClientId = 9UL };
            var clock = new FakeClock();
            var mirror = new ArenaSeatStatusMirror();
            var ledger = new ArenaSeatLedger();
            var matchHost = new ArenaMatchHost(new ArenaCommitCollector(), ledger, transport, logger);
            var sim = CreateClient(transport, 2, isHost: false);
            var reconnectHost = new ArenaReconnectHost(
                ledger, matchHost, context, sim.Controller, transport, session, logger);
            var client = new ArenaReconnectClient(
                session, transport, sim.Controller, new ArenaSnapshotRestorer(new NoStatusReconstructor(), logger),
                context, mirror, ledger, matchHost, reconnectHost, _matchConfig, clock,
                new FakeEndpointSource(), logger);

            bool failed = false;
            client.ReconnectFailed += () => failed = true;
            client.ArmForMatch();

            session.RaiseClientDisconnected(9UL);
            clock.Now = _matchConfig.ReconnectAttemptSeconds + 0.5f;
            client.Tick();

            Assert.IsTrue(failed, "no address for the elected seat → the old terminal UX");
            Assert.AreEqual(ArenaReconnectPhase.Failed, client.Phase);
        }
    }
}
