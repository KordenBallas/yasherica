using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class UnitFacingTests
    {
        private static Unit MakeUnit()
        {
            var owner = new HumanPlayer(1, "Player");
            return new Unit(
                id: 10,
                owner: owner,
                position: new HexCoordinates(2, 3),
                currentHP: 40,
                maxHP: 50,
                abilities: new List<IAbilityInstance>(),
                hasActedThisTurn: true);
        }

        [Test]
        public void Facing_DefaultsToEast()
        {
            var unit = MakeUnit();

            Assert.AreEqual(HexDirection.E, unit.FacingDirection);
        }

        [Test]
        public void WithFacingDirection_ChangesOnlyFacing()
        {
            var unit = MakeUnit();

            var turned = unit.WithFacingDirection(HexDirection.NW);

            Assert.AreEqual(HexDirection.NW, turned.FacingDirection);
            Assert.AreEqual(unit.Id, turned.Id);
            Assert.AreEqual(unit.Position, turned.Position);
            Assert.AreEqual(unit.CurrentHP, turned.CurrentHP);
            Assert.AreEqual(unit.MaxHP, turned.MaxHP);
            Assert.AreEqual(unit.HasActedThisTurn, turned.HasActedThisTurn);
        }

        [Test]
        public void OtherWithMethods_PreserveFacing()
        {
            var unit = MakeUnit().WithFacingDirection(HexDirection.SW);

            Assert.AreEqual(HexDirection.SW, unit.WithPosition(new HexCoordinates(0, 0)).FacingDirection);
            Assert.AreEqual(HexDirection.SW, unit.WithHP(1).FacingDirection);
            Assert.AreEqual(HexDirection.SW, unit.WithActedThisTurn(false).FacingDirection);
            Assert.AreEqual(HexDirection.SW, unit.WithAbilityQueue(new List<ScheduledAbility>()).FacingDirection);
            Assert.AreEqual(HexDirection.SW, unit.WithStatusEffects(new List<IStatusEffect>()).FacingDirection);
            Assert.AreEqual(HexDirection.SW, unit.WithAbilities(new List<IAbilityInstance>()).FacingDirection);
        }
    }
}
