using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Combat.Player;
using Combat.TurnManagement;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Direct coverage of the shared <see cref="CombatRoundEngine"/> extracted in A1 (the PvE controller had
    /// no direct test before — it was play-mode-only). Drives the engine against a recording fake
    /// <see cref="ICombatRoundFlow"/> so the shared mechanics are exercised in isolation from either mode's
    /// round shape: the AddUnit guards, the resolve-loop control flow, and the win-check (including the
    /// no-double-fire guard the two controllers now share).
    /// </summary>
    [TestFixture]
    public class CombatRoundEngineTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        /// <summary>Records which flow hooks the engine invoked, without shaping a real round.</summary>
        private sealed class RecordingFlow : ICombatRoundFlow
        {
            public int EnemyResolveFinishedCount { get; private set; }
            public void OnInitialized(CombatRoundEngine engine) { }
            public void OnRoundStart(CombatRoundEngine engine) { }
            public Combat.Execution.ActionResult ProcessAction(CombatRoundEngine engine, IAction action) => null;
            public void OnIntentResolved(CombatRoundEngine engine, EnemyIntent intent) { }
            public void OnEnemyResolveFinished(CombatRoundEngine engine) { EnemyResolveFinishedCount++; }
            public void OnRoundEndTail(CombatRoundEngine engine) { }
            public void OnCleanup(CombatRoundEngine engine) { }
        }

        private sealed class FakeTurnManager : ITurnManager
        {
            private IPlayer _current;
            public IPlayer CurrentPlayer => _current;
            public int CurrentTurnNumber => 0;
            public IReadOnlyList<IPlayer> TurnOrder => new List<IPlayer>();
            public void Initialize(IReadOnlyList<IPlayer> players) { _current = players.Count > 0 ? players[0] : null; }
            public void NextTurn() { }
            public bool IsPlayerTurn(IPlayer player) => false;
        }

        /// <summary>A win condition whose outcome the test controls.</summary>
        private sealed class FakeWinCondition : IWinCondition
        {
            private readonly IPlayer _winner;
            public FakeWinCondition(IPlayer winner) { _winner = winner; }
            public int CheckCount { get; private set; }
            public WinConditionType Type => WinConditionType.EliminateAllEnemies;
            public bool Check(ICombatState gameState, out IPlayer winningPlayer)
            {
                CheckCount++;
                winningPlayer = _winner;
                return true;
            }
        }

        private HumanPlayer _human;

        [SetUp]
        public void SetUp()
        {
            _human = new HumanPlayer(1, "Player");
        }

        private CombatRoundEngine BuildEngine(RecordingFlow flow, IReadOnlyList<IWinCondition> winConditions = null)
        {
            return new CombatRoundEngine(
                actionValidator: null,
                actionExecutor: null,
                turnManager: new FakeTurnManager(),
                intentResolver: null,
                roundLifecycle: null,
                battlefieldFactory: null,
                hexConfig: null,
                winConditions: winConditions ?? new List<IWinCondition>(),
                flow: flow,
                logger: new FakeLogger());
        }

        private CombatState InitialState(RoundPhase roundPhase = RoundPhase.EnemyPlan)
        {
            return new CombatState(
                new List<IUnit>(),
                new List<IPlayer> { _human },
                _human,
                roundPhase: roundPhase);
        }

        private Unit Hero(int id)
        {
            return new Unit(id, _human, new HexCoordinates(0, 0), 50, 50, new List<IAbilityInstance>());
        }

        [Test]
        public void AddUnit_Null_IsIgnored()
        {
            var engine = BuildEngine(new RecordingFlow());
            engine.Initialize(InitialState(), new List<IPlayer> { _human }, CombatInitiator.Enemy);

            engine.AddUnit(null);

            Assert.AreEqual(0, engine.State.Units.Count, "a null unit is not added");
        }

        [Test]
        public void AddUnit_DuplicateId_IsSkipped()
        {
            var engine = BuildEngine(new RecordingFlow());
            engine.Initialize(InitialState(), new List<IPlayer> { _human }, CombatInitiator.Enemy);

            engine.AddUnit(Hero(7));
            engine.AddUnit(Hero(7));

            Assert.AreEqual(1, engine.State.Units.Count, "a second unit with the same id is skipped");
        }

        [Test]
        public void ResolveNextEnemyIntent_WrongPhase_ReturnsFalse_WithoutFinishing()
        {
            var flow = new RecordingFlow();
            var engine = BuildEngine(flow);
            // Phase becomes Combat in Initialize; RoundPhase stays PlayerAct (not the Resolve phase).
            engine.Initialize(InitialState(RoundPhase.PlayerAct), new List<IPlayer> { _human }, CombatInitiator.Enemy);

            var advanced = engine.ResolveNextEnemyIntent();

            Assert.IsFalse(advanced);
            Assert.AreEqual(0, flow.EnemyResolveFinishedCount, "outside the Resolve phase the flow is not finished");
        }

        [Test]
        public void ResolveNextEnemyIntent_NoIntentsLeft_FinishesThroughTheFlow()
        {
            var flow = new RecordingFlow();
            var engine = BuildEngine(flow);
            engine.Initialize(InitialState(RoundPhase.EnemyResolve), new List<IPlayer> { _human }, CombatInitiator.Enemy);

            var advanced = engine.ResolveNextEnemyIntent();

            Assert.IsFalse(advanced, "no committed intents remain");
            Assert.AreEqual(1, flow.EnemyResolveFinishedCount, "the engine hands the resolve-finished decision to the flow");
        }

        [Test]
        public void CheckWinConditions_Met_EndsTheGame_AndFiresOnce()
        {
            var flow = new RecordingFlow();
            var condition = new FakeWinCondition(_human);
            var engine = BuildEngine(flow, new List<IWinCondition> { condition });
            engine.Initialize(InitialState(), new List<IPlayer> { _human }, CombatInitiator.Enemy);

            var endedCount = 0;
            CombatPhase endedPhase = CombatPhase.Combat;
            engine.OnGameEnded += (winner, phase) => { endedCount++; endedPhase = phase; };

            engine.CheckWinConditions();
            // A second sweep (as Update()/EndRound() would) must not re-fire once the game is over.
            engine.CheckWinConditions();

            Assert.AreEqual(1, endedCount, "OnGameEnded fires exactly once");
            Assert.AreEqual(CombatPhase.Victory, endedPhase, "a winner ends in Victory");
            Assert.AreEqual(CombatPhase.Victory, engine.State.Phase);
            Assert.AreEqual(1, condition.CheckCount, "the ended-game guard stops re-evaluating the condition");
        }
    }
}
