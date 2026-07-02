using System;
using System.Collections.Generic;
using Inventory.Core;

namespace Mutation.Core
{
    /// <summary>
    /// Recomputes a blank's socketed trend (combined post-grammar trait profile)
    /// whenever its sockets change and publishes it through
    /// <see cref="ISocketingTrendSource"/>. Pure C#; lifecycle-managed by Zenject
    /// (subscribes on Initialize, unsubscribes on Dispose).
    /// </summary>
    public class SocketingTrendEvaluator : ISocketingTrendSource, IDisposable
    {
        private readonly ISocketingModel _socketing;
        private readonly IArtifactTraitSource _traitSource;
        private readonly EmergentFusionCalculator _fusionCalculator;
        private readonly TraitFusionRuleSet _fusionRules;
        private readonly FusionSettings _fusionSettings;

        public event Action<SocketingTrend> OnTrendChanged;

        public SocketingTrendEvaluator(
            ISocketingModel socketing,
            IArtifactTraitSource traitSource,
            EmergentFusionCalculator fusionCalculator,
            TraitFusionRuleSet fusionRules,
            FusionSettings fusionSettings)
        {
            _socketing = socketing ?? throw new ArgumentNullException(nameof(socketing));
            _traitSource = traitSource ?? throw new ArgumentNullException(nameof(traitSource));
            _fusionCalculator = fusionCalculator ?? throw new ArgumentNullException(nameof(fusionCalculator));
            _fusionRules = fusionRules ?? throw new ArgumentNullException(nameof(fusionRules));
            _fusionSettings = fusionSettings ?? throw new ArgumentNullException(nameof(fusionSettings));

            _socketing.OnSocketsChanged += HandleSocketsChanged;
        }

        public void Dispose()
        {
            _socketing.OnSocketsChanged -= HandleSocketsChanged;
        }

        private void HandleSocketsChanged(int blankInstanceId)
        {
            var socketed = _socketing.SocketedArtifacts(blankInstanceId);
            var profiles = new List<ArtifactTraitProfile>(socketed.Count);
            foreach (var artifact in socketed)
            {
                if (_traitSource.TryGetProfile(artifact.DefinitionId, out var profile))
                {
                    profiles.Add(profile);
                }
            }

            var target = _fusionCalculator.ComputeTarget(profiles, _fusionRules, _fusionSettings);
            OnTrendChanged?.Invoke(new SocketingTrend(blankInstanceId, target.Traits, target.Tier));
        }
    }
}
