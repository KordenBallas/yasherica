using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// D5 — a dead unit frees its cell the moment it dies: occupancy (GetUnitAt) reads alive
    /// units only, while identity lookups (GetUnit) still see the corpse so HP sync, win
    /// conditions, and the turn strip keep working.
    /// </summary>
    [TestFixture]
    public class CombatStateOccupancyTests
    {
        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private HumanPlayer _human;
        private AIPlayer _ai;

        [SetUp]
        public void SetUp()
        {
            _human = new HumanPlayer(1, "Player");
            _ai = new AIPlayer(100, "Enemy", new NoopAI());
        }

        private Unit UnitAt(int id, IPlayer owner, HexCoordinates position, int hp)
        {
            return new Unit(id, owner, position, hp, 30, new List<IAbilityInstance>());
        }

        private CombatState StateWith(params IUnit[] units)
        {
            return new CombatState(
                units.ToList(),
                new List<IPlayer> { _human, _ai },
                _human,
                phase: CombatPhase.Combat);
        }

        [Test]
        public void GetUnitAt_ReturnsNull_WhereOnlyACorpseStands()
        {
            var corpse = UnitAt(10, _ai, new HexCoordinates(1, 0), hp: 0);
            var state = StateWith(corpse);

            Assert.IsNull(state.GetUnitAt(new HexCoordinates(1, 0)),
                "a dead unit no longer occupies its cell");
        }

        [Test]
        public void GetUnitAt_ReturnsTheAliveUnit_WhenItStandsOnACorpseCell()
        {
            var corpse = UnitAt(10, _ai, new HexCoordinates(1, 0), hp: 0);
            var walker = UnitAt(1, _human, new HexCoordinates(1, 0), hp: 50);
            var state = StateWith(corpse, walker);

            var occupant = state.GetUnitAt(new HexCoordinates(1, 0));

            Assert.IsNotNull(occupant);
            Assert.AreEqual(1, occupant.Id, "the living occupant owns the cell, not the corpse");
        }

        [Test]
        public void GetUnit_StillReturnsTheDeadUnit_ById()
        {
            var corpse = UnitAt(10, _ai, new HexCoordinates(1, 0), hp: 0);
            var state = StateWith(corpse);

            var found = state.GetUnit(10);

            Assert.IsNotNull(found, "identity lookups keep seeing dead units (HP sync, win checks)");
            Assert.IsFalse(found.IsAlive);
        }
    }
}
