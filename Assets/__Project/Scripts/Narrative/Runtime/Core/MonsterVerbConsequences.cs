using System;
using Core.Logging;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Threads.Core;
using Zenject;

namespace Narrative.Runtime.Core
{
    /// <summary>
    /// The Monster verb's consequence chokepoint (P1-7, attack-card-monster-verb.md): when a
    /// dialogue-routed fight ends with the NPC dead — whether the player picked the Attack card or
    /// the NPC self-initiated, one verb, two entry points — it:
    /// <list type="bullet">
    /// <item>writes <c>actor.&lt;id&gt;.slain</c> (the planner then never recasts the actor — the
    /// forfeited future arc), and increments the Conquest path lean <c>world.path_conquest</c>;</item>
    /// <item>forecloses the encounter's thread (state + indicator fact, no closure beat — the same
    /// grammar as conflict/expiry retirement).</item>
    /// </list>
    /// Corpse-loot stays on the existing combat channel (<c>EnemyLootDropper</c>) — orthogonal to the
    /// quest-reward economy by design; this relay never touches items. Losing the fight writes
    /// nothing (defeat is handled by the run lifecycle).
    /// </summary>
    public sealed class MonsterVerbConsequences : IInitializable, IDisposable
    {
        private readonly DialogueRunner _runner;
        private readonly IFactStore _facts;
        private readonly IThreadLedger _threads;
        private readonly IGameLogger _logger;

        public MonsterVerbConsequences(DialogueRunner runner, IFactStore facts, IThreadLedger threads,
            IGameLogger logger = null)
        {
            _runner = runner;
            _facts = facts;
            _threads = threads;
            _logger = logger;
        }

        public void Initialize()
        {
            _runner.OnCombatResolved += HandleCombatResolved;
        }

        public void Dispose()
        {
            _runner.OnCombatResolved -= HandleCombatResolved;
        }

        private void HandleCombatResolved(bool playerWon)
        {
            if (!playerWon)
            {
                return;
            }

            var actorId = _runner.EncounterActorId;
            if (!string.IsNullOrEmpty(actorId))
            {
                _facts.SetBool(ActorFacts.Slain, true, actorId);
            }

            _facts.SetInt(WorldFacts.PathConquest, _facts.GetInt(WorldFacts.PathConquest) + 1);

            ForecloseThread(_runner.ActiveThreadId);

            _logger?.Info(LogCategory.Narrative,
                $"[MonsterVerbConsequences] Actor '{actorId}' slain - arc foreclosed, Conquest lean " +
                $"now {_facts.GetInt(WorldFacts.PathConquest)}.");
        }

        private void ForecloseThread(string threadId)
        {
            if (string.IsNullOrEmpty(threadId) || _threads.IsRetired(threadId))
            {
                return;
            }

            _threads.Fail(threadId, ThreadRetirementReason.Foreclosed);
            // The same indicator-fact grammar as ThreadMaintenanceService.Retire: a state change the
            // quest log / preconditions can read, never a fabricated closing beat.
            _facts.Set(
                new FactKey(FactNamespace.World, threadId, ThreadFactKeys.Retired),
                FactValue.FromString(ThreadFactKeys.RetiredValueForeclosed));
        }
    }
}
