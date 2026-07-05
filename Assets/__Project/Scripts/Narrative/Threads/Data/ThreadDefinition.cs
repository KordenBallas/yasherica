using System.Collections.Generic;
using Narrative.Facts.Data;
using Narrative.Threads.Core;
using UnityEngine;

namespace Narrative.Threads.Data
{
    /// <summary>
    /// ScriptableObject declaring one narrative thread (R8/D13) — configuration data only; the
    /// director consumes the mapped Core <see cref="ThreadDefinitionData"/>, never this SO. A story's
    /// <c>_threadId</c> label pointing at no ThreadDefinition asset resolves to an implicit ephemeral
    /// default at runtime, so declaring one is only needed to make a thread an arc, give it premise /
    /// resolution facts, or tune its lifespan.
    /// </summary>
    [CreateAssetMenu(fileName = "Thread", menuName = "Narrative/Threads/Thread")]
    public class ThreadDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must match the _threadId label on the stories that form this thread's beats.")]
        [SerializeField] private string _threadId = string.Empty;
        [Tooltip("Ephemeral (default): expires when un-advanced past its lifespan. Arc: a long storyline, expiry-exempt (but not conflict-exempt).")]
        [SerializeField] private ThreadKind _kind = ThreadKind.Ephemeral;

        [Header("Lifecycle facts (world-scoped only - no $ tokens)")]
        [Tooltip("Premise: predicates that must all HOLD. A live thread whose premise a written fact contradicts is retired as failed/conflict - the mutual-exclusion mechanism.")]
        [SerializeField] private List<FactPredicateSerial> _premise = new List<FactPredicateSerial>();
        [Tooltip("Resolution: when these all hold the thread retires as resolved (its payoff was reached). Empty = the thread stays live until expiry/conflict.")]
        [SerializeField] private List<FactPredicateSerial> _resolutionConditions = new List<FactPredicateSerial>();

        [Header("Expiry (Ephemeral only)")]
        [Tooltip("Windows the thread may go without an advance (a resolved beat) before it silently expires. Ignored for Arc threads.")]
        [Min(1)]
        [SerializeField] private int _lifespanWindows = 3;

        [Header("Documentation")]
        [TextArea]
        [SerializeField] private string _description = string.Empty;

        public string ThreadId => _threadId;
        public ThreadKind Kind => _kind;
        public IReadOnlyList<FactPredicateSerial> Premise => _premise;
        public IReadOnlyList<FactPredicateSerial> ResolutionConditions => _resolutionConditions;
        public int LifespanWindows => _lifespanWindows;
    }
}
