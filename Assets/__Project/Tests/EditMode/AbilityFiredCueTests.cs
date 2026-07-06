using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the D3 live-animation seam: executing an ability notifies the fired-cue sink with the
    /// caster, shape, and struck cells (so a presentation view can play the animation), and the executor
    /// stays null-safe when no sink is bound (headless / Arena).
    /// </summary>
    [TestFixture]
    public class AbilityFiredCueTests
    {
        private sealed class FakeSink : IAbilityFiredSink
        {
            public event Action<AbilityFiredCue> Fired;
            public int NotifyCount;
            public AbilityFiredCue Last;

            public void Notify(AbilityFiredCue cue)
            {
                NotifyCount++;
                Last = cue;
                Fired?.Invoke(cue);
            }
        }

        private const int CasterId = 1;
        private const int AbilityId = 100;

        private HexDirectionConfig _config;
        private HumanPlayer _player;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _player = new HumanPlayer(1, "Player");
        }

        [TearDown]
        public void TearDown() => TestHexDirectionConfig.Destroy(_config);

        private AbilityExecutor Executor(IAbilityFiredSink sink) =>
            new AbilityExecutor(new DamageSystem(), null, new AbilityShapeCalculator(_config), _config, sink);

        private static IAbility LineAbility() =>
            new DataDrivenDamageAbility(AbilityId, "Test Line", 1, AbilityShapeData.ForLine(2), 10);

        private CombatState State(Unit caster) =>
            new CombatState(new List<IUnit> { caster }, new List<IPlayer> { _player },
                currentPlayer: _player, phase: CombatPhase.Combat);

        private Unit Caster() =>
            new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50, new List<IAbilityInstance>());

        [Test]
        public void ExecuteAbilityAtCells_NotifiesSink_WithCasterShapeAndCells()
        {
            var sink = new FakeSink();
            var executor = Executor(sink);
            var caster = Caster();
            var cells = new List<HexCoordinates> { new HexCoordinates(1, 0), new HexCoordinates(2, 0) };

            executor.ExecuteAbilityAtCells(State(caster), caster, LineAbility(), cells, HexDirection.E);

            Assert.AreEqual(1, sink.NotifyCount, "one cue per ability execution");
            Assert.AreEqual(CasterId, sink.Last.CasterUnitId);
            Assert.AreEqual(AbilityShapeType.Line, sink.Last.Shape);
            Assert.AreEqual(HexDirection.E, sink.Last.LineDirection);
            Assert.AreEqual(cells, sink.Last.Cells);
            Assert.IsFalse(sink.Last.IsHeal);
        }

        [Test]
        public void ExecuteAbilityAtCells_NoSink_DoesNotThrow()
        {
            var executor = Executor(null);
            var caster = Caster();
            var cells = new List<HexCoordinates> { new HexCoordinates(1, 0) };

            Assert.DoesNotThrow(() =>
                executor.ExecuteAbilityAtCells(State(caster), caster, LineAbility(), cells, HexDirection.E));
        }
    }
}
