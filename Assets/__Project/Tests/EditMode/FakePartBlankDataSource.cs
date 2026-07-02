using System.Collections.Generic;
using Mutation.Core;

namespace Tests.EditMode
{
    /// <summary>
    /// Shared test double for <see cref="IPartBlankDataSource"/>: a hand-built
    /// Part-Blank pool, used by the socketing fixtures.
    /// </summary>
    internal sealed class FakePartBlankDataSource : IPartBlankDataSource
    {
        private readonly List<PartBlankData> _all = new List<PartBlankData>();
        private readonly Dictionary<string, PartBlankData> _byId = new Dictionary<string, PartBlankData>();

        public IReadOnlyList<PartBlankData> All => _all;

        public FakePartBlankDataSource Add(
            string definitionId, string slotId, string speciesArchetypeId, int socketCount)
        {
            var blank = new PartBlankData(definitionId, definitionId, slotId, speciesArchetypeId, socketCount);
            _all.Add(blank);
            _byId[definitionId] = blank;
            return this;
        }

        public bool TryGet(string definitionId, out PartBlankData blank)
        {
            blank = null;
            return definitionId != null && _byId.TryGetValue(definitionId, out blank);
        }
    }
}
