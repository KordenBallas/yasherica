using System;
using System.Collections.Generic;

namespace GameInput.Core
{
    /// <summary>
    /// Enforces the coverage rule (Input Foundation R2): every action must be bound on every source, and
    /// an unbound pair is acceptable only while it sits on the explicit deferred-gap allowlist. The
    /// coverage test runs both directions — unlisted gaps fail, and allowlist entries that have quietly
    /// become bound fail too, so the list can never go stale.
    /// </summary>
    public sealed class InputCoverageValidator
    {
        /// <summary>Every (action × source) pair that is neither bound in the catalog nor allowlisted.</summary>
        public IReadOnlyList<string> FindUncoveredPairs(InputBindingCatalog catalog)
        {
            var problems = new List<string>();
            foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
            {
                foreach (InputSource source in Enum.GetValues(typeof(InputSource)))
                {
                    if (!catalog.IsBound(action, source) && !IsAllowlisted(catalog, action, source))
                    {
                        problems.Add($"{action} has no {source} binding and is not an allowlisted deferred gap");
                    }
                }
            }

            return problems;
        }

        /// <summary>Allowlist entries whose pair is actually bound now — stale entries that must be removed.</summary>
        public IReadOnlyList<string> FindStaleAllowlistEntries(InputBindingCatalog catalog)
        {
            var problems = new List<string>();
            var gaps = catalog.DeferredGaps;
            for (int i = 0; i < gaps.Count; i++)
            {
                var gap = gaps[i];
                if (catalog.IsBound(gap.Action, gap.Source))
                {
                    problems.Add($"{gap.Action} × {gap.Source} is allowlisted as deferred but the catalog binds it — remove the stale allowlist entry");
                }
            }

            return problems;
        }

        private static bool IsAllowlisted(InputBindingCatalog catalog, GameAction action, InputSource source)
        {
            var gaps = catalog.DeferredGaps;
            for (int i = 0; i < gaps.Count; i++)
            {
                if (gaps[i].Action == action && gaps[i].Source == source)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
