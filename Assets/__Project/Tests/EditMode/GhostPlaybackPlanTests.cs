using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the outcome→world mapping the ghost view consumes: caster position and volley
    /// look direction, one marker per struck unit with correct from/to world cells and
    /// numbers, and the against-the-current-board property (a new board → a new plan).
    /// </summary>
    [TestFixture]
    public class GhostPlaybackPlanTests
    {
        private HexDirectionConfig _config;
        private HumanPlayer _player;

        // Fake battlefield mapping: 2 world units per Q, 1 per R (distinct axes for assertions).
        private static Vector3 HexToWorld(HexCoordinates hex) => new Vector3(hex.Q * 2f, 0f, hex.R * 1f);

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _player = new HumanPlayer(1, "Player");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private Unit Caster()
        {
            return new Unit(1, _player, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance>());
        }

        [Test]
        public void Build_MapsCasterPositionAndVolleyDirection()
        {
            var outcome = new AbilityOutcome(new List<HexCoordinates>(), new List<UnitOutcome>());

            var plan = GhostPlaybackPlanBuilder.Build(outcome, Caster(), HexDirection.E, _config, HexToWorld);

            Assert.AreEqual(1, plan.CasterUnitId);
            Assert.AreEqual(HexToWorld(new HexCoordinates(0, 0)), plan.CasterPosition);
            Assert.AreEqual(Vector3.right, plan.CasterLookDirection, "east neighbor is +X in the fake mapping");
        }

        [Test]
        public void Build_RingOutcome_HasNoLookDirection()
        {
            var outcome = new AbilityOutcome(new List<HexCoordinates>(), new List<UnitOutcome>());

            var plan = GhostPlaybackPlanBuilder.Build(outcome, Caster(), null, _config, HexToWorld);

            Assert.AreEqual(Vector3.zero, plan.CasterLookDirection);
        }

        [Test]
        public void Build_CreatesMarkerPerStruckUnit_WithWorldFromTo()
        {
            var outcome = new AbilityOutcome(
                new List<HexCoordinates> { new HexCoordinates(1, 0), new HexCoordinates(2, 0) },
                new List<UnitOutcome>
                {
                    new UnitOutcome(10, 20, 0, new HexCoordinates(1, 0), new HexCoordinates(3, 0)),
                    new UnitOutcome(11, 20, 0, new HexCoordinates(2, 0), new HexCoordinates(2, 0))
                });

            var plan = GhostPlaybackPlanBuilder.Build(outcome, Caster(), HexDirection.E, _config, HexToWorld);

            Assert.AreEqual(2, plan.Markers.Count);
            var displaced = plan.Markers.Single(m => m.UnitId == 10);
            Assert.IsTrue(displaced.IsDisplaced);
            Assert.AreEqual(HexToWorld(new HexCoordinates(1, 0)), displaced.From);
            Assert.AreEqual(HexToWorld(new HexCoordinates(3, 0)), displaced.To);
            Assert.AreEqual(20, displaced.Damage);

            var inPlace = plan.Markers.Single(m => m.UnitId == 11);
            Assert.IsFalse(inPlace.IsDisplaced);
        }

        [Test]
        public void Build_AgainstADifferentBoard_YieldsADifferentPlan()
        {
            var firstOutcome = new AbilityOutcome(
                new List<HexCoordinates> { new HexCoordinates(1, 0) },
                new List<UnitOutcome>
                {
                    new UnitOutcome(10, 20, 0, new HexCoordinates(1, 0), new HexCoordinates(3, 0))
                });
            // The struck unit moved: the replay's outcome differs.
            var secondOutcome = new AbilityOutcome(
                new List<HexCoordinates> { new HexCoordinates(1, 0) },
                new List<UnitOutcome>());

            var firstPlan = GhostPlaybackPlanBuilder.Build(firstOutcome, Caster(), HexDirection.E, _config, HexToWorld);
            var secondPlan = GhostPlaybackPlanBuilder.Build(secondOutcome, Caster(), HexDirection.E, _config, HexToWorld);

            Assert.AreEqual(1, firstPlan.Markers.Count);
            Assert.IsEmpty(secondPlan.Markers, "replay reflects the current board");
        }
    }
}
