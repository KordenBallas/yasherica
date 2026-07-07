using System;
using Mutation.Core;
using Narrative.Barks.Core;
using Zenject;

namespace Narrative.Barks
{
    /// <summary>
    /// Fires the socketing-trend bark slot off the existing <see cref="ISocketingTrendSource"/> seam
    /// (P1-10 / Track J's J-craft channel): whenever a readable reagent direction forms on a blank,
    /// the cauldron needles the player about what they're becoming. Consumes the seam only — the
    /// trend detection itself stays Track J's.
    /// </summary>
    public sealed class SocketingTrendBarkTrigger : IInitializable, IDisposable
    {
        private readonly ISocketingTrendSource _trends;
        private readonly ICauldronBarkService _barks;

        public SocketingTrendBarkTrigger(ISocketingTrendSource trends, ICauldronBarkService barks)
        {
            _trends = trends;
            _barks = barks;
        }

        public void Initialize()
        {
            _trends.OnTrendChanged += HandleTrendChanged;
        }

        public void Dispose()
        {
            _trends.OnTrendChanged -= HandleTrendChanged;
        }

        private void HandleTrendChanged(SocketingTrend trend)
        {
            // Only a formed direction is worth a remark; an emptied blank stays quiet.
            if (trend != null && trend.TraitIds != null && trend.TraitIds.Count > 0)
            {
                _barks.Bark(CauldronBarkSlot.SocketingTrend);
            }
        }
    }
}
