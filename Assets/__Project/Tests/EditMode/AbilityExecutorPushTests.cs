using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the push displacement: damage lands first, survivors are shoved away from the
    /// caster along the line direction, farthest-first so pushed units never collide, and
    /// the committed (ExecuteAbilityAtCells) path behaves identically to the live path.
    /// </summary>
    [TestFixture]
    public class AbilityExecutorPushTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int CasterId = 1;
        private const int NearVictimId = 2;
        private const int FarVictimId = 3;
        private const int PushAbilityId = 200;
        private const int BaseDamage = 20;
        private const int PushDistance = 2;

        private HexDirectionConfig _config;
        private HumanPlayer _player;
        private HumanPlayer _enemyOwner;
        private AbilityExecutor _executor;
        private AbilityShapeCalculator _shapeCalculator;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            _executor = new AbilityExecutor(new DamageSystem(), null, _shapeCalculator, _config);
            _player = new HumanPlayer(1, "Player");
            _enemyOwner = new HumanPlayer(2, "Enemies");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance PushAbility(int lineLength = 2)
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                PushAbilityId, "Test Push", 1, AbilityShapeData.ForLine(lineLength), BaseDamage, PushDistance));
        }

        private Unit Caster(IAbilityInstance ability)
        {
            return new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance> { ability },
                new List<ScheduledAbility> { new ScheduledAbility(ability, 0) });
        }

        private Unit Victim(int id, HexCoordinates position, int hp = 50)
        {
            return new Unit(id, _enemyOwner, position, hp, 50, new List<IAbilityInstance>());
        }

        private CombatState StateWith(params IUnit[] units)
        {
            return new CombatState(units.ToList(),
                new List<IPlayer> { _player, _enemyOwner },
                _player, phase: CombatPhase.Combat);
        }

        [Test]
        public void Push_DamagesThenDisplacesAwayFromCaster()
        {
            var ability = PushAbility();
            var caster = Caster(ability); // facing E by default
            var victim = Victim(NearVictimId, new HexCoordinates(1, 0));
            var state = StateWith(caster, victim);

            var result = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            var pushed = result.GetUnit(NearVictimId);
            Assert.AreEqual(50 - BaseDamage, pushed.CurrentHP, "damage applied");
            Assert.AreEqual(new HexCoordinates(3, 0), pushed.Position, "pushed 2 cells east");
        }

        [Test]
        public void Push_FarthestFirst_BothStruckUnitsMoveWithoutColliding()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var near = Victim(NearVictimId, new HexCoordinates(1, 0));
            var far = Victim(FarVictimId, new HexCoordinates(2, 0));
            var state = StateWith(caster, near, far);

            var result = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            // Far victim moves first (2,0)→(4,0), clearing the lane for the near one (1,0)→(3,0).
            Assert.AreEqual(new HexCoordinates(4, 0), result.GetUnit(FarVictimId).Position);
            Assert.AreEqual(new HexCoordinates(3, 0), result.GetUnit(NearVictimId).Position);
        }

        [Test]
        public void Push_DeadVictim_IsNotDisplaced()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var fragile = Victim(NearVictimId, new HexCoordinates(1, 0), hp: BaseDamage); // dies to the hit
            var state = StateWith(caster, fragile);

            var result = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            var corpse = result.GetUnit(NearVictimId);
            Assert.IsFalse(corpse.IsAlive);
            Assert.AreEqual(new HexCoordinates(1, 0), corpse.Position, "corpses stay where they fell");
        }

        [Test]
        public void Push_ThroughCommittedPath_MatchesLivePath()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var victim = Victim(NearVictimId, new HexCoordinates(1, 0));
            var state = StateWith(caster, victim);

            var committedCells = _shapeCalculator.GetAffectedCells(
                ability.Ability.Shape, caster.Position, HexDirection.E, state.IsPositionValid);

            var result = _executor.ExecuteAbilityAtCells(
                state, caster, ability.Ability, committedCells, HexDirection.E);

            var pushed = result.GetUnit(NearVictimId);
            Assert.AreEqual(50 - BaseDamage, pushed.CurrentHP);
            Assert.AreEqual(new HexCoordinates(3, 0), pushed.Position);
        }

        [Test]
        public void Push_StopsAtBystander_NeverStacksUnits()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var victim = Victim(NearVictimId, new HexCoordinates(1, 0));
            // A bystander OUTSIDE the affected line (line length 2 → cells (1,0),(2,0)) blocks at (3,0).
            var bystander = Victim(FarVictimId, new HexCoordinates(3, 0));
            var state = StateWith(caster, victim, bystander);

            var result = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            Assert.AreEqual(new HexCoordinates(2, 0), result.GetUnit(NearVictimId).Position,
                "push stops before the occupied cell");
            Assert.AreEqual(new HexCoordinates(3, 0), result.GetUnit(FarVictimId).Position,
                "bystander outside the line is not displaced");
        }
    }
}
