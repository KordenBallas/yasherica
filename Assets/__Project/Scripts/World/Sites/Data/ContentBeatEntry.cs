using System;
using UnityEngine;
using World.Sites.Core;

namespace World.Sites.Data
{
    /// <summary>One authored <c>base·flavor</c> beat reference (an anchor entry).</summary>
    [Serializable]
    public class ContentBeatEntry
    {
        [Tooltip("The stable base kind of the beat")]
        [SerializeField] private ContentBaseKind _kind = ContentBaseKind.Empty;
        [Tooltip("Open flavor tag refining the kind (e.g. market, bandit, townsfolk); empty = unflavored")]
        [SerializeField] private string _flavor = string.Empty;

        public ContentBaseKind Kind => _kind;
        public string Flavor => _flavor;
    }
}
