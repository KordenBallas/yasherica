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
    /// The Phase 4 edge-rule sweep: the deterministic conflict corners the PRD delegated to us —
    /// swap-move, a caster standing in its own committed cells, a simultaneous last-two-die draw,
    /// a unit stunned when its committed step arrives, and a player departing mid-planning. Each
    /// runs the real production round pieces over the loopback transport.
    /// </summary>
    [TestFixture]
    public class ArenaEdgeCaseTests
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
        private AbilityShapeCalculator _shapeCalculator;
        private LoopbackArenaTransport _transport;
        private ArenaCombatController _controller;
        private ArenaAICommitSource _aiSource;
        private readonly Dictionary<int, ScriptedAI> _scripts = new Dictionary<int, ScriptedAI>();
        private readonly List<IPlayer> _players = new List<IPlayer>();

        private sealed class ScriptedAI : IAIDecisionMaker
        {
            private readonly Queue<IAction> _script = new Queue<IAction>();
            public void Enqueue(IAction action) => _script.Enqueue(action);

            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                _script.Count > 0 ? _script.Dequeue() : new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _scripts.Clear();
            _players.Clear();
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
        /// Player 1 (local human) + N scripted AI dummies (auto-committed each round) + M passive
        /// "remote" players (`HumanPlayer`s the AI source never drives — they stand in for joiners
        /// who are still planning, so a case can hold a round open or exercise a departure).
        /// </summary>
        private void CreateMatch(int dummyCount, int remotePlayerCount = 0)
        {
            var logger = new FakeLogger();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            var damage = new DamageSystem();
            var trigger = new StatusEffectTriggerProcessor(damage);
            var abilityExecutor = new AbilityExecutor(damage, trigger, _shapeCalculator, _config);
            var builder = new ArenaCommitBuilder(_shapeCalculator);
            _transport = new LoopbackArenaTransport();

            _controller = new ArenaCombatController(
                new ActionValidator(new CombatConfig(2f, HexOrientation.Flat, 3)),
                new ActionExecutor(abilityExecutor, logger),
                new TurnManager(logger),
                new RoundLifecycleProcessor(damage, trigger),
                new EnemyIntentResolver(abilityExecutor, logger),
                builder,
                new RotatingInitiativeOrder(),
                new LastHeroStandingWinCondition(),
                _transport,
                new ArenaMatchHost(new ArenaCommitCollector(), _transport, logger),
                null,
                _config,
                logger);

            _players.Add(new HumanPlayer(1, "P1"));
            for (int i = 0; i < dummyCount; i++)
            {
                int playerId = _players.Count + 1;
                var script = new ScriptedAI();
                _scripts[playerId] = script;
                _players.Add(new AIPlayer(playerId, $"P{playerId}", script));
            }
            for (int i = 0; i < remotePlayerCount; i++)
            {
                _players.Add(new HumanPlayer(_players.Count + 1, $"Remote{_players.Count + 1}"));
            }

            _controller.Initialize(
                new CombatState(new List<IUnit>(), _players, _players[0], 1, CombatPhase.Combat),
                _players);

            _aiSource = new ArenaAICommitSource(builder, _transport, logger);
            _aiSource.Initialize(_controller);
        }

        private void AddUnit(int unitId, int ownerIndex, HexCoordinates cell, int hp = MaxHp,
            HexDirection facing = HexDirection.E, IReadOnlyList<IStatusEffect> effects = null)
        {
            _controller.AddUnit(new Unit(unitId, _players[ownerIndex], cell, hp, MaxHp,
                new List<IAbilityInstance> { LineAbility() },
                statusEffects: effects, facingDirection: facing));
        }

        private void PumpResolve()
        {
            Assert.AreEqual(RoundPhase.EnemyResolve, _controller.CombatState.RoundPhase);
            while (_controller.ResolveNextEnemyIntent())
            {
            }
        }

        private IUnit Unit(int id) => _controller.CombatState.GetUnit(id);
        private IPlayer Human => _players[0];

        [Test]
        public void MoveIntoAnOccupiedCell_IsRejectedAtPlanTime()
        {
            // The validator blocks committing a move onto a currently-occupied cell — so a "swap"
            // (both aiming at each other's cell) or a "chase" (aiming into an occupant that is
            // leaving) can never be committed in the first place. The only reachable same-hex
            // conflict is two units racing to the same EMPTY cell (below). The rejected action is
            // not a commitment: the round stays open (nothing was locked in).
            CreateMatch(dummyCount: 0, remotePlayerCount: 1);
            AddUnit(1, 0, new HexCoordinates(0, 0));
            AddUnit(2, 1, new HexCoordinates(1, 0));
            _controller.ArmWinCondition();
            _controller.BeginRounds();

            var result = _controller.ProcessAction(new MoveAction(Human, 1, new HexCoordinates(1, 0)));

            Assert.IsFalse(result.Success, "cannot plan a move onto an occupied cell");
            Assert.AreEqual(RoundPhase.PlayerAct, _controller.CombatState.RoundPhase,
                "a rejected action locks in nothing");
            Assert.AreEqual(new HexCoordinates(0, 0), Unit(1).Position);
        }

        [Test]
        public void TwoUnitsRaceToTheSameEmptyCell_EarlierEnters_LaterFizzles()
        {
            // The reachable same-hex conflict: both commit a move to the same cell that is EMPTY at
            // plan time (so both pass validation). At resolve the earlier unit in initiative enters;
            // the later finds it occupied and fizzles — no re-target.
            CreateMatch(dummyCount: 1);
            var contested = new HexCoordinates(2, 0);
            AddUnit(1, 0, new HexCoordinates(1, 0));
            AddUnit(2, 1, new HexCoordinates(3, 0));
            _controller.ArmWinCondition();

            _scripts[2].Enqueue(new MoveAction(_players[1], 2, contested));
            _controller.BeginRounds();
            _controller.ProcessAction(new MoveAction(Human, 1, contested));
            PumpResolve();

            Assert.AreEqual(contested, Unit(1).Position, "round-1 initiative (player 1) enters the cell");
            Assert.AreEqual(new HexCoordinates(3, 0), Unit(2).Position, "the later mover fizzles in place");
        }

        [Test]
        public void CasterInOwnCommittedCells_IsUnharmedByItsOwnBlow()
        {
            // A Line(2) from (2,0) facing west covers (1,0),(0,0) — not the caster's own cell, so a
            // self-overlap can't arise for Line; assert the caster is never in its own target set
            // and takes no damage from firing. (Guards the "caster-in-own-cells" corner.)
            CreateMatch(dummyCount: 1);
            var human = new Unit(1, Human, new HexCoordinates(2, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() },
                new List<ScheduledAbility> { new ScheduledAbility(LineAbility(), 0) },
                facingDirection: HexDirection.W);
            _controller.AddUnit(human);
            AddUnit(2, 1, new HexCoordinates(0, 0));
            _controller.ArmWinCondition();

            _scripts[2].Enqueue(new EndUnitTurnAction(_players[1], 2));
            _controller.BeginRounds();
            _controller.ProcessAction(new ExecuteAbilityQueueAction(Human, 1));
            PumpResolve();

            Assert.AreEqual(MaxHp, Unit(1).CurrentHP, "the caster is not in its own committed cells");
            Assert.AreEqual(MaxHp - BaseDamage, Unit(2).CurrentHP, "the blow still landed on the target");
        }

        [Test]
        public void MutualNonLethalBlows_BothLand_MatchContinues()
        {
            // Full-HP human at (1,0) and dummy at (2,0) commit lethal-only-if-low casts at each
            // other; at 30 HP neither dies, so — unlike the lethal case (R4) — BOTH committed blows
            // resolve and both units survive damaged. (A true simultaneous last-two-die draw is
            // unreachable via committed blows under sequential skip-dead resolution — that draw path
            // is the defensive rule covered by ArenaCoreTests.WinCondition_NobodyAlive_IsADraw.)
            CreateMatch(dummyCount: 1);
            var human = new Unit(1, Human, new HexCoordinates(1, 0), MaxHp, MaxHp,
                new List<IAbilityInstance> { LineAbility() },
                new List<ScheduledAbility> { new ScheduledAbility(LineAbility(), 0) },
                facingDirection: HexDirection.E);
            _controller.AddUnit(human);                 // fires east → (2,0),(3,0): hits dummy
            AddUnit(2, 1, new HexCoordinates(2, 0), facing: HexDirection.W); // fires west → (1,0),(0,0): hits human
            _controller.ArmWinCondition();

            _scripts[2].Enqueue(new ScheduleAbilityAction(_players[1], 2, AbilityId, HexDirection.W));
            _controller.BeginRounds();
            _controller.ProcessAction(new ExecuteAbilityQueueAction(Human, 1));
            PumpResolve();

            Assert.AreEqual(MaxHp - BaseDamage, Unit(1).CurrentHP, "the dummy's committed blow landed");
            Assert.AreEqual(MaxHp - BaseDamage, Unit(2).CurrentHP, "the human's committed blow landed");
            Assert.AreEqual(CombatPhase.Combat, _controller.CombatState.Phase, "both alive — match goes on");
        }

        [Test]
        public void StunnedAtResolve_SkipsTheWholeCommitment()
        {
            // The dummy is stunned; its committed cast must not fire (the resolver skips a stunned
            // caster), so the human takes no damage even though a blow was committed at its cell.
            CreateMatch(dummyCount: 1);
            AddUnit(1, 0, new HexCoordinates(1, 0));
            AddUnit(2, 1, new HexCoordinates(3, 0), effects: new List<IStatusEffect> { new StunEffect(5) });
            _controller.ArmWinCondition();

            _scripts[2].Enqueue(new ScheduleAbilityAction(_players[1], 2, AbilityId, HexDirection.W));
            _controller.BeginRounds();
            _controller.ProcessAction(new EndUnitTurnAction(Human, 1));
            PumpResolve();

            Assert.AreEqual(MaxHp, Unit(1).CurrentHP, "a stunned caster's committed blow is skipped");
        }

        [Test]
        public void PlayerDepartsMidPlanning_ItsUnitDiesAndTheRoundCompletes()
        {
            // Player 1 (local human) + an AI dummy that locks in + a passive remote player who is
            // still planning when it leaves. After the human and dummy lock in, the round waits on
            // the remote; its departure removes that obligation (completing the round), and the
            // bundle carries the departure so its unit dies on normalize — leaving players 1 and 2
            // alive. Deterministic: the death rides the bundle, not local timing.
            CreateMatch(dummyCount: 1, remotePlayerCount: 1);
            AddUnit(1, 0, new HexCoordinates(0, 0));   // local human
            AddUnit(2, 1, new HexCoordinates(2, 0));   // AI dummy (auto-commits a pass)
            AddUnit(3, 2, new HexCoordinates(4, 0));   // passive remote — never commits
            _controller.ArmWinCondition();

            _scripts[2].Enqueue(new EndUnitTurnAction(_players[1], 2));
            _controller.BeginRounds();
            _controller.ProcessAction(new EndUnitTurnAction(Human, 1));

            Assert.AreEqual(RoundPhase.PlayerAct, _controller.CombatState.RoundPhase,
                "the round is still waiting on the remote player");
            _transport.SimulateDeparture(3);
            PumpResolve();

            Assert.IsFalse(Unit(3).IsAlive, "the departed player's unit died at the bundle");
            Assert.IsTrue(Unit(1).IsAlive);
            Assert.IsTrue(Unit(2).IsAlive);
        }
    }
}
