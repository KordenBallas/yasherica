using System;
using UnityEngine;
using World.Sites.Core;

namespace World.Sites.Data
{
    /// <summary>One authored fill-table row: a <c>base·flavor</c> beat and its relative draw weight.</summary>
    [Serializable]
    public class WeightedBeatEntry
    {
        [Tooltip("The stable base kind of the beat")]
        [SerializeField] private ContentBaseKind _kind = ContentBaseKind.Empty;
        [Tooltip("Open flavor tag refining the kind (e.g. market, bandit, townsfolk); empty = unflavored")]
        [SerializeField] private string _flavor = string.Empty;
        [Tooltip("Relative draw weight among this table's rows")]
        [Min(0)]
        [SerializeField] private int _weight = 1;

        public ContentBaseKind Kind => _kind;
        public string Flavor => _flavor;
        public int Weight => _weight;
    }
}
