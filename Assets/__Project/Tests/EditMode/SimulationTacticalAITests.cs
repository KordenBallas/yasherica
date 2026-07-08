using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using Combat.Player.AI;
using Core.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the P2-4 decision quality: the AI aims lines at the facing that actually hits
    /// the most hostiles, secures kills, avoids friendly fire, values heals by missing HP,
    /// repositions when a move enables a better shot, stays deterministic under a seed, and
    /// degrades monotonically under the difficulty dials.
    /// </summary>
    [TestFixture]
    public class SimulationTacticalAITests
    {
        private const int DamageAbilityId = 100;
        private const int HealAbilityId = 101;
        private const int BaseDamage = 20;

        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class FakeCell : IHexCell
        {
            public FakeCell(HexCoordinates coordinates) { Coordinates = coordinates; }
            public HexCoordinates Coordinates { get; }
            public Vector3 WorldPosition => Vector3.zero;
            public HexCellStateMachine StateMachine => null;
            public bool IsActive { get; set; }
            public Color Color { get; set; }
#pragma warning disable 67
            public event Action<IHexCellState> OnStateChanged;
#pragma warning restore 67
            public void InitializeStateMachine(IHexCellState initialState) { }
            public void ChangeState(IHexCellState newState) { }
        }

        /// <summary>
        /// Hex-radius battlefield: all axial cells within the given cube distance of (0,0).
        /// </summary>
        private sealed class FakeBattlefield : IBattlefield
        {
            private readonly int _radius;

            public FakeBattlefield(int radius) { _radius = radius; }

            public bool IsActive => true;
            public float HexSize => 1f;
            public Vector3 Center => Vector3.zero;
            public IHexGrid Grid => null;

            public void Initialize(PlatformHexSurface surface, Vector3 center, HexDirectionConfig hexConfig) { }
            public void Activate() { }
            public void Deactivate() { }
            public void Clear() { }

            public IHexCell GetCellAt(HexCoordinates coordinates) => new FakeCell(coordinates);

            public IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range)
            {
                var cells = new List<IHexCell>();
                for (int q = center.Q - range; q <= center.Q + range; q++)
                for (int r = center.R - range; r <= center.R + range; r++)
                {
                    var cell = new HexCoordinates(q, r);
                    if (Distance(center, cell) <= range && IsCellInBoundary(cell))
                        cells.Add(new FakeCell(cell));
                }
                return cells;
            }

            public IReadOnlyList<HexCoordinates> GetCellsInBoundary() =>
                throw new NotSupportedException();

            public Vector3 HexToWorld(HexCoordinates hex) => Vector3.zero;
            public HexCoordinates WorldToHex(Vector3 world) => new HexCoordinates(0, 0);

            public bool IsCellInBoundary(HexCoordinates hex) =>
                Distance(new HexCoordinates(0, 0), hex) <= _radius;

            public IReadOnlyList<IHexCell> GetCellsBySelection(
                CellSelectionType selectionType, CellSelectionParams parameters) =>
                throw new NotSupportedException();

            private static int Distance(HexCoordinates from, HexCoordinates to)
            {
                int dq = Math.Abs(from.Q - to.Q);
                int dr = Math.Abs(from.R - to.R);
                int ds = Math.Abs((from.Q + from.R) - (to.Q + to.R));
                return (dq + dr + ds) / 2;
            }
        }

        private HexDirectionConfig _config;
        private AbilityOutcomeCalculator _calculator;
        private HumanPlayer _human;
        private AIPlayer _enemyOwner;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _calculator = new AbilityOutcomeCalculator(
                new AbilityShapeCalculator(_config), new DamageSystem(), _config);
            _human = new HumanPlayer(1, "Player");
            _enemyOwner = new AIPlayer(100, "Enemy", null);
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineDamage(int length = 2, int damage = BaseDamage)
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                DamageAbilityId, "Test Line", 1, AbilityShapeData.ForLine(length), damage));
        }

        private static IAbilityInstance RingHeal(int amount)
        {
            return new AbilityInstance(new DataDrivenHealAbility(
                HealAbilityId, "Test Heal", 1, AbilityShapeData.ForRing(1), amount));
        }

        private Unit Enemy(int id, HexCoordinates position, int hp = 30, params IAbilityInstance[] abilities)
        {
            return new Unit(id, _enemyOwner, position, hp, 30, abilities.ToList());
        }

        private Unit Hero(int id, HexCoordinates position, int currentHp = 50, int maxHp = 50)
        {
            return new Unit(id, _human, position, currentHp, maxHp, new List<IAbilityInstance>());
        }

        private CombatState StateWith(IBattlefield battlefield, params IUnit[] units)
        {
            var players = new List<IPlayer> { _human };
            players.AddRange(units.Select(u => u.Owner).Distinct().Where(p => p != _human));
            return new CombatState(units.ToList(), players, _human,
                phase: CombatPhase.Combat, battlefield: battlefield);
        }

        private SimulationTacticalAI MakeAI(
            AIBehaviorProfile profile = null,
            AIDifficultySettings difficulty = null,
            IHostilityPolicy policy = null,
            int seed = 1234)
        {
            return new SimulationTacticalAI(
                AITuning.Compose(profile ?? AIBehaviorProfile.Default, difficulty ?? AIDifficultySettings.Neutral),
                _calculator,
                policy ?? new TeamHostilityPolicy(),
                seed,
                new FakeLogger());
        }

        // (a) Facing selection is real: the line fires where the hostiles actually are.
        [Test]
        public void AimsLine_AtTheFacingHittingTheMostHostiles()
        {
            var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
            var state = StateWith(null,
                enemy,
                Hero(1, new HexCoordinates(1, 0)),   // E, in line
                Hero(2, new HexCoordinates(2, 0)),   // E, in line
                Hero(3, new HexCoordinates(0, -1))); // NW, alone

            var action = MakeAI().DecideAction(state, enemy);

            var schedule = action as ScheduleAbilityAction;
            Assert.IsNotNull(schedule, "a two-hit line beats everything else");
            Assert.AreEqual(HexDirection.E, schedule.FacingToSet);
        }

        // (b) Kill-securing beats spreading damage — and the KillBonus dial controls it.
        [Test]
        public void SecuresTheKill_WhenKillBonusIsOn_SpreadsDamage_WhenItIsOff()
        {
            var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
            var state = StateWith(null,
                enemy,
                Hero(1, new HexCoordinates(1, 0), currentHp: 5),  // E: killable
                Hero(2, new HexCoordinates(-1, 0)),               // W: two full-HP targets
                Hero(3, new HexCoordinates(-2, 0)));

            var killAction = MakeAI().DecideAction(state, enemy) as ScheduleAbilityAction;
            var noBonusAi = MakeAI(new AIBehaviorProfile(killBonus: 0f));
            var spreadAction = noBonusAi.DecideAction(state, enemy) as ScheduleAbilityAction;

            Assert.IsNotNull(killAction);
            Assert.AreEqual(HexDirection.E, killAction.FacingToSet, "default kill bonus secures the kill");
            Assert.IsNotNull(spreadAction);
            Assert.AreEqual(HexDirection.W, spreadAction.FacingToSet, "without kill bonus two hits win");
        }

        // (c) Friendly fire: a clean line beats a line through an ally; the penalty dial and
        // the hostility policy both flip the choice.
        [Test]
        public void AvoidsFriendlyFire_UnlessThePenaltyIsOff()
        {
            var allyOwner = new AIPlayer(101, "Other enemy", null);
            var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
            var ally = new Unit(11, allyOwner, new HexCoordinates(1, 0), 30, 30,
                new List<IAbilityInstance>());
            var state = StateWith(null,
                enemy,
                ally,                                              // E, in the way
                Hero(1, new HexCoordinates(2, 0), currentHp: 30),  // E, behind the ally
                Hero(2, new HexCoordinates(-1, 0)));               // W, clean shot

            var careful = MakeAI().DecideAction(state, enemy) as ScheduleAbilityAction;
            var reckless = MakeAI(new AIBehaviorProfile(friendlyFirePenaltyWeight: 0f))
                .DecideAction(state, enemy) as ScheduleAbilityAction;

            Assert.IsNotNull(careful);
            Assert.AreEqual(HexDirection.W, careful.FacingToSet, "the ally blocks the east shot");
            Assert.IsNotNull(reckless);
            Assert.AreEqual(HexDirection.E, reckless.FacingToSet,
                "without the penalty the wounded east target is worth more");
        }

        [Test]
        public void FreeForAllPolicy_TreatsTheFellowEnemyAsATarget()
        {
            var allyOwner = new AIPlayer(101, "Other enemy", null);
            var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
            var fellow = new Unit(11, allyOwner, new HexCoordinates(1, 0), 30, 30,
                new List<IAbilityInstance>());
            var state = StateWith(null,
                enemy,
                fellow,                                            // E
                Hero(1, new HexCoordinates(2, 0), currentHp: 30),  // E
                Hero(2, new HexCoordinates(-1, 0)));               // W

            var action = MakeAI(policy: new FreeForAllHostilityPolicy())
                .DecideAction(state, enemy) as ScheduleAbilityAction;

            Assert.IsNotNull(action);
            Assert.AreEqual(HexDirection.E, action.FacingToSet,
                "in FFA the east line hits two targets");
        }

        // (d) Determinism: same seed + same state → identical pick even with noise and top-N.
        [Test]
        public void SameSeed_SameState_YieldsTheSamePick_EvenWithNoise()
        {
            var noisyProfile = new AIBehaviorProfile(scoreNoise: 15f, pickFromTopN: 3);
            ScheduleAbilityAction DecideOnce()
            {
                var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
                var state = StateWith(null,
                    enemy,
                    Hero(1, new HexCoordinates(1, 0)),
                    Hero(2, new HexCoordinates(2, 0)),
                    Hero(3, new HexCoordinates(0, -1)));
                return MakeAI(noisyProfile, seed: 777).DecideAction(state, enemy) as ScheduleAbilityAction;
            }

            var first = DecideOnce();
            var second = DecideOnce();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreEqual(first.AbilityId, second.AbilityId);
            Assert.AreEqual(first.FacingToSet, second.FacingToSet);
        }

        // (e) Difficulty dials degrade decision quality monotonically.
        [Test]
        public void EasyDials_PickTheOptimalActionLessOftenThanNeutral()
        {
            const int seeds = 200;
            var easy = new AIDifficultySettings(
                extraScoreNoise: 25f, extraTopN: 2, extraMistakeChance: 0.25f);

            int neutralOptimal = CountOptimalPicks(AIDifficultySettings.Neutral, seeds);
            int easyOptimal = CountOptimalPicks(easy, seeds);

            Assert.AreEqual(seeds, neutralOptimal, "perfect dials always pick the clear best");
            Assert.Less(easyOptimal, neutralOptimal, "easy dials must cost decision quality");
        }

        [Test]
        public void CertainMistake_AbandonsTheArgmax()
        {
            const int seeds = 50;
            var alwaysWrongProfile = new AIBehaviorProfile(mistakeChance: 1f);

            int optimal = 0;
            for (int seed = 0; seed < seeds; seed++)
            {
                var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
                var state = StateWith(null,
                    enemy,
                    Hero(1, new HexCoordinates(1, 0)),
                    Hero(2, new HexCoordinates(2, 0)),
                    Hero(3, new HexCoordinates(0, -1)));

                if (IsOptimalPick(MakeAI(alwaysWrongProfile, seed: seed).DecideAction(state, enemy)))
                    optimal++;
            }

            Assert.Less(optimal, seeds, "a certain mistake cannot reproduce argmax on every seed");
        }

        private int CountOptimalPicks(AIDifficultySettings difficulty, int seeds)
        {
            int optimal = 0;
            for (int seed = 0; seed < seeds; seed++)
            {
                var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
                var state = StateWith(null,
                    enemy,
                    Hero(1, new HexCoordinates(1, 0)),
                    Hero(2, new HexCoordinates(2, 0)),
                    Hero(3, new HexCoordinates(0, -1)));

                if (IsOptimalPick(MakeAI(difficulty: difficulty, seed: seed).DecideAction(state, enemy)))
                    optimal++;
            }
            return optimal;
        }

        private static bool IsOptimalPick(IAction action)
        {
            return action is ScheduleAbilityAction schedule
                && schedule.FacingToSet == HexDirection.E;
        }

        // (g) Heals are worth only the HP they restore.
        [Test]
        public void HealsTheWoundedAlly_ButNotTheFullOne()
        {
            var allyOwner = new AIPlayer(101, "Other enemy", null);
            Unit Ally(int hp) => new Unit(11, allyOwner, new HexCoordinates(1, 0), hp, 30,
                new List<IAbilityInstance>());
            CombatState StateWithAlly(Unit enemy, Unit ally) => StateWith(null,
                enemy, ally, Hero(1, new HexCoordinates(5, 0))); // hero out of line reach

            var enemyA = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage(), RingHeal(15));
            var healAction = MakeAI().DecideAction(StateWithAlly(enemyA, Ally(hp: 5)), enemyA);

            var enemyB = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage(), RingHeal(15));
            var idleAction = MakeAI().DecideAction(StateWithAlly(enemyB, Ally(hp: 30)), enemyB);

            var heal = healAction as ScheduleAbilityAction;
            Assert.IsNotNull(heal, "a wounded ally makes the heal worth casting");
            Assert.AreEqual(HealAbilityId, heal.AbilityId);
            Assert.IsFalse(idleAction is ScheduleAbilityAction,
                "healing a full-HP ally is worthless — never cast it");
        }

        // (h) Move lookahead: repositioning that enables a hit beats idling and blank casts.
        [Test]
        public void MovesTowardTheShot_WhenNothingIsHittableFromHere()
        {
            var battlefield = new FakeBattlefield(radius: 4);
            var enemy = Enemy(10, new HexCoordinates(0, 0), 30, LineDamage());
            var state = StateWith(battlefield,
                enemy,
                Hero(1, new HexCoordinates(4, 0))); // out of line-2 reach from (0,0)

            var action = MakeAI().DecideAction(state, enemy);

            var move = action as MoveAction;
            Assert.IsNotNull(move, "the AI must reposition, not idle or fire blanks");
            Assert.AreEqual(new HexCoordinates(3, 0), move.TargetPosition,
                "the best reachable cell: adjacent, with the east line hitting the hero next round");
        }
    }
}
