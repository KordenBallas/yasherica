using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Stories.Data
{
    /// <summary>
    /// The only bridge from <see cref="StoryTemplate"/> to the UnityEngine-free
    /// <see cref="StoryTemplateData"/> Core record. **Pure (W3-2):** maps the template's own fields
    /// only and does NOT populate the derived effect footprint — that union needs the whole fragment
    /// library, which the casting/planning component owns.
    /// </summary>
    public static class StoryTemplateMapper
    {
        public static StoryTemplateData ToData(StoryTemplate template)
        {
            if (template == null)
            {
                return null;
            }

            var slots = new List<StorySlot>();
            foreach (var slot in template.Slots)
            {
                if (slot == null)
                {
                    continue;
                }

                slots.Add(new StorySlot(slot.SlotId, slot.Kind, new List<string>(slot.RequiredTags), slot.Optional));
            }

            var preconditions = new List<FactPredicate>();
            foreach (var predicate in template.Preconditions)
            {
                if (predicate != null)
                {
                    preconditions.Add(predicate.ToCore());
                }
            }

            var ownEffects = new List<FactEffectCore>();
            foreach (var effect in template.OwnEffects)
            {
                if (effect != null)
                {
                    ownEffects.Add(effect.ToCore());
                }
            }

            return new StoryTemplateData(
                template.StoryId,
                slots,
                preconditions,
                ownEffects,
                new List<string>(template.StoryTags),
                template.ThreadId,
                template.IsSpine,
                template.Weight);
        }
    }
}
