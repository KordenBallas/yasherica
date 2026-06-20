using System.Collections.Generic;
using Core.Logging;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Startup/editor guard that keeps the curated typed-accessor refs and the authoritative SO
    /// registry in agreement (D3). Flags refs missing from the registry (a code/asset drift, e.g. a
    /// renamed key) and — optionally — registry keys that no ref mirrors (informational only).
    /// Pure C#; callers supply a logger for surfacing problems.
    /// </summary>
    public static class FactKeyRefRegistryCheck
    {
        /// <summary>
        /// Returns true if every supplied ref is declared in the registry with a matching value type.
        /// Each mismatch is warned through <paramref name="logger"/>.
        /// </summary>
        public static bool Validate(IFactKeyRegistry registry, IEnumerable<FactKeyRef> refs, IGameLogger logger = null)
        {
            if (registry == null)
            {
                logger?.Warning("[FactKeyRefRegistryCheck] No registry supplied - cannot validate typed refs.");
                return false;
            }

            var allKnown = true;
            foreach (var keyRef in refs ?? System.Array.Empty<FactKeyRef>())
            {
                if (!registry.TryGetInfo(keyRef.Namespace, keyRef.Key, out var info))
                {
                    allKnown = false;
                    logger?.Warning(
                        $"[FactKeyRefRegistryCheck] Typed ref '{keyRef.Namespace}.{keyRef.Key}' is not declared in the fact registry.");
                    continue;
                }

                if (info.ValueType != keyRef.ValueType)
                {
                    allKnown = false;
                    logger?.Warning(
                        $"[FactKeyRefRegistryCheck] Typed ref '{keyRef.Namespace}.{keyRef.Key}' type {keyRef.ValueType} " +
                        $"disagrees with registry type {info.ValueType}.");
                }
            }

            return allKnown;
        }
    }
}
