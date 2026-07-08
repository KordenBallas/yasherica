using System;
using System.Collections.Generic;
using Heat.Core;

namespace Hub.Core
{
    /// <summary>
    /// The pact under construction at the cauldron (heat-ascension FR1–FR3): per-modifier ranks the
    /// player cycles, the live total, and the staging events the voice reacts to. Cycling walks
    /// 0→1→…→max→0; a step that would exceed the soft cap wraps to 0 early instead of refusing, so
    /// every modifier stays cyclable (dialing DOWN is always allowed, FR3) and the cap always holds.
    /// Pure C# domain; the presenter drives it from view events and seals it at panel close.
    /// </summary>
    public sealed class HubHeatModel
    {
        private readonly HeatSettings _settings;
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();

        public HubHeatModel(HeatSettings settings)
        {
            _settings = settings ?? HeatSettings.Defaults;
        }

        /// <summary>The authored modifier menu the panel deals.</summary>
        public IReadOnlyList<HeatModifier> Menu => _settings.Modifiers;

        /// <summary>Fired when the pact panel opens (the cauldron speaks its dare).</summary>
        public event Action Opened;

        /// <summary>Fired on every rank change (the dig re-deals under the new heat).</summary>
        public event Action PactChanged;

        /// <summary>Fired when the panel closes on the seal card; payload = the total Heat.</summary>
        public event Action<int> PactSealed;

        public int RankOf(string modifierId)
        {
            return _ranks.TryGetValue(modifierId ?? string.Empty, out int rank) ? rank : 0;
        }

        /// <summary>The soft cap the pact refuses to exceed; 0 = uncapped.</summary>
        public int SoftCap => _settings.SoftCapTotalHeat;

        /// <summary>
        /// True when the modifier's next cycle step would be refused by the soft cap (and wrap to 0
        /// instead of stepping up) — the presenter marks such cards so the refusal is never silent.
        /// </summary>
        public bool NextStepIsCapped(string modifierId)
        {
            if (!_settings.TryGetModifier(modifierId, out var modifier))
            {
                return false;
            }

            int next = RankOf(modifierId) + 1;
            return next <= modifier.MaxRank && ExceedsSoftCap(modifier, next);
        }

        /// <summary>The live total Heat of the pact as currently dialed.</summary>
        public int TotalHeat
        {
            get
            {
                int total = 0;
                foreach (var pair in _ranks)
                {
                    if (_settings.TryGetModifier(pair.Key, out var modifier))
                    {
                        total += modifier.HeatAtRank(pair.Value);
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Cycles the modifier one rank step (0→1→…→max→0). A step the soft cap cannot afford wraps
        /// to 0 early. Returns the new rank; unknown ids are a no-op returning 0.
        /// </summary>
        public int CycleRank(string modifierId)
        {
            if (!_settings.TryGetModifier(modifierId, out var modifier))
            {
                return 0;
            }

            int current = RankOf(modifierId);
            int next = current + 1;
            if (next > modifier.MaxRank || ExceedsSoftCap(modifier, next))
            {
                next = 0;
            }

            if (next == current)
            {
                return current;
            }

            _ranks[modifier.Id] = next;
            PactChanged?.Invoke();
            return next;
        }

        public HeatPact BuildPact()
        {
            return HeatPact.From(_settings, _ranks);
        }

        public void NotifyOpened()
        {
            Opened?.Invoke();
        }

        public void Seal()
        {
            PactSealed?.Invoke(TotalHeat);
        }

        private bool ExceedsSoftCap(HeatModifier modifier, int candidateRank)
        {
            if (_settings.SoftCapTotalHeat <= 0)
            {
                return false;
            }

            int totalWithCandidate = TotalHeat - modifier.HeatAtRank(RankOf(modifier.Id))
                + modifier.HeatAtRank(candidateRank);
            return totalWithCandidate > _settings.SoftCapTotalHeat;
        }
    }
}
