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
    /// Proves the locked-intent Resolve semantics: committed abilities fire at their
    /// committed cells (whiffing after a dodge, hitting whoever stands there NOW),
    /// committed moves fizzle when blocked, dead casters are skipped, never re-target.
    /// </summary>
    [TestFixture]
    public class EnemyIntentResolverTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int EnemyUnitId = 10;
        private const int HeroUnitId = 1;
        private const int AbilityId = 100;
        private const int BaseDamage = 10;

        private HexDirectionConfig _config;
        private EnemyIntentResolver _resolver;
        private HumanPlayer _human;
        private AIPlayer _enemyOwner;
        private AbilityShapeCalculator _shapeCalculator;

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            _resolver = new EnemyIntentResolver(
                new AbilityExecutor(new DamageSystem(), null, _shapeCalculator, _config),
                new FakeLogger());
            _human = new HumanPlayer(1, "Player");
            _enemyOwner = new AIPlayer(100, "Enemy", new NoopAI());
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineAbility()
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 2, AbilityShapeData.ForLine(2), BaseDamage));
        }

        private Unit Enemy(HexCoordinates position, int hp = 30)
        {
            return new Unit(EnemyUnitId, _enemyOwner, position, hp, 30,
                new List<IAbilityInstance> { LineAbility() });
        }

        private Unit Hero(HexCoordinates position)
        {
            return new Unit(HeroUnitId, _human, position, 50, 50, new List<IAbilityInstance>());
        }

        private CombatState StateWith(params IUnit[] units)
        {
            return new CombatState(
                units.ToList(),
                new List<IPlayer> { _human, _enemyOwner },
                _human,
                phase: CombatPhase.Combat);
        }

        /// <summary>
        /// Committed intent: ability fired west from (2,0) → cells (1,0), (0,0).
        /// </summary>
        private EnemyIntent CommittedWestCast(Unit enemy)
        {
            var action = new ScheduleAbilityAction(_enemyOwner, enemy.Id, AbilityId, HexDirection.W);
            var cells = _shapeCalculator.GetAffectedCells(
                enemy.Abilities[0].Ability.Shape, enemy.Position, HexDirection.W, _ => true);
            return new EnemyIntent(enemy.Id, action, HexDirection.W, enemy.Position, cells);
        }

        [Test]
        public void CommittedCast_Whiffs_WhenTheTargetDodged()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            var hero = Hero(new HexCoordinates(1, 0));
            var intent = CommittedWestCast(enemy);

            // The player moved out of the committed line before resolve.
            var dodgedHero = hero.WithPosition(new HexCoordinates(2, 1));
            var state = StateWith(enemy, dodgedHero);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(50, resolved.GetUnit(HeroUnitId).CurrentHP, "dodged blow whiffs");
        }

        [Test]
        public void CommittedCast_HitsWhoeverStandsInTheCommittedCellsNow()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            // The hero moved INTO the committed line after the plan was revealed (bait gone wrong).
            var hero = Hero(new HexCoordinates(0, 0));
            var intent = CommittedWestCast(enemy);
            var state = StateWith(enemy, hero);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(50 - BaseDamage, resolved.GetUnit(HeroUnitId).CurrentHP);
        }

        [Test]
        public void CommittedCast_FiresAtCommittedCells_EvenIfTheCasterWasDisplaced()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            var hero = Hero(new HexCoordinates(1, 0));
            var intent = CommittedWestCast(enemy);

            // The enemy itself was pushed after planning; the cells stay committed.
            var displacedEnemy = enemy.WithPosition(new HexCoordinates(4, 0));
            var state = StateWith(displacedEnemy, hero);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(50 - BaseDamage, resolved.GetUnit(HeroUnitId).CurrentHP,
                "the blow lands where it was shown, not where the enemy now stands");
        }

        [Test]
        public void CommittedCast_SetsFacingAndStartsCooldown()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            var hero = Hero(new HexCoordinates(1, 0));
            var intent = CommittedWestCast(enemy);
            var state = StateWith(enemy, hero);

            var resolved = _resolver.Resolve(state, intent);

            var resolvedEnemy = resolved.GetUnit(EnemyUnitId);
            Assert.AreEqual(HexDirection.W, resolvedEnemy.FacingDirection, "committed facing applied");
            Assert.IsFalse(resolvedEnemy.GetAbility(AbilityId).IsAvailable, "cooldown started");
        }

        [Test]
        public void DeadCaster_IsSkipped()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), hp: 0);
            var hero = Hero(new HexCoordinates(1, 0));
            var intent = CommittedWestCast(enemy);
            var state = StateWith(enemy, hero);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(50, resolved.GetUnit(HeroUnitId).CurrentHP, "dead enemy does not fire");
        }

        [Test]
        public void CommittedMove_Fizzles_WhenDestinationIsOccupied()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            var hero = Hero(new HexCoordinates(1, 0));
            var move = new MoveAction(_enemyOwner, enemy.Id, new HexCoordinates(1, 0));
            var intent = new EnemyIntent(enemy.Id, move, null, enemy.Position, null);
            var state = StateWith(enemy, hero);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(new HexCoordinates(2, 0), resolved.GetUnit(EnemyUnitId).Position,
                "blocked committed move does not execute and does not re-target");
        }

        [Test]
        public void CommittedMove_Executes_WhenOnlyACorpseHoldsTheDestination()
        {
            // D5: a dead unit frees its cell instantly, so a committed move onto the corpse's
            // cell resolves instead of fizzling on a blocker that is no longer there.
            var enemy = Enemy(new HexCoordinates(2, 0));
            var corpse = Hero(new HexCoordinates(1, 0)).WithHP(0);
            var move = new MoveAction(_enemyOwner, enemy.Id, new HexCoordinates(1, 0));
            var intent = new EnemyIntent(enemy.Id, move, null, enemy.Position, null);
            var state = StateWith(enemy, corpse);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(new HexCoordinates(1, 0), resolved.GetUnit(EnemyUnitId).Position,
                "a corpse does not block a committed move");
        }

        [Test]
        public void CommittedMove_Executes_WhenDestinationIsFree()
        {
            var enemy = Enemy(new HexCoordinates(2, 0));
            var move = new MoveAction(_enemyOwner, enemy.Id, new HexCoordinates(1, 0));
            var intent = new EnemyIntent(enemy.Id, move, null, enemy.Position, null);
            var state = StateWith(enemy);

            var resolved = _resolver.Resolve(state, intent);

            Assert.AreEqual(new HexCoordinates(1, 0), resolved.GetUnit(EnemyUnitId).Position);
        }
    }
}
