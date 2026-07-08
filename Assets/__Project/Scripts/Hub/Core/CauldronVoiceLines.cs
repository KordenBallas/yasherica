using System;
using System.Collections.Generic;

namespace Hub.Core
{
    /// <summary>
    /// UnityEngine-free line pools for the Hub's cauldron voice (O1), mapped from the
    /// <c>HubVoiceLinesConfig</c> SO. Part-pick lines may be keyed per race with a generic
    /// fallback; the other moments carry one pool each. Empty pools are valid — the voice
    /// simply stays quiet for that moment.
    /// </summary>
    public sealed class CauldronVoiceLines
    {
        private static readonly IReadOnlyList<string> NoLines = Array.Empty<string>();

        public static readonly CauldronVoiceLines Empty =
            new CauldronVoiceLines(null, null, null, null, null);

        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _partPickedByRace;
        private readonly IReadOnlyList<string> _partPickedGeneric;
        private readonly IReadOnlyList<string> _noPartAvailable;
        private readonly IReadOnlyList<string> _launch;
        private readonly IReadOnlyList<string> _deathReturn;
        private readonly IReadOnlyList<string> _heatDare;
        private readonly IReadOnlyList<string> _heatSealed;
        private readonly IReadOnlyList<string> _heatDeclined;

        public CauldronVoiceLines(
            IReadOnlyDictionary<string, IReadOnlyList<string>> partPickedByRace,
            IReadOnlyList<string> partPickedGeneric,
            IReadOnlyList<string> noPartAvailable,
            IReadOnlyList<string> launch,
            IReadOnlyList<string> deathReturn,
            IReadOnlyList<string> heatDare = null,
            IReadOnlyList<string> heatSealed = null,
            IReadOnlyList<string> heatDeclined = null)
        {
            _partPickedByRace = partPickedByRace
                                ?? new Dictionary<string, IReadOnlyList<string>>(0);
            _partPickedGeneric = partPickedGeneric ?? NoLines;
            _noPartAvailable = noPartAvailable ?? NoLines;
            _launch = launch ?? NoLines;
            _deathReturn = deathReturn ?? NoLines;
            _heatDare = heatDare ?? NoLines;
            _heatSealed = heatSealed ?? NoLines;
            _heatDeclined = heatDeclined ?? NoLines;
        }

        /// <summary>The pool for a moment; a part pick prefers the race's pool, falling back to
        /// the generic one. Never null (empty = quiet moment).</summary>
        public IReadOnlyList<string> PoolFor(CauldronVoiceMoment moment, string raceId = null)
        {
            switch (moment)
            {
                case CauldronVoiceMoment.PartPicked:
                    if (!string.IsNullOrEmpty(raceId)
                        && _partPickedByRace.TryGetValue(raceId, out var racePool)
                        && racePool != null && racePool.Count > 0)
                    {
                        return racePool;
                    }

                    return _partPickedGeneric;
                case CauldronVoiceMoment.NoPartAvailable:
                    return _noPartAvailable;
                case CauldronVoiceMoment.Launch:
                    return _launch;
                case CauldronVoiceMoment.DeathReturn:
                    return _deathReturn;
                case CauldronVoiceMoment.HeatDare:
                    return _heatDare;
                case CauldronVoiceMoment.HeatSealed:
                    return _heatSealed;
                case CauldronVoiceMoment.HeatDeclined:
                    return _heatDeclined;
                default:
                    return NoLines;
            }
        }
    }
}
