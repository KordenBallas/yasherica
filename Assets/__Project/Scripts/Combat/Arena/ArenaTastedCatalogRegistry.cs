using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;

namespace Combat.Arena
{
    /// <summary>
    /// Host-side store of every participant's tasted-forms catalog (P4-5 req 5): clients submit
    /// their part-id lists after connecting; at draft start the host takes the union for the
    /// board composer. Purely additive — a re-submission replaces that client's list.
    /// </summary>
    public class ArenaTastedCatalogRegistry : IDisposable
    {
        private readonly IArenaTransport _transport;
        private readonly Dictionary<ulong, List<string>> _catalogsByClientId =
            new Dictionary<ulong, List<string>>();

        public ArenaTastedCatalogRegistry(IArenaTransport transport)
        {
            _transport = transport;
            _transport.TastedCatalogReceived += HandleCatalogReceived;
        }

        public void Dispose()
        {
            _transport.TastedCatalogReceived -= HandleCatalogReceived;
        }

        /// <summary>The distinct, ordinal-sorted union of the given participants' catalogs.</summary>
        public IReadOnlyList<string> UnionFor(IEnumerable<ulong> clientIds)
        {
            var union = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var clientId in clientIds)
            {
                if (_catalogsByClientId.TryGetValue(clientId, out var catalog))
                {
                    foreach (var partId in catalog)
                    {
                        union.Add(partId);
                    }
                }
            }

            return union.ToList();
        }

        private void HandleCatalogReceived(ulong clientId, IReadOnlyList<string> partIds)
        {
            _catalogsByClientId[clientId] =
                partIds?.Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
        }
    }
}
