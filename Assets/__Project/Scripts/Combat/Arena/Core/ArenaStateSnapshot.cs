using System.Collections.Generic;
using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>One unit's sim-relevant fields at a round-start boundary (mirrors <see cref="ArenaStateHash"/>).</summary>
    public class ArenaUnitSnapshot
    {
        public int UnitId { get; }
        public int OwnerPlayerId { get; }
        public int Q { get; }
        public int R { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public int Facing { get; }
        public IReadOnlyList<ArenaAbilityCooldownSnapshot> Abilities { get; }
        public IReadOnlyList<ArenaStatusSnapshot> Statuses { get; }

        public ArenaUnitSnapshot(
            int unitId, int ownerPlayerId, int q, int r, int currentHp, int maxHp, int facing,
            IReadOnlyList<ArenaAbilityCooldownSnapshot> abilities,
            IReadOnlyList<ArenaStatusSnapshot> statuses)
        {
            UnitId = unitId;
            OwnerPlayerId = ownerPlayerId;
            Q = q;
            R = r;
            CurrentHP = currentHp;
            MaxHP = maxHp;
            Facing = facing;
            Abilities = abilities ?? new List<ArenaAbilityCooldownSnapshot>();
            Statuses = statuses ?? new List<ArenaStatusSnapshot>();
        }
    }

    public class ArenaAbilityCooldownSnapshot
    {
        public int AbilityId { get; }
        public int CurrentCooldown { get; }

        public ArenaAbilityCooldownSnapshot(int abilityId, int currentCooldown)
        {
            AbilityId = abilityId;
            CurrentCooldown = currentCooldown;
        }
    }

    public class ArenaStatusSnapshot
    {
        public int EffectId { get; }
        public int Duration { get; }
        public int StackCount { get; }

        public ArenaStatusSnapshot(int effectId, int duration, int stackCount)
        {
            EffectId = effectId;
            Duration = duration;
            StackCount = stackCount;
        }
    }

    /// <summary>
    /// The authoritative match state at a round-start boundary — what a rejoining or resyncing
    /// peer is brought to. Captures exactly the sim-relevant unit fields <see cref="ArenaStateHash"/>
    /// digests (position, HP, facing, cooldowns, statuses), always at "planning just opened for
    /// <see cref="RoundNumber"/>", never mid-resolve. Deliberately NOT captured: ability queues
    /// (local planning state no other client ever reads — a resynced player loses an unexecuted
    /// queue), acted flags and intents (always clean at the boundary), the battlefield and the
    /// units' loadouts (both re-derive deterministically from the match seed + draft result that
    /// ride the rejoin package beside this snapshot).
    /// </summary>
    public class ArenaStateSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version { get; }
        public int RoundNumber { get; }

        /// <summary>The lockstep hash of the previous round's end (0 during round 1) — the
        /// rejoiner adopts it so its next commit envelope reports the host's own value.</summary>
        public ulong LastRoundHash { get; }

        public IReadOnlyList<ArenaUnitSnapshot> Units { get; }

        public ArenaStateSnapshot(
            int version, int roundNumber, ulong lastRoundHash, IReadOnlyList<ArenaUnitSnapshot> units)
        {
            Version = version;
            RoundNumber = roundNumber;
            LastRoundHash = lastRoundHash;
            Units = units ?? new List<ArenaUnitSnapshot>();
        }

        /// <summary>Digests a round-start state (units UnitId-ordered, statuses in list order).</summary>
        public static ArenaStateSnapshot Capture(ICombatState roundStartState, ulong lastRoundHash)
        {
            var units = roundStartState.Units
                .OrderBy(u => u.Id)
                .Select(u => new ArenaUnitSnapshot(
                    u.Id,
                    u.Owner.Id,
                    u.Position.Q,
                    u.Position.R,
                    u.CurrentHP,
                    u.MaxHP,
                    (int)u.FacingDirection,
                    u.Abilities
                        .OrderBy(a => a.Ability.Id)
                        .Select(a => new ArenaAbilityCooldownSnapshot(a.Ability.Id, a.CurrentCooldown))
                        .ToList(),
                    u.StatusEffects
                        .Select(e => new ArenaStatusSnapshot(e.Id, e.Duration, e.StackCount))
                        .ToList()))
                .ToList();

            return new ArenaStateSnapshot(
                CurrentVersion, roundStartState.TurnNumber, lastRoundHash, units);
        }
    }
}
