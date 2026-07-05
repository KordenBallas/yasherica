using Core.Logging;
using Narrative.Casting.Core;
using Narrative.Facts.Core;

namespace Narrative.Threads.Core
{
    /// <summary>
    /// Default <see cref="IThreadMaintenance"/>. Retirement is a state change plus one indicator
    /// fact (<see cref="ThreadFactKeys.Retired"/>) — no closure beat is placed (FR6). Premise and
    /// resolution predicates are world-scoped, so they evaluate against an empty context; the mapper
    /// warns about <c>$</c>-token subjects at build time.
    /// </summary>
    public sealed class ThreadMaintenanceService : IThreadMaintenance
    {
        private readonly IThreadLedger _ledger;
        private readonly IThreadCatalog _catalog;
        private readonly IPreconditionEvaluator _evaluator;
        private readonly IGameLogger _logger;

        public ThreadMaintenanceService(IThreadLedger ledger, IThreadCatalog catalog,
            IPreconditionEvaluator evaluator, IGameLogger logger = null)
        {
            _ledger = ledger;
            _catalog = catalog;
            _evaluator = evaluator;
            _logger = logger;
        }

        public void Tick(int windowIndex, IFactStore facts)
        {
            if (facts == null)
            {
                return;
            }

            var context = new ContextBag();
            var threads = _ledger.Threads;
            for (int i = 0; i < threads.Count; i++)
            {
                var record = threads[i];
                if (record.State != ThreadState.Live)
                {
                    continue;
                }

                FoldAdvance(record);
                var definition = _catalog.GetOrImplicitDefault(record.ThreadId);

                if (definition.ResolutionConditions.Count > 0 &&
                    _evaluator.EvaluateAll(definition.ResolutionConditions, facts, context))
                {
                    _ledger.Resolve(record.ThreadId);
                    _logger?.Info(LogCategory.Narrative,
                        $"[ThreadMaintenance] Thread '{record.ThreadId}' resolved (payoff reached) at window {windowIndex}.");
                    continue;
                }

                // Conflict outranks expiry: the player-caused ending wins over the clock (FR5).
                if (definition.Premise.Count > 0 &&
                    !_evaluator.EvaluateAll(definition.Premise, facts, context))
                {
                    Retire(record, ThreadRetirementReason.Conflict, facts, windowIndex);
                    continue;
                }

                if (record.Kind == ThreadKind.Ephemeral &&
                    record.WindowsWithoutAdvance > definition.LifespanWindows)
                {
                    Retire(record, ThreadRetirementReason.Expired, facts, windowIndex);
                }
            }
        }

        private static void FoldAdvance(ThreadRecord record)
        {
            if (record.AdvancedSinceLastTick)
            {
                record.WindowsWithoutAdvance = 0;
                record.AdvancedSinceLastTick = false;
                return;
            }

            record.WindowsWithoutAdvance++;
        }

        private void Retire(ThreadRecord record, ThreadRetirementReason reason, IFactStore facts, int windowIndex)
        {
            _ledger.Fail(record.ThreadId, reason);
            facts.Set(
                new FactKey(FactNamespace.World, record.ThreadId, ThreadFactKeys.Retired),
                FactValue.FromString(reason == ThreadRetirementReason.Expired
                    ? ThreadFactKeys.RetiredValueExpired
                    : ThreadFactKeys.RetiredValueConflict));
            _logger?.Info(LogCategory.Narrative,
                $"[ThreadMaintenance] Thread '{record.ThreadId}' retired ({reason}) at window {windowIndex} - indicator fact written, no closure beat.");
        }
    }
}
