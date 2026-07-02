using System.Collections.Generic;
using LevelGeneration;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Plain dictionary-backed <see cref="IBiomeMonsterPoolCatalog"/>. The data layer
    /// (<c>BiomeMonsterPoolMapper</c>) converts the authored SO assets into the id map once at install
    /// time so this stays UnityEngine-free and unit-testable.
    /// </summary>
    public sealed class BiomeMonsterPoolCatalog : IBiomeMonsterPoolCatalog
    {
        private static readonly int[] EmptyPool = System.Array.Empty<int>();

        private readonly Dictionary<LevelTheme, IReadOnlyList<int>> _pools;

        public BiomeMonsterPoolCatalog(Dictionary<LevelTheme, IReadOnlyList<int>> pools)
        {
            _pools = pools ?? new Dictionary<LevelTheme, IReadOnlyList<int>>();
        }

        public IReadOnlyList<int> GetPool(LevelTheme theme)
        {
            return _pools.TryGetValue(theme, out var pool) && pool != null ? pool : EmptyPool;
        }
    }
}
