using System;
using System.Collections.Generic;

namespace Narrative.Barks.Core
{
    /// <summary>
    /// UnityEngine-free line pools for the cauldron's live bark channel (P1-10), keyed
    /// slot × lean, mapped from <c>CauldronBarkLinesConfig</c>. A slot missing its lean's pool
    /// falls back to the OTHER lean (a half-authored slot still speaks); a slot with no lines at
    /// all is a quiet moment, never an error. Also carries the authored set of belonging ids that
    /// read as "dark" (Monster-lean) — the data-side vocabulary the dark-offer trigger matches
    /// offer belongings against, so no family id is ever hard-coded.
    /// </summary>
    public sealed class CauldronBarkLines
    {
        private static readonly IReadOnlyList<string> NoLines = Array.Empty<string>();

        public static readonly CauldronBarkLines Empty = new CauldronBarkLines(null, null);

        private readonly IReadOnlyDictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>> _pools;
        private readonly HashSet<string> _darkBelongingIds;

        public CauldronBarkLines(
            IReadOnlyDictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>> pools,
            IEnumerable<string> darkBelongingIds)
        {
            _pools = pools ?? new Dictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>>(0);
            _darkBelongingIds = new HashSet<string>(StringComparer.Ordinal);
            if (darkBelongingIds != null)
            {
                foreach (var id in darkBelongingIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _darkBelongingIds.Add(id);
                    }
                }
            }
        }

        /// <summary>The pool for a slot at a lean, falling back to the opposite lean. Never null.</summary>
        public IReadOnlyList<string> PoolFor(CauldronBarkSlot slot, BarkLean lean)
        {
            if (_pools.TryGetValue((slot, lean), out var pool) && pool != null && pool.Count > 0)
            {
                return pool;
            }

            var other = lean == BarkLean.Indulgent ? BarkLean.Restrained : BarkLean.Indulgent;
            if (_pools.TryGetValue((slot, other), out var fallback) && fallback != null && fallback.Count > 0)
            {
                return fallback;
            }

            return NoLines;
        }

        /// <summary>Whether an offer belonging id reads as a dark (Monster-lean) currency.</summary>
        public bool IsDarkBelonging(string belongingId) =>
            !string.IsNullOrEmpty(belongingId) && _darkBelongingIds.Contains(belongingId);
    }
}
