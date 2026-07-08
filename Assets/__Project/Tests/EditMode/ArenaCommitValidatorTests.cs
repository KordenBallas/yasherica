using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// X2 anti-cheat: the host-side commit validator against the canonical round-start state —
    /// ownership spoofing, dead/stunned actors, out-of-range moves, unknown/cooling abilities,
    /// volley budgets, and (the load-bearing one) tampered committed cells are all rejected;
    /// legal commits and empty commitments pass.
    /// </summary>
    [TestFixture]
    public class ArenaCommitValidatorTests
    {
        private const int AbilityId = 100;
        private const int MaxHp = 30;
        private const int MaxQueueSize = 3;

        private HexDirectionConfig _config;
        private ArenaCommitBuilder _builder;
        private ArenaCommitValidator _validator;
        private HumanPlayer _p1;
        private HumanPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _builder = new ArenaCommitBuilder(new AbilityShapeCalculator(_config));
            _validator = new ArenaCommitValidator(_builder);
            _p1 = new HumanPlayer(1, "P1");
            _p2 = new HumanPlayer(2, "P2");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineAbility(int cooldown = 0) =>
            new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 2, AbilityShapeData.ForLine(2), 10), cooldown);

        private CombatState State(
            int unit1Hp = MaxHp, int abilityCooldown = 0, IReadOnlyList<IStatusEffect> unit1Effects = null)
        {
            var units = new List<IUnit>
            {
                new Unit(1, _p1, new HexCoordinates(0, 0), unit1Hp, MaxHp,
                    new List<IAbilityInstance> { LineAbility(abilityCooldown) },
                    statusEffects: unit1Effects, facingDirection: HexDirection.E),
                new Unit(2, _p2, new HexCoordinates(4, 0), MaxHp, MaxHp,
                    new List<IAbilityInstance> { LineAbility() }, facingDirection: HexDirection.W)
            };

            return new CombatState(units, new List<IPlayer> { _p1, _p2 }, _p1,
                turnNumber: 2, CombatPhase.Combat, roundPhase: RoundPhase.PlayerAct);
        }

        private ArenaCommit LegalVolley(CombatState state)
        {
            var unit = (state.GetUnit(1) as Unit).WithAbilityQueue(new List<ScheduledAbility>
            {
                new ScheduledAbility(state.GetUnit(1).GetAbility(AbilityId), 0)
            });
            return _builder.FromTerminalAction(
                state, unit, new ExecuteAbilityQueueAction(_p1, 1));
        }

        private ArenaCommitVerdict Validate(ICombatState state, ArenaCommit commit, int credits = 1)
            => _validator.Validate(state, commit, MaxQueueSize, credits);

        // ---- accepts ----

        [Test]
        public void EmptyCommit_AlwaysAccepted()
        {
            var verdict = Validate(State(), new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>()));
            Assert.IsTrue(verdict.IsValid);
        }

        [Test]
        public void LegalMove_Accepted()
        {
            var state = State();
            var commit = _builder.FromTerminalAction(
                state, state.GetUnit(1), new MoveAction(_p1, 1, new HexCoordinates(1, 0)));

            Assert.IsTrue(Validate(state, commit).IsValid);
        }

        [Test]
        public void LegalVolley_WithBankedCredit_Accepted()
        {
            var state = State();
            Assert.IsTrue(Validate(state, LegalVolley(state), credits: 1).IsValid);
        }

        // ---- rejects ----

        [Test]
        public void SpoofedOwnership_Rejected()
        {
            var state = State();
            // Player 2 claims player 1's unit.
            var commit = new ArenaCommit(2, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new MoveAction(_p2, 1, new HexCoordinates(1, 0)),
                    null, new HexCoordinates(0, 0), null)
            });

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void DeadUnit_Rejected()
        {
            var state = State(unit1Hp: 0);
            var commit = new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new MoveAction(_p1, 1, new HexCoordinates(1, 0)),
                    null, new HexCoordinates(0, 0), null)
            });

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void StunnedUnit_OnlyEmptyCommitLegal()
        {
            var stun = new DataDrivenControlEffect(3, "Stun", 2, ControlKind.Stun, 0,
                StatusEffectTriggerType.TurnEnd, stackCount: 1, stackRule: StackRule.Refresh, maxStacks: 1);
            var state = State(unit1Effects: new List<IStatusEffect> { stun });

            var move = new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new MoveAction(_p1, 1, new HexCoordinates(1, 0)),
                    null, new HexCoordinates(0, 0), null)
            });
            Assert.IsFalse(Validate(state, move).IsValid, "a stunned unit cannot commit steps");
            Assert.IsTrue(Validate(state,
                new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>())).IsValid,
                "the stunned wait (empty commitment) stays legal");
        }

        [Test]
        public void Move_BeyondEffectiveRange_Rejected()
        {
            var state = State();
            var commit = new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new MoveAction(_p1, 1, new HexCoordinates(9, 0)),
                    null, new HexCoordinates(0, 0), null)
            });

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void Move_WithWrongOrigin_Rejected()
        {
            var state = State();
            // Claims to start beside the target — teleport by origin forgery.
            var commit = new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new MoveAction(_p1, 1, new HexCoordinates(4, 1)),
                    null, new HexCoordinates(4, 0), null)
            });

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void UnknownAbility_Rejected()
        {
            var state = State();
            var commit = new ArenaCommit(1, 1, HexDirection.E, new List<EnemyIntent>
            {
                new EnemyIntent(1, new ScheduleAbilityAction(_p1, 1, 999, HexDirection.E),
                    HexDirection.E, new HexCoordinates(0, 0), new List<HexCoordinates>())
            });

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void AbilityOnCooldown_Rejected()
        {
            var state = State(abilityCooldown: 2);
            var commit = LegalVolley(State());

            Assert.IsFalse(Validate(state, commit).IsValid);
        }

        [Test]
        public void TamperedCommittedCells_Rejected()
        {
            var state = State();
            var legal = LegalVolley(state);
            var step = legal.Steps[0];

            // Same ability, but the "committed" cells were redirected onto the victim's actual
            // position behind its back.
            var forgedCells = new List<HexCoordinates> { new HexCoordinates(4, 0) };
            var forged = new ArenaCommit(1, 1, legal.FinalFacing, new List<EnemyIntent>
            {
                new EnemyIntent(step.UnitId, step.Action, step.CommittedFacing,
                    step.CommittedOrigin, forgedCells)
            });

            Assert.IsFalse(Validate(state, forged).IsValid,
                "re-derived cells must match the submitted ones exactly");
        }

        [Test]
        public void VolleyBeyondQueueBudget_Rejected()
        {
            var state = State();
            var single = LegalVolley(state);
            var fourSteps = Enumerable.Repeat(single.Steps[0], MaxQueueSize + 1).ToList();
            var oversized = new ArenaCommit(1, 1, single.FinalFacing, fourSteps);

            Assert.IsFalse(Validate(state, oversized, credits: 10).IsValid,
                "the queue-size cap holds");
            Assert.IsFalse(Validate(state, single, credits: 0).IsValid,
                "a volley out of nowhere (no banked scheduling rounds) is rejected");
        }

        [Test]
        public void MoveMixedWithAbilities_Rejected()
        {
            var state = State();
            var volley = LegalVolley(state);
            var move = new EnemyIntent(1, new MoveAction(_p1, 1, new HexCoordinates(1, 0)),
                null, new HexCoordinates(0, 0), null);
            var mixed = new ArenaCommit(1, 1, volley.FinalFacing,
                new List<EnemyIntent> { move, volley.Steps[0] });

            Assert.IsFalse(Validate(state, mixed).IsValid);
        }
    }
}
