using System.Collections.Generic;
using Combat.Core;
using Combat.Execution;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The P4-3b eligibility policy: outside batch mode it mirrors the resolver's default
    /// (alive and not stunned right now), so binding it unconditionally changes nothing; inside
    /// batch mode it answers from the snapshot taken at the current batch's start — a unit
    /// killed (or stunned) mid-batch still fires its same-batch step, which is what makes the
    /// exchange genuinely simultaneous.
    /// </summary>
    public class ArenaBatchEligibility : IIntentEligibility
    {
        private readonly HashSet<int> _canActThisBatch = new HashSet<int>();
        private bool _batchMode;

        public void BeginBatchMode()
        {
            _batchMode = true;
        }

        public void EndBatchMode()
        {
            _batchMode = false;
            _canActThisBatch.Clear();
        }

        /// <summary>Snapshots who may act, at a batch boundary.</summary>
        public void RefreshFrom(ICombatState state)
        {
            _canActThisBatch.Clear();
            foreach (var unit in state.Units)
            {
                if (unit.IsAlive && unit.ActionState != UnitActionState.Stunned)
                {
                    _canActThisBatch.Add(unit.Id);
                }
            }
        }

        public bool CanAct(IUnit caster)
        {
            return _batchMode
                ? _canActThisBatch.Contains(caster.Id)
                : caster.IsAlive && caster.ActionState != UnitActionState.Stunned;
        }
    }
}
