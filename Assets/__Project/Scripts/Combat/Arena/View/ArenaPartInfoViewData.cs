using System;
using System.Collections.Generic;
using UI.AbilityPreview;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>One ability row on the part-info popover + its shared-preview payload.</summary>
    public readonly struct ArenaPartAbilityViewData
    {
        public string Label { get; }
        public Sprite Icon { get; }
        public AbilityPreviewData Preview { get; }

        public ArenaPartAbilityViewData(string label, Sprite icon, AbilityPreviewData preview)
        {
            Label = label;
            Icon = icon;
            Preview = preview;
        }
    }

    /// <summary>
    /// Everything the part-info popover shows for one clicked part (board entry or hero
    /// part): identity, its ability rows, whether the Draft button applies, and the note
    /// line (turn state / rejection reason — G4 req 7 visible rejection).
    /// </summary>
    public class ArenaPartInfoViewData
    {
        public string PartName { get; }
        public string SlotLabel { get; }
        public IReadOnlyList<ArenaPartAbilityViewData> Abilities { get; }
        public bool CanDraft { get; }
        public string Note { get; }

        public ArenaPartInfoViewData(
            string partName,
            string slotLabel,
            IReadOnlyList<ArenaPartAbilityViewData> abilities,
            bool canDraft,
            string note)
        {
            PartName = partName;
            SlotLabel = slotLabel;
            Abilities = abilities ?? Array.Empty<ArenaPartAbilityViewData>();
            CanDraft = canDraft;
            Note = note ?? string.Empty;
        }
    }
}
