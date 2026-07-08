using System;
using System.Collections.Generic;
using Inventory.Core;
using MetaProgression.Core;
using Mutation.Core;
using Narrative.Facts.Core;
using Zenject;

namespace MetaProgression.Integration
{
    /// <summary>
    /// The artifact half of the cross-run direction ledger (meta-progression FR8): at the unseal
    /// commit (<see cref="ISocketingModel.OnSocketsConsumed"/> — the one point reagents are truly
    /// spent) every consumed artifact's definition id is recorded under the current run index. The
    /// function family is resolved at read time from the artifact catalog, so the ledger stores raw
    /// ids only.
    /// </summary>
    public sealed class SocketedArtifactsLedgerRecorder : IInitializable, IDisposable
    {
        private readonly ISocketingModel _socketing;
        private readonly IFactStore _facts;
        private readonly RunLedger _ledger;

        public SocketedArtifactsLedgerRecorder(ISocketingModel socketing, IFactStore facts, RunLedger ledger)
        {
            _socketing = socketing;
            _facts = facts;
            _ledger = ledger;
        }

        public void Initialize()
        {
            if (_socketing != null)
            {
                _socketing.OnSocketsConsumed += HandleConsumed;
            }
        }

        public void Dispose()
        {
            if (_socketing != null)
            {
                _socketing.OnSocketsConsumed -= HandleConsumed;
            }
        }

        private void HandleConsumed(IReadOnlyList<ArtifactInstance> consumed)
        {
            if (consumed == null)
            {
                return;
            }

            int runIndex = (int)_facts.GetInt(WorldFacts.RunCount);
            foreach (var artifact in consumed)
            {
                if (artifact != null)
                {
                    _ledger.RecordSocketedArtifact(runIndex, artifact.DefinitionId);
                }
            }
        }
    }
}
