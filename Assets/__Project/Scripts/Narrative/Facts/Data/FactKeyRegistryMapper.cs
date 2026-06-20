using System.Collections.Generic;
using CoreRegistry = Narrative.Facts.Core.FactKeyRegistry;
using Narrative.Facts.Core;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// The only bridge from the authored fact vocabulary ScriptableObjects into the UnityEngine-free
    /// Core <see cref="CoreRegistry"/>. Pure adapter: each <see cref="FactKeyDefinition"/> produces a
    /// <see cref="FactKeyInfo"/>; null/empty entries are skipped.
    /// </summary>
    public static class FactKeyRegistryMapper
    {
        public static CoreRegistry ToRegistry(FactKeyRegistry registry)
        {
            return new CoreRegistry(ToInfos(registry?.Keys));
        }

        public static CoreRegistry ToRegistry(IEnumerable<FactKeyDefinition> definitions)
        {
            return new CoreRegistry(ToInfos(definitions));
        }

        private static IEnumerable<FactKeyInfo> ToInfos(IEnumerable<FactKeyDefinition> definitions)
        {
            if (definitions == null)
            {
                yield break;
            }

            foreach (var def in definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.Key))
                {
                    continue;
                }

                yield return def.ToInfo();
            }
        }
    }
}
