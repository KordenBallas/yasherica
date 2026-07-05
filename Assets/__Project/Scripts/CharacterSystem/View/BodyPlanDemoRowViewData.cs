using System;
using System.Collections.Generic;

namespace CharacterSystem.View
{
    /// <summary>
    /// One row of the body-plan demo console: a slot with its selectable parts.
    /// <see cref="OptionPartIds"/> runs parallel to <see cref="OptionLabels"/>; a null/empty
    /// part id marks the "(empty)" placeholder shown while the slot has no occupant.
    /// </summary>
    public sealed class BodyPlanDemoRowViewData
    {
        public string SlotId { get; }
        public string SlotLabel { get; }
        public IReadOnlyList<string> OptionLabels { get; }
        public IReadOnlyList<string> OptionPartIds { get; }
        public int SelectedIndex { get; }

        public BodyPlanDemoRowViewData(
            string slotId,
            string slotLabel,
            IReadOnlyList<string> optionLabels,
            IReadOnlyList<string> optionPartIds,
            int selectedIndex)
        {
            SlotId = slotId;
            SlotLabel = slotLabel;
            OptionLabels = optionLabels ?? Array.Empty<string>();
            OptionPartIds = optionPartIds ?? Array.Empty<string>();
            SelectedIndex = selectedIndex;
        }
    }
}
