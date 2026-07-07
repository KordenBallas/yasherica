using System.Collections.Generic;
using System.Linq;
using Combat.Arena;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Player;
using Combat.TurnManagement;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// End-to-end symmetric round over the loopback transport with the real pieces (validator,
    /// executor, host, collector, rotating order, intent resolver, lifecycle): hidden commit →
    /// gather → bundle → normalize → simultaneous resolve. Proves the PRD acceptance semantics —
    /// whiff on dodge (R11), deterministic same-hex conflict (R12), lethal-first-skips-return
    /// (R4/R5), last-hero-standing + rotation of initiative — and the determinism replay gate
    /// (same seed + same committed actions → identical state hash).
    /// </summary>
    [TestFixture]
    public class ArenaRoundFlowTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        /// <summary>Scripted dummy: returns the queued action for each round, then passes.</summary>
        private sealed class ScriptedAI : IAIDecisionMaker
        {
            private readonly Queue<IAction> _script = new Queue<IAction>();
            public void Enqueue(IAction action) => _script.Enqueue(action);

            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                _script.Count > 0 ? _script.Dequeue() : new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private const int HumanUnitId = 1;
        private const int AiUnitId = 2;
        private const int AbilityId = 100;
        private const int BaseDamage = 10;
        private const int MaxHp = 30;

        private HexDirectionConfig _config;
        private AbilityShapeCalculator _shapeCalculator;
        private ArenaCombatController _controller;
        private ArenaAICommitSource _aiSource;
        private HumanPlayer _human;
        private AIPlayer _ai;
        private ScriptedAI _script;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
        }

        [TearDown]
        public void TearDown()
        {
            _aiSource?.Dispose();
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineAbility() =>
            new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 2, AbilityShapeData.ForLine(2), BaseDamage));

        /// <summary>
        /// Builds a full 1-human + 1-scripted-dummy match with the production wiring over a
        /// loopback transport. Heroes: human at humanCell (with a pre-built one-ability queue so
        /// a volley can fire in round 1), dummy at aiCell.
        /// </summary>
        private void CreateMatch(
            HexCoordinates humanCell, HexCoordinates aiCell, int humanHp = MaxHp, int aiHp = MaxHp)
        {
            var logger = new FakeLogger();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, _shapeCalculator, _config);
            var builder = new ArenaCommitBuilder(_shapeCalculator);
            var transport = new LoopbackArenaTransport();

            _controller = new ArenaCombatController(
                new ActionValidator(new CombatConfig(2f, HexOrientation.Flat, 3)),
                new ActionExecutor(abilityExecutor, logger),
                new TurnManager(logger),
                new RoundLifecycleProcessor(trigger),
                new EnemyIntentResolver(abilityExecutor, logger),
                builder,
                new RotatingInitiativeOrder(),
                new LastHeroStandingWinCondition(),
                transport,
                new ArenaMatchHost(new ArenaCommitCollector(), transport, logger),
                null,
                _config,
                logger);

            _human = new HumanPlayer(1, "P1");
            _script = new ScriptedAI();
            _ai = new AIPlayer(2, "P2", _script);

            var players = new List<IPlayer> { _human, _ai };
            _controller.Initialize(
                new CombatState(new List<IUnit>(), players, players[0], 1, CombatPhase.Combat),
                players);

            _aiSource = new ArenaAICommitSource(builder, transport, logger);
            _aiSource.Initialize(_controller);

            var humanAbility = LineAbility();
            var humanUnit = new Unit(HumanUnitId, _human, humanCell, humanHp, MaxHp,
                new List<IAbilityInstance> { humanAbility },
                new List<ScheduledAbility> { new ScheduledAbility(humanAbility, 0) },
                facingDirection: HexDirection.W);
            var aiUnit = new Unit(AiUnitId, _ai, aiCell, aiHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() },
                facingDirection: HexDirection.W);

            _controller.AddUnit(humanUnit);
            _controller.AddUnit(aiUnit);
            _controller.ArmWinCondition();
        }

        /// <summary>Drives the Resolve phase to its end (what the pacing coroutine does in play).</summary>
        private void PumpResolve()
        {
            Assert.AreEqual(RoundPhase.EnemyResolve, _controller.CombatState.RoundPhase,
                "bundle must have arrived (all players locked in)");
            while (_controller.ResolveNextEnemyIntent())
            {
            }
        }

        private IUnit Human() => _controller.CombatState.GetUnit(HumanUnitId);
        private IUnit Ai() => _controller.CombatState.GetUnit(AiUnitId);

        [Test]
        public void NothingResolves_UntilEveryAlivePlayerLockedIn()
        {
            CreateMatch(new HexCoordinates(1, 0), new HexCoordinates(4, 0));
            // Dummy commits a cast at the human's cell the moment planning opens.
            _script.Enqueue(new ScheduleAbilityAction(_ai, AiUnitId, AbilityId, HexDirection.W));
            _controller.BeginRounds();

            Assert.AreEqual(RoundPhase.PlayerAct, _controller.CombatState.RoundPhase,
                "AI locked in, human has not — the round must wait");
            Assert.AreEqual(MaxHp, Human().CurrentHP, "no committed action fires early");
        }

        [Test]
        public void CommittedCast_Whiffs_WhenTheTargetMovedAway()
        {
            // Dummy at (4,0) casts west → committed cells (3,0),(2,0). Human at (3,0) dodges.
            CreateMatch(new HexCoordinates(3, 0), new HexCoordinates(4, 0));
            _script.Enqueue(new ScheduleAbilityAction(_ai, AiUnitId, AbilityId, HexDirection.W));
            _controller.BeginRounds();

            // Round 1 initiative starts at player 1: the dodge resolves before the blow.
            _controller.ProcessAction(new MoveAction(_human, HumanUnitId, new HexCoordinates(3, 1)));
            PumpResolve();

            Assert.AreEqual(new HexCoordinates(3, 1), Human().Position, "the dodge landed");
            Assert.AreEqual(MaxHp, Human().CurrentHP, "the committed blow whiffed — it never re-targets");
        }

        [Test]
        public void CommittedCast_HitsAUnitThatSteppedIntoTheCommittedCells()
        {
            // Dummy at (4,0) casts west → cells (3,0),(2,0). Human at (2,1) steps INTO (3,0).
            CreateMatch(new HexCoordinates(2, 1), new HexCoordinates(4, 0));
            _script.Enqueue(new ScheduleAbilityAction(_ai, AiUnitId, AbilityId, HexDirection.W));
            _controller.BeginRounds();

            _controller.ProcessAction(new MoveAction(_human, HumanUnitId, new HexCoordinates(3, 0)));
            PumpResolve();

            Assert.AreEqual(MaxHp - BaseDamage, Human().CurrentHP,
                "stepping into committed cells is punished — the blow fires as committed");
        }

        [Test]
        public void SameHexConflict_ResolvesByInitiative_LaterMoveFizzles()
        {
            CreateMatch(new HexCoordinates(0, 0), new HexCoordinates(4, 0));
            var contested = new HexCoordinates(2, 0);
            _script.Enqueue(new MoveAction(_ai, AiUnitId, contested));
            _controller.BeginRounds();

            // Round 1: player 1 holds initiative → the human enters, the dummy fizzles in place.
            _controller.ProcessAction(new MoveAction(_human, HumanUnitId, contested));
            PumpResolve();

            Assert.AreEqual(contested, Human().Position, "earlier in the order enters the hex");
            Assert.AreEqual(new HexCoordinates(4, 0), Ai().Position,
                "the later committed move fizzles — no re-target to a neighbour");
        }

        [Test]
        public void LethalEarlierBlow_SkipsTheVictimsCommitment_AndDeclaresTheWinner()
        {
            // Human at (2,0) fires its pre-queued volley west → cells (1,0),(0,0) where the
            // 10-HP dummy stands; the dummy committed a cast back at the human.
            CreateMatch(new HexCoordinates(2, 0), new HexCoordinates(1, 0), aiHp: BaseDamage);
            _script.Enqueue(new ScheduleAbilityAction(_ai, AiUnitId, AbilityId, HexDirection.E));

            IPlayer winner = null;
            CombatPhase endPhase = CombatPhase.Combat;
            _controller.OnGameEnded += (w, p) => { winner = w; endPhase = p; };

            _controller.BeginRounds();
            _controller.ProcessAction(new ExecuteAbilityQueueAction(_human, HumanUnitId));
            PumpResolve();

            Assert.AreEqual(0, Ai().CurrentHP, "the volley killed the dummy");
            Assert.AreEqual(MaxHp, Human().CurrentHP,
                "a unit dead before its step never fires (mutual blows are sequential — R4/R5)");
            Assert.AreSame(_human, winner, "last hero standing wins");
            Assert.AreEqual(CombatPhase.Victory, endPhase);
        }

        [Test]
        public void InitiativeRotates_TheOtherPlayerWinsTheSameConflictNextRound()
        {
            CreateMatch(new HexCoordinates(0, 0), new HexCoordinates(4, 0));
            var contestedRound2 = new HexCoordinates(2, 1);
            _script.Enqueue(new MoveAction(_ai, AiUnitId, new HexCoordinates(4, 1)));
            _script.Enqueue(new MoveAction(_ai, AiUnitId, contestedRound2));
            _controller.BeginRounds();

            // Round 1: harmless repositioning.
            _controller.ProcessAction(new MoveAction(_human, HumanUnitId, new HexCoordinates(0, 1)));
            PumpResolve();

            // Round 2: both contest one hex — initiative rotated to player 2.
            _controller.ProcessAction(new MoveAction(_human, HumanUnitId, contestedRound2));
            PumpResolve();

            Assert.AreEqual(contestedRound2, Ai().Position, "round 2 initiative belongs to player 2");
            Assert.AreEqual(new HexCoordinates(0, 1), Human().Position, "the human's move fizzled");
        }

        [Test]
        public void Replay_SameCommittedActions_ProduceIdenticalStateHashes()
        {
            ulong RunMatch()
            {
                CreateMatch(new HexCoordinates(3, 0), new HexCoordinates(4, 0));
                _script.Enqueue(new ScheduleAbilityAction(_ai, AiUnitId, AbilityId, HexDirection.W));
                _script.Enqueue(new MoveAction(_ai, AiUnitId, new HexCoordinates(4, 1)));
                _controller.BeginRounds();

                _controller.ProcessAction(new MoveAction(_human, HumanUnitId, new HexCoordinates(2, 1)));
                PumpResolve();
                _controller.ProcessAction(new MoveAction(_human, HumanUnitId, new HexCoordinates(1, 1)));
                PumpResolve();

                var hash = _controller.LastRoundHash;
                _aiSource.Dispose();
                _aiSource = null;
                return hash;
            }

            var first = RunMatch();
            var second = RunMatch();

            Assert.AreNotEqual(0UL, first, "two rounds completed");
            Assert.AreEqual(first, second, "same commits → identical lockstep hash (R10)");
        }
    }
}
