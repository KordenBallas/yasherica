using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Core.Logging;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Applies an authoritative <see cref="ArenaStateSnapshot"/> onto a live combat state:
    /// an OVERLAY, not a rebuild — every snapshot unit must already exist locally (the rejoiner
    /// spawns the identical roster from the seed + draft loadouts first), and only the
    /// sim-relevant fields are rewritten. Ability instances are re-created around the unit's
    /// existing ability objects (matched by id) so the behaviour never travels the wire;
    /// statuses rebuild through <see cref="IArenaStatusReconstructor"/>. Ability queues are
    /// cleared and acted flags reset — the snapshot is a round-start boundary by contract.
    /// Returns null on any mismatch (version, missing unit, unresolvable id): the caller falls
    /// back to its terminal path rather than resuming a half-restored sim.
    /// </summary>
    public class ArenaSnapshotRestorer
    {
        private readonly IArenaStatusReconstructor _statusReconstructor;
        private readonly IGameLogger _logger;

        public ArenaSnapshotRestorer(IArenaStatusReconstructor statusReconstructor, IGameLogger logger)
        {
            _statusReconstructor = statusReconstructor;
            _logger = logger;
        }

        public ICombatState Restore(ICombatState current, ArenaStateSnapshot snapshot)
        {
            if (snapshot == null)
                return null;

            if (snapshot.Version != ArenaStateSnapshot.CurrentVersion)
            {
                _logger.Error(LogCategory.Combat,
                    $"[ArenaSnapshotRestorer] Snapshot version {snapshot.Version} != " +
                    $"{ArenaStateSnapshot.CurrentVersion} — refusing to restore");
                return null;
            }

            var state = current as CombatState;
            if (state == null)
                return null;

            foreach (var unitSnapshot in snapshot.Units)
            {
                var unit = state.GetUnit(unitSnapshot.UnitId) as Unit;
                if (unit == null)
                {
                    _logger.Error(LogCategory.Combat,
                        $"[ArenaSnapshotRestorer] Snapshot unit {unitSnapshot.UnitId} does not exist " +
                        "locally — the roster/spawn build diverged; refusing to restore");
                    return null;
                }

                var restored = RestoreUnit(unit, unitSnapshot);
                if (restored == null)
                    return null;

                state = state.WithUpdatedUnit(restored);
            }

            return state
                .WithTurnNumber(snapshot.RoundNumber)
                .WithEnemyIntents(new List<EnemyIntent>())
                .WithRoundPhase(RoundPhase.PlayerAct);
        }

        private Unit RestoreUnit(Unit unit, ArenaUnitSnapshot snapshot)
        {
            var abilities = new List<IAbilityInstance>();
            foreach (var abilitySnapshot in snapshot.Abilities)
            {
                var existing = unit.Abilities.FirstOrDefault(
                    a => a.Ability.Id == abilitySnapshot.AbilityId);
                if (existing == null)
                {
                    _logger.Error(LogCategory.Combat,
                        $"[ArenaSnapshotRestorer] Unit {unit.Id} has no ability " +
                        $"{abilitySnapshot.AbilityId} locally — loadouts diverged; refusing to restore");
                    return null;
                }

                abilities.Add(new AbilityInstance(existing.Ability, abilitySnapshot.CurrentCooldown));
            }

            var statuses = new List<IStatusEffect>();
            foreach (var statusSnapshot in snapshot.Statuses)
            {
                if (!_statusReconstructor.TryRebuild(
                        statusSnapshot.EffectId, statusSnapshot.Duration, statusSnapshot.StackCount,
                        out var effect))
                {
                    _logger.Error(LogCategory.Combat,
                        $"[ArenaSnapshotRestorer] Status effect {statusSnapshot.EffectId} cannot be " +
                        "rebuilt (no definition) — refusing to restore");
                    return null;
                }

                statuses.Add(effect);
            }

            return new Unit(
                unit.Id,
                unit.Owner,
                new HexCoordinates(snapshot.Q, snapshot.R),
                snapshot.CurrentHP,
                snapshot.MaxHP,
                abilities,
                abilityQueue: new List<ScheduledAbility>(),
                statusEffects: statuses,
                hasActedThisTurn: false,
                facingDirection: (HexDirection)snapshot.Facing);
        }
    }
}
