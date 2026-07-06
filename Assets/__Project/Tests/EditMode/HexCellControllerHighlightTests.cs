using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Core.Logging;
using UnityEngine;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// D8 — the plan-phase highlight must always clear back to the cell's base color: stacking a
    /// highlight over an existing highlight (a hover over an aim, two presenters overlapping)
    /// must thread the UNDERLYING original color through, never capture the current highlight
    /// color as "original" — that capture is how cells were left stuck yellow for the fight.
    /// </summary>
    [TestFixture]
    public class HexCellControllerHighlightTests
    {
        private static readonly Color BaseColor = Color.black;

        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private sealed class SingleCellBattlefield : IBattlefield
        {
            private readonly IHexCell _cell;

            public SingleCellBattlefield(IHexCell cell) => _cell = cell;

            public bool IsActive => true;
            public float HexSize => 1f;
            public Vector3 Center => Vector3.zero;
            public IHexGrid Grid => null;

            public void Initialize(PlatformHexSurface surface, Vector3 center, HexDirectionConfig hexConfig) { }
            public void Activate() { }
            public void Deactivate() { }
            public void Clear() { }

            public IHexCell GetCellAt(HexCoordinates coordinates) =>
                coordinates.Equals(_cell.Coordinates) ? _cell : null;
            public IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range) => new List<IHexCell>();
            public IReadOnlyList<HexCoordinates> GetCellsInBoundary() =>
                new List<HexCoordinates> { _cell.Coordinates };
            public Vector3 HexToWorld(HexCoordinates hex) => Vector3.zero;
            public HexCoordinates WorldToHex(Vector3 world) => _cell.Coordinates;
            public bool IsCellInBoundary(HexCoordinates hex) => hex.Equals(_cell.Coordinates);
            public IReadOnlyList<IHexCell> GetCellsBySelection(CellSelectionType selectionType, CellSelectionParams parameters) =>
                throw new NotSupportedException();
        }

        private HexCell _cell;
        private HexCellController _controller;
        private CombatMovementConfig _config;
        private HexCoordinates Coords => new HexCoordinates(0, 0);

        [SetUp]
        public void SetUp()
        {
            var logger = new FakeLogger();
            _cell = new HexCell(new HexCoordinates(0, 0), Vector3.zero, logger: logger);
            _cell.InitializeStateMachine(new HexCellIdleState(BaseColor, logger));
            _config = ScriptableObject.CreateInstance<CombatMovementConfig>();
            _controller = new HexCellController(new SingleCellBattlefield(_cell), _config, logger);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private Color CurrentColor => _cell.StateMachine.CurrentState.GetColor();

        [Test]
        public void HighlightThenClear_RestoresTheBaseColor()
        {
            _controller.HighlightCell(Coords, HighlightType.ValidAbilityTarget);
            _controller.ClearHighlight(Coords);

            Assert.AreEqual(BaseColor, CurrentColor);
            Assert.AreEqual(HexCellStateType.Idle, _cell.StateMachine.CurrentState.StateType);
        }

        [Test]
        public void HighlightOverHighlight_ThenClear_RestoresTheBaseColor()
        {
            // The stuck-yellow chain: aim highlight, then a hover stacked on top of it.
            _controller.HighlightCell(Coords, HighlightType.ValidAbilityTarget);
            _controller.HighlightCell(Coords, HighlightType.Hovered);
            _controller.ClearHighlight(Coords);

            Assert.AreEqual(BaseColor, CurrentColor,
                "clearing a stacked highlight must fall back to the true base, not the first highlight");
        }

        [Test]
        public void ThreeStackedHighlights_ThenClear_RestoreTheBaseColor()
        {
            _controller.HighlightCell(Coords, HighlightType.ValidAbilityTarget);
            _controller.HighlightCell(Coords, HighlightType.Hovered);
            _controller.HighlightCell(Coords, HighlightType.InvalidMove);
            _controller.ClearHighlight(Coords);

            Assert.AreEqual(BaseColor, CurrentColor);
        }

        [Test]
        public void ClearingTwice_IsHarmless()
        {
            _controller.HighlightCell(Coords, HighlightType.Hovered);
            _controller.ClearHighlight(Coords);
            _controller.ClearHighlight(Coords);

            Assert.AreEqual(BaseColor, CurrentColor);
        }
    }
}
