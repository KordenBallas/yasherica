using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Deterministic FNV-1a digest of the sim-relevant combat state (R10 lockstep safety net):
    /// units ordered by UnitId — id, position, HP, facing, per-ability cooldowns (ability-id
    /// order), status effects (id + duration + stacks, in list order). Identical bundles resolved
    /// on identical states must produce identical hashes on every client; the host logs a loud
    /// error on mismatch (no recovery in the MVP — the PRD excludes reconnect).
    /// </summary>
    public static class ArenaStateHash
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static ulong Compute(ICombatState state)
        {
            ulong hash = FnvOffsetBasis;

            foreach (var unit in state.Units.OrderBy(u => u.Id))
            {
                hash = HashInt(hash, unit.Id);
                hash = HashInt(hash, unit.Position.Q);
                hash = HashInt(hash, unit.Position.R);
                hash = HashInt(hash, unit.CurrentHP);
                hash = HashInt(hash, (int)unit.FacingDirection);

                foreach (var ability in unit.Abilities.OrderBy(a => a.Ability.Id))
                {
                    hash = HashInt(hash, ability.Ability.Id);
                    hash = HashInt(hash, ability.CurrentCooldown);
                }

                foreach (var effect in unit.StatusEffects)
                {
                    hash = HashInt(hash, effect.Id);
                    hash = HashInt(hash, effect.Duration);
                    hash = HashInt(hash, effect.StackCount);
                }
            }

            return hash;
        }

        private static ulong HashInt(ulong hash, int value)
        {
            unchecked
            {
                var v = (uint)value;
                hash = (hash ^ (v & 0xFF)) * FnvPrime;
                hash = (hash ^ ((v >> 8) & 0xFF)) * FnvPrime;
                hash = (hash ^ ((v >> 16) & 0xFF)) * FnvPrime;
                hash = (hash ^ ((v >> 24) & 0xFF)) * FnvPrime;
                return hash;
            }
        }
    }
}
