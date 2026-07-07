using System.Collections.Generic;
using Narrative.Barks.Core;

namespace Narrative.Barks.Data
{
    /// <summary>
    /// The only bridge from <see cref="CauldronBarkLinesConfig"/> to the UnityEngine-free
    /// <see cref="CauldronBarkLines"/> record, run once at install time. A null config maps to
    /// <see cref="CauldronBarkLines.Empty"/> (a quiet cauldron, never an error).
    /// </summary>
    public static class CauldronBarkLinesMapper
    {
        public static CauldronBarkLines ToLines(CauldronBarkLinesConfig config)
        {
            if (config == null)
            {
                return CauldronBarkLines.Empty;
            }

            var pools = new Dictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>>
            {
                { (CauldronBarkSlot.Temptation, BarkLean.Indulgent), Clean(config.TemptationIndulgent) },
                { (CauldronBarkSlot.Temptation, BarkLean.Restrained), Clean(config.TemptationRestrained) },
                { (CauldronBarkSlot.DarkOffer, BarkLean.Indulgent), Clean(config.DarkOfferIndulgent) },
                { (CauldronBarkSlot.DarkOffer, BarkLean.Restrained), Clean(config.DarkOfferRestrained) },
                { (CauldronBarkSlot.Restraint, BarkLean.Indulgent), Clean(config.RestraintIndulgent) },
                { (CauldronBarkSlot.Restraint, BarkLean.Restrained), Clean(config.RestraintRestrained) },
                { (CauldronBarkSlot.SocketingTrend, BarkLean.Indulgent), Clean(config.SocketingTrendIndulgent) },
                { (CauldronBarkSlot.SocketingTrend, BarkLean.Restrained), Clean(config.SocketingTrendRestrained) }
            };

            return new CauldronBarkLines(pools, config.DarkBelongingIds);
        }

        private static IReadOnlyList<string> Clean(IReadOnlyList<string> lines)
        {
            var result = new List<string>();
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        result.Add(line);
                    }
                }
            }

            return result;
        }
    }
}
