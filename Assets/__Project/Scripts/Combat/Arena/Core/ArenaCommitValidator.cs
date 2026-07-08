using System;
using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>One commit's verdict; the reason feeds the host's loud log.</summary>
    public class ArenaCommitVerdict
    {
        public bool IsValid { get; }
        public string Reason { get; }

        private ArenaCommitVerdict(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public static readonly ArenaCommitVerdict Valid = new ArenaCommitVerdict(true, null);
        public static ArenaCommitVerdict Invalid(string reason) => new ArenaCommitVerdict(false, reason);
    }

    /// <summary>
    /// X2 anti-cheat: re-validates a relayed commit against the host's canonical round-start
    /// state instead of trusting the peer. Checks ownership, actability, shape (one move XOR a
    /// volley within the queue budget), move legality (origin/range/board), ability legality
    /// (known + off cooldown), and — the load-bearing one — re-derives every ability step's
    /// committed cells through the same <see cref="ArenaCommitBuilder.RebuildAbilityIntent"/>
    /// the client used at lock time and requires exact equality: tampered cells cannot enter the
    /// bundle. Pure C#; an empty commitment is always legal (it is also the substitution the
    /// host applies to whatever fails here).
    /// </summary>
    public class ArenaCommitValidator
    {
        private readonly ArenaCommitBuilder _commitBuilder;

        public ArenaCommitValidator(ArenaCommitBuilder commitBuilder)
        {
            _commitBuilder = commitBuilder;
        }

        public ArenaCommitVerdict Validate(
            ICombatState roundStartState, ArenaCommit commit, int maxQueueSize, int queueCredits)
        {
            var unit = roundStartState.GetUnit(commit.UnitId);
            if (unit == null)
                return ArenaCommitVerdict.Invalid($"unit {commit.UnitId} does not exist");
            if (unit.Owner.Id != commit.PlayerId)
                return ArenaCommitVerdict.Invalid(
                    $"unit {commit.UnitId} belongs to player {unit.Owner.Id}, not {commit.PlayerId}");

            if (!Enum.IsDefined(typeof(Combat.Config.HexDirection), commit.FinalFacing))
                return ArenaCommitVerdict.Invalid($"undefined facing {(int)commit.FinalFacing}");

            // An empty commitment (pass / schedule / stunned wait) is always legal.
            if (commit.Steps.Count == 0)
                return ArenaCommitVerdict.Valid;

            if (!unit.IsAlive)
                return ArenaCommitVerdict.Invalid($"unit {commit.UnitId} is dead");
            if (unit.ActionState == UnitActionState.Stunned)
                return ArenaCommitVerdict.Invalid($"unit {commit.UnitId} is stunned — only a pass is legal");

            int moveSteps = commit.Steps.Count(s => s.IsMove);
            int abilitySteps = commit.Steps.Count(s => s.IsAbility);
            if (moveSteps + abilitySteps != commit.Steps.Count)
                return ArenaCommitVerdict.Invalid("a step is neither a move nor an ability");
            if (moveSteps > 1 || (moveSteps == 1 && abilitySteps > 0))
                return ArenaCommitVerdict.Invalid(
                    "a commit is one move OR an ability volley, never both");

            if (abilitySteps > 0)
            {
                if (maxQueueSize > 0 && abilitySteps > maxQueueSize)
                    return ArenaCommitVerdict.Invalid(
                        $"volley of {abilitySteps} exceeds the queue size {maxQueueSize}");
                if (abilitySteps > queueCredits)
                    return ArenaCommitVerdict.Invalid(
                        $"volley of {abilitySteps} exceeds the {queueCredits} scheduling round(s) the seat banked");
            }

            foreach (var step in commit.Steps)
            {
                var verdict = step.IsMove
                    ? ValidateMove(roundStartState, unit, step)
                    : ValidateAbility(roundStartState, unit, step, commit.FinalFacing);
                if (!verdict.IsValid)
                    return verdict;
            }

            return ArenaCommitVerdict.Valid;
        }

        private static ArenaCommitVerdict ValidateMove(ICombatState state, IUnit unit, EnemyIntent step)
        {
            if (!step.CommittedOrigin.Equals(unit.Position))
                return ArenaCommitVerdict.Invalid(
                    $"move origin {step.CommittedOrigin} is not the unit's position {unit.Position}");

            var destination = step.MoveDestination.Value;
            if (!state.IsPositionValid(destination))
                return ArenaCommitVerdict.Invalid($"move destination {destination} is off the platform");

            int range = MovementRange.EffectiveFor(unit);
            int distance = state.CalculateDistance(unit.Position, destination);
            if (distance > range)
                return ArenaCommitVerdict.Invalid(
                    $"move of {distance} exceeds the effective range {range}");

            return ArenaCommitVerdict.Valid;
        }

        private ArenaCommitVerdict ValidateAbility(
            ICombatState state, IUnit unit, EnemyIntent step, Combat.Config.HexDirection finalFacing)
        {
            var instance = unit.GetAbility(step.AbilityId);
            if (instance == null)
                return ArenaCommitVerdict.Invalid(
                    $"unit {unit.Id} does not know ability {step.AbilityId}");
            if (instance.CurrentCooldown > 0)
                return ArenaCommitVerdict.Invalid(
                    $"ability {step.AbilityId} is on cooldown ({instance.CurrentCooldown})");

            // The one rule (shared with lock-time building): recompute the committed snapshot
            // from the canonical position + claimed facing and demand exact equality.
            var canonical = _commitBuilder.RebuildAbilityIntent(state, unit, instance, finalFacing);
            if (!step.CommittedOrigin.Equals(canonical.CommittedOrigin))
                return ArenaCommitVerdict.Invalid(
                    $"ability {step.AbilityId} origin {step.CommittedOrigin} ≠ canonical {canonical.CommittedOrigin}");
            if (!Nullable.Equals(step.CommittedFacing, canonical.CommittedFacing))
                return ArenaCommitVerdict.Invalid(
                    $"ability {step.AbilityId} facing {step.CommittedFacing} ≠ canonical {canonical.CommittedFacing}");
            if (!step.CommittedCells.SequenceEqual(canonical.CommittedCells))
                return ArenaCommitVerdict.Invalid(
                    $"ability {step.AbilityId} committed cells were tampered with");

            return ArenaCommitVerdict.Valid;
        }
    }
}
