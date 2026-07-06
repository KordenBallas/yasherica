using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Integration;
using Combat.Player;
using UnityEngine;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// D6 — the one spawn-placement rule: every unit lands on the closest FREE in-boundary cell
    /// to its own world position, so sequentially integrated units (hero, boss, crew) can never
    /// share a cell. This is the invariant whose violation stacked the bandit crew.
    /// </summary>
    [TestFixture]
    public class SpawnCellResolverTests
    {
        /// <summary>
        /// Minimal square-lattice battlefield: cell (Q,R) sits at world (Q, 0, R).
        /// Only the members the resolver touches are implemented.
        /// </summary>
        private sealed class FakeBattlefield : IBattlefield
        {
            private readonly HashSet<HexCoordinates> _cells;

            public FakeBattlefield(int radius)
            {
                _cells = new HashSet<HexCoordinates>();
                for (int q = -radius; q <= radius; q++)
                for (int r = -radius; r <= radius; r++)
                    _cells.Add(new HexCoordinates(q, r));
            }

            public bool IsActive => true;
            public float HexSize => 1f;
            public Vector3 Center => Vector3.zero;
            public IHexGrid Grid => null;

            public void Initialize(PlatformHexSurface surface, Vector3 center, HexDirectionConfig hexConfig) { }
            public void Activate() { }
            public void Deactivate() { }
            public void Clear() { }

            public IHexCell GetCellAt(HexCoordinates coordinates) => null;
            public IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range) => new List<IHexCell>();
            public IReadOnlyList<HexCoordinates> GetCellsInBoundary() => _cells.ToList();
            public Vector3 HexToWorld(HexCoordinates hex) => new Vector3(hex.Q, 0f, hex.R);
            public HexCoordinates WorldToHex(Vector3 world) =>
                new HexCoordinates(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
            public bool IsCellInBoundary(HexCoordinates hex) => _cells.Contains(hex);
            public IReadOnlyList<IHexCell> GetCellsBySelection(CellSelectionType selectionType, CellSelectionParams parameters) =>
                throw new NotSupportedException();
        }

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private FakeBattlefield _battlefield;
        private HumanPlayer _human;
        private AIPlayer _ai;

        [SetUp]
        public void SetUp()
        {
            _battlefield = new FakeBattlefield(radius: 3);
            _human = new HumanPlayer(1, "Player");
            _ai = new AIPlayer(100, "Enemy", new NoopAI());
        }

        private Unit UnitAt(int id, IPlayer owner, HexCoordinates position, int hp = 30)
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
        public void ResolvesToTheOwnCell_WhenItIsFree()
        {
            var cell = SpawnCellResolver.Resolve(new Vector3(1f, 0f, 1f), _battlefield, StateWith());

            Assert.AreEqual(new HexCoordinates(1, 1), cell);
        }

        [Test]
        public void SkipsTheOccupiedCell_ToTheNearestFreeOne()
        {
            var state = StateWith(UnitAt(1, _human, new HexCoordinates(1, 1)));

            var cell = SpawnCellResolver.Resolve(new Vector3(1f, 0f, 1f), _battlefield, state);

            Assert.AreNotEqual(new HexCoordinates(1, 1), cell, "an occupied cell is never assigned");
            Assert.IsTrue(_battlefield.IsCellInBoundary(cell));
            Assert.AreEqual(1f, Vector3.Distance(new Vector3(1f, 0f, 1f), _battlefield.HexToWorld(cell)),
                "the replacement is a nearest neighbour");
        }

        [Test]
        public void SequentialPlacementsFromTheSameSpot_NeverShareACell()
        {
            // The bandit-camp regression: several crew members integrated one after another,
            // all resolving from (nearly) the same world position.
            var placed = new List<IUnit>();
            var seen = new HashSet<HexCoordinates>();

            for (int i = 0; i < 4; i++)
            {
                var state = StateWith(placed.ToArray());
                var cell = SpawnCellResolver.Resolve(new Vector3(0f, 0f, 0f), _battlefield, state);

                Assert.IsTrue(seen.Add(cell), $"unit {i} was stacked onto already-taken {cell}");
                placed.Add(UnitAt(10 + i, _ai, cell));
            }
        }

        [Test]
        public void OutOfBoundaryPosition_ResolvesToTheNearestFreeCellInBoundary()
        {
            var cell = SpawnCellResolver.Resolve(new Vector3(10f, 0f, 0f), _battlefield, StateWith());

            Assert.AreEqual(new HexCoordinates(3, 0), cell);
        }

        [Test]
        public void ACorpseDoesNotBlockPlacement()
        {
            // D5 synergy: occupancy is alive-only, so a spawn may take a dead unit's cell.
            var state = StateWith(UnitAt(10, _ai, new HexCoordinates(1, 1), hp: 0));

            var cell = SpawnCellResolver.Resolve(new Vector3(1f, 0f, 1f), _battlefield, state);

            Assert.AreEqual(new HexCoordinates(1, 1), cell);
        }

        [Test]
        public void DistanceTies_BreakDeterministically()
        {
            // Centre occupied: all six (well, four in this square fake) nearest neighbours tie —
            // the resolver must pick the same one every run.
            var state = StateWith(UnitAt(1, _human, new HexCoordinates(0, 0)));

            var first = SpawnCellResolver.Resolve(Vector3.zero, _battlefield, state);
            var second = SpawnCellResolver.Resolve(Vector3.zero, _battlefield, state);

            Assert.AreEqual(first, second);
        }
    }
}
