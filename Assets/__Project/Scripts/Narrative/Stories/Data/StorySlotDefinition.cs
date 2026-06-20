using System;
using System.Collections.Generic;
using Narrative.Stories.Core;
using UnityEngine;

namespace Narrative.Stories.Data
{
    /// <summary>
    /// Authoring form of one typed story slot (R5). Fragments are matched into it by
    /// <see cref="RequiredTags"/>, never by hard reference. Serialized on <see cref="StoryTemplate"/>.
    /// </summary>
    [Serializable]
    public class StorySlotDefinition
    {
        [SerializeField] private string _slotId;
        [SerializeField] private SlotKind _kind = SlotKind.Dialogue;
        [Tooltip("A fragment must carry all of these tags to fill the slot")]
        [SerializeField] private List<string> _requiredTags = new List<string>();
        [SerializeField] private bool _optional;

        public string SlotId => _slotId;
        public SlotKind Kind => _kind;
        public IReadOnlyList<string> RequiredTags => _requiredTags;
        public bool Optional => _optional;
    }
}
