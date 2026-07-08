using System.Collections.Generic;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Player;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The X1 state transfer: a round-start snapshot captured on the authority and restored as an
    /// overlay on a freshly spawned board must reproduce the exact sim-relevant state — the
    /// capture→restore→hash round trip is the invariant the whole rejoin path leans on.
    /// </summary>
    [TestFixture]
    public class ArenaSnapshotTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        /// <summary>
        /// Stands in for the catalog+factory production path: rebuilds the hand-authored test
        /// effects by id at an explicit duration + stack count.
        /// </summary>
        private sealed class FakeStatusReconstructor : IArenaStatusReconstructor
        {
            public bool TryRebuild(int effectId, int duration, int stackCount, out IStatusEffect effect)
            {
                effect = effectId switch
                {
                    PoisonId => new DataDrivenDamageOverTimeEffect(
                        PoisonId, "Poison", duration, stackCount, StackRule.StackToCap, 3,
                        StatusEffectTriggerType.TurnEnd, 4, 4),
                    StunId => new DataDrivenControlEffect(
                        StunId, "Stun", duration, ControlKind.Stun, 0,
                        StatusEffectTriggerType.TurnEnd, stackCount: stackCount,
                        stackRule: StackRule.Refresh, maxStacks: 1),
                    _ => null
                };
                return effect != null;
            }
        }

        private const int AbilityId = 100;
        private const int SecondAbilityId = 101;
        private const int PoisonId = 2;
        private const int StunId = 3;
        private const int MaxHp = 30;

        private static IAbilityInstance Ability(int id, int cooldown = 0) =>
            new AbilityInstance(new DataDrivenDamageAbility(
                id, $"Test {id}", 2, AbilityShapeData.ForLine(2), 10), cooldown);

        private static IStatusEffect Poison(int duration, int stacks) =>
            new DataDrivenDamageOverTimeEffect(
                PoisonId, "Poison", duration, stacks, StackRule.StackToCap, 3,
                StatusEffectTriggerType.TurnEnd, 4, 4);

        private ArenaSnapshotRestorer CreateRestorer() =>
            new ArenaSnapshotRestorer(new FakeStatusReconstructor(), new FakeLogger());

        /// <summary>A mid-match "authority" state: wear on HP/cooldowns/statuses/positions.</summary>
        private static CombatState LivedInState(IPlayer p1, IPlayer p2)
        {
            var units = new List<IUnit>
            {
                new Unit(1, p1, new HexCoordinates(1, -1), 22, MaxHp,
                    new List<IAbilityInstance> { Ability(AbilityId, cooldown: 2), Ability(SecondAbilityId) },
                    statusEffects: new List<IStatusEffect> { Poison(2, 3) },
                    facingDirection: HexDirection.W),
                new Unit(2, p2, new HexCoordinates(3, 0), 7, MaxHp,
                    new List<IAbilityInstance> { Ability(AbilityId) },
                    facingDirection: HexDirection.NE)
            };

            return new CombatState(units, new List<IPlayer> { p1, p2 }, p1,
                turnNumber: 5, CombatPhase.Combat, roundPhase: RoundPhase.PlayerAct);
        }

        /// <summary>The same roster as a rejoiner spawns it: full HP, cold abilities, no wear.</summary>
        private static CombatState FreshlySpawnedState(IPlayer p1, IPlayer p2)
        {
            var units = new List<IUnit>
            {
                new Unit(1, p1, new HexCoordinates(0, 0), MaxHp, MaxHp,
                    new List<IAbilityInstance> { Ability(AbilityId), Ability(SecondAbilityId) },
                    facingDirection: HexDirection.E),
                new Unit(2, p2, new HexCoordinates(4, 0), MaxHp, MaxHp,
                    new List<IAbilityInstance> { Ability(AbilityId) },
                    facingDirection: HexDirection.W)
            };

            return new CombatState(units, new List<IPlayer> { p1, p2 }, p1,
                turnNumber: 1, CombatPhase.Combat, roundPhase: RoundPhase.PlayerAct);
        }

        [Test]
        public void CaptureRestore_RoundTrips_StateHash()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var authority = LivedInState(p1, p2);

            var snapshot = ArenaStateSnapshot.Capture(authority, lastRoundHash: 0xABCDUL);
            var restored = CreateRestorer().Restore(FreshlySpawnedState(p1, p2), snapshot);

            Assert.IsNotNull(restored);
            Assert.AreEqual(ArenaStateHash.Compute(authority), ArenaStateHash.Compute(restored),
                "the transferred board must hash identically to the authority's");
            Assert.AreEqual(5, restored.TurnNumber, "the rejoiner adopts the authoritative round");
            Assert.AreEqual(RoundPhase.PlayerAct, restored.RoundPhase);
            CollectionAssert.IsEmpty(restored.EnemyIntents);
        }

        [Test]
        public void Restore_RebuildsPositionsHpFacingCooldownsAndStatusStacks()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var snapshot = ArenaStateSnapshot.Capture(LivedInState(p1, p2), 0);

            var restored = CreateRestorer().Restore(FreshlySpawnedState(p1, p2), snapshot);

            var unit1 = restored.GetUnit(1);
            Assert.AreEqual(new HexCoordinates(1, -1), unit1.Position);
            Assert.AreEqual(22, unit1.CurrentHP);
            Assert.AreEqual(HexDirection.W, unit1.FacingDirection);
            Assert.AreEqual(2, unit1.GetAbility(AbilityId).CurrentCooldown,
                "mid-cooldown transfers");
            Assert.AreEqual(0, unit1.GetAbility(SecondAbilityId).CurrentCooldown);

            Assert.AreEqual(1, unit1.StatusEffects.Count);
            Assert.AreEqual(PoisonId, unit1.StatusEffects[0].Id);
            Assert.AreEqual(2, unit1.StatusEffects[0].Duration, "mid-life duration transfers");
            Assert.AreEqual(3, unit1.StatusEffects[0].StackCount, "stacks transfer");

            var unit2 = restored.GetUnit(2);
            Assert.AreEqual(7, unit2.CurrentHP);
            Assert.AreEqual(HexDirection.NE, unit2.FacingDirection);
        }

        [Test]
        public void Restore_ClearsLocalQueue_AndActedFlag()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var snapshot = ArenaStateSnapshot.Capture(LivedInState(p1, p2), 0);

            // The local board has planning junk: a queued volley + an acted flag.
            var local = FreshlySpawnedState(p1, p2);
            var dirty = (local.GetUnit(1) as Unit)
                .WithAbilityQueue(new List<ScheduledAbility>
                {
                    new ScheduledAbility(Ability(AbilityId), 0)
                })
                .WithActedThisTurn(true);
            local = local.WithUpdatedUnit(dirty);

            var restored = CreateRestorer().Restore(local, snapshot);

            CollectionAssert.IsEmpty(restored.GetUnit(1).AbilityQueue,
                "an unexecuted queue is local planning state — discarded on transfer");
            Assert.IsFalse(restored.GetUnit(1).HasActedThisTurn,
                "the snapshot is a round-start boundary: nobody has acted");
        }

        [Test]
        public void Restore_VersionMismatch_ReturnsNull()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var good = ArenaStateSnapshot.Capture(LivedInState(p1, p2), 0);
            var alien = new ArenaStateSnapshot(
                ArenaStateSnapshot.CurrentVersion + 1, good.RoundNumber, good.LastRoundHash, good.Units);

            Assert.IsNull(CreateRestorer().Restore(FreshlySpawnedState(p1, p2), alien));
        }

        [Test]
        public void Restore_UnknownUnit_ReturnsNull()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var snapshot = ArenaStateSnapshot.Capture(LivedInState(p1, p2), 0);

            // The local board is missing unit 2 — a diverged spawn build must not half-restore.
            var units = new List<IUnit>
            {
                new Unit(1, p1, new HexCoordinates(0, 0), MaxHp, MaxHp,
                    new List<IAbilityInstance> { Ability(AbilityId), Ability(SecondAbilityId) })
            };
            var local = new CombatState(units, new List<IPlayer> { p1, p2 }, p1);

            Assert.IsNull(CreateRestorer().Restore(local, snapshot));
        }

        [Test]
        public void Restore_UnknownStatusDefinition_ReturnsNull()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var authority = LivedInState(p1, p2);
            var withAlienStatus = authority.WithUpdatedUnit(
                (authority.GetUnit(2) as Unit).WithStatusEffects(new List<IStatusEffect>
                {
                    new DataDrivenDamageOverTimeEffect(999, "Alien", 2, 1, StackRule.Refresh, 1,
                        StatusEffectTriggerType.TurnEnd, 1, 0)
                }));
            var snapshot = ArenaStateSnapshot.Capture(withAlienStatus, 0);

            Assert.IsNull(CreateRestorer().Restore(FreshlySpawnedState(p1, p2), snapshot),
                "a status with no local definition must fail the whole restore");
        }

        [Test]
        public void Restore_DeadUnit_StaysDead()
        {
            var p1 = new HumanPlayer(1, "P1");
            var p2 = new HumanPlayer(2, "P2");
            var authority = LivedInState(p1, p2);
            authority = authority.WithUpdatedUnit((authority.GetUnit(2) as Unit).WithHP(0));
            var snapshot = ArenaStateSnapshot.Capture(authority, 0);

            var restored = CreateRestorer().Restore(FreshlySpawnedState(p1, p2), snapshot);

            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.GetUnit(2).IsAlive,
                "a unit that died before the transfer (incl. departed seats) arrives dead");
            Assert.AreEqual(ArenaStateHash.Compute(authority), ArenaStateHash.Compute(restored));
        }
    }
}
