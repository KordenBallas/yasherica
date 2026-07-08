using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Decorates the blank catalog with the run's socket cut (heat-ascension FR8, "thinner
    /// medallions"): every consumer — the socketing model, the medallion gem ring, the variant
    /// presenter, quest-reward pools — sees the SAME reduced count, because they all read this one
    /// source. Cut 0 passes the inner records through untouched (Heat 0 zero-diff); the reduced
    /// count floors at one so every blank stays socketable.
    /// </summary>
    public sealed class SocketAdjustedBlankSource : IPartBlankDataSource
    {
        private const int MinSockets = 1;

        private readonly IPartBlankDataSource _inner;
        private readonly MutationRuleModifiers _rules;

        private IReadOnlyList<PartBlankData> _all;

        public SocketAdjustedBlankSource(IPartBlankDataSource inner, MutationRuleModifiers rules = null)
        {
            _inner = inner;
            _rules = rules ?? MutationRuleModifiers.Neutral;
        }

        public IReadOnlyList<PartBlankData> All
        {
            get
            {
                if (_all == null)
                {
                    if (_rules.SocketCut == 0)
                    {
                        _all = _inner.All;
                    }
                    else
                    {
                        var adjusted = new List<PartBlankData>(_inner.All.Count);
                        foreach (var blank in _inner.All)
                        {
                            adjusted.Add(Adjust(blank));
                        }

                        _all = adjusted;
                    }
                }

                return _all;
            }
        }

        public bool TryGet(string definitionId, out PartBlankData blank)
        {
            if (!_inner.TryGet(definitionId, out var inner))
            {
                blank = null;
                return false;
            }

            blank = _rules.SocketCut == 0 ? inner : Adjust(inner);
            return true;
        }

        private PartBlankData Adjust(PartBlankData blank)
        {
            int cut = blank.SocketCount - _rules.SocketCut;
            return new PartBlankData(
                blank.DefinitionId,
                blank.DisplayName,
                blank.SlotId,
                blank.SpeciesArchetypeId,
                cut < MinSockets ? MinSockets : cut,
                blank.RaceId,
                blank.Gate);
        }
    }
}
