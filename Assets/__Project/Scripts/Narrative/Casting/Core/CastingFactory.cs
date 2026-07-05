using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Director.Core;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using Narrative.Stories.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// Default <see cref="ICastingFactory"/>. Slot fill (R5): for each slot, gather library fragments
    /// carrying all required tags (already in stable id order), then pick one via the seeded stream
    /// (D1). The dialogue slot is required; quest/combat slots are optional. Builds the context bag
    /// (<c>$self</c>/<c>$faction</c>, <c>npc_name</c>, and the W2-2 availability vars) and records which
    /// optional slots were filled.
    /// </summary>
    public sealed class CastingFactory : ICastingFactory
    {
        private readonly IRandomSource _random;
        private readonly IGameLogger _logger;

        public CastingFactory(IRandomSource random, IGameLogger logger = null)
        {
            _random = random;
            _logger = logger;
        }

        public Casting Cast(StoryTemplateData story, NpcInstance actor, IFragmentLibrary library)
        {
            if (story == null || actor == null || library == null)
            {
                return null;
            }

            DialogueData dialogue = null;
            QuestData quest = null;
            string enemyId = null;

            foreach (var slot in story.Slots)
            {
                switch (slot.Kind)
                {
                    case SlotKind.Dialogue:
                        dialogue = PickById(library.FindDialogues(slot.RequiredTags));
                        if (dialogue == null && !slot.Optional)
                        {
                            _logger?.Warning(LogCategory.Narrative,$"[CastingFactory] Story '{story.StoryId}' dialogue slot '{slot.SlotId}' has no matching fragment.");
                            return null;
                        }

                        break;
                    case SlotKind.Quest:
                        quest = PickById(library.FindQuests(slot.RequiredTags));
                        if (quest == null && !slot.Optional)
                        {
                            _logger?.Warning(LogCategory.Narrative,$"[CastingFactory] Story '{story.StoryId}' quest slot '{slot.SlotId}' has no matching fragment.");
                            return null;
                        }

                        break;
                    case SlotKind.Combat:
                        var enemy = PickById(library.FindEnemies(slot.RequiredTags));
                        enemyId = enemy?.EnemyId;
                        if (enemy == null && !slot.Optional)
                        {
                            _logger?.Warning(LogCategory.Narrative,$"[CastingFactory] Story '{story.StoryId}' combat slot '{slot.SlotId}' has no matching fragment.");
                            return null;
                        }

                        break;
                }
            }

            if (dialogue == null)
            {
                _logger?.Warning(LogCategory.Narrative,$"[CastingFactory] Story '{story.StoryId}' produced no dialogue - casting aborted.");
                return null;
            }

            var context = new ContextBag()
                .BindSubject("$self", actor.InstanceId)
                .BindSubject("$faction", actor.FactionId)
                .SetVariable("npc_name", actor.ChosenDisplayName)
                .SetVariable("quest_available", quest != null)
                .SetVariable("combat_available", !string.IsNullOrEmpty(enemyId));

            return new Casting(actor, dialogue, quest, enemyId, context, story.StoryId, story.ThreadId);
        }

        public IReadOnlyList<FactKeyShapeCore> DeriveFootprint(StoryTemplateData story, IFragmentLibrary library)
        {
            var shapes = new List<FactKeyShapeCore>();
            if (story == null)
            {
                return shapes;
            }

            foreach (var effect in story.OwnEffects)
            {
                AddShape(shapes, effect.Shape);
            }

            if (library != null)
            {
                foreach (var slot in story.Slots)
                {
                    CollectSlotFootprint(shapes, slot, library);
                }
            }

            story.SetDerivedFootprint(shapes);
            return shapes;
        }

        private void CollectSlotFootprint(List<FactKeyShapeCore> shapes, StorySlot slot, IFragmentLibrary library)
        {
            switch (slot.Kind)
            {
                case SlotKind.Dialogue:
                    var dialogues = library.FindDialogues(slot.RequiredTags);
                    WarnIfEmpty(slot, dialogues.Count);
                    foreach (var d in dialogues)
                    {
                        foreach (var shape in d.DeclaredFactWrites)
                        {
                            AddShape(shapes, shape);
                        }
                    }

                    break;
                case SlotKind.Quest:
                    var quests = library.FindQuests(slot.RequiredTags);
                    WarnIfEmpty(slot, quests.Count);
                    foreach (var q in quests)
                    {
                        foreach (var shape in q.Footprint)
                        {
                            AddShape(shapes, shape);
                        }
                    }

                    break;
                case SlotKind.Combat:
                    WarnIfEmpty(slot, library.FindEnemies(slot.RequiredTags).Count);
                    break;
            }
        }

        private void WarnIfEmpty(StorySlot slot, int candidateCount)
        {
            if (candidateCount == 0 && !slot.Optional)
            {
                _logger?.Warning(LogCategory.Narrative,$"[CastingFactory] Slot '{slot.SlotId}' ({slot.Kind}) has zero matching candidates.");
            }
        }

        private static void AddShape(List<FactKeyShapeCore> shapes, FactKeyShapeCore shape)
        {
            if (!shapes.Contains(shape))
            {
                shapes.Add(shape);
            }
        }

        private T PickById<T>(IReadOnlyList<T> matches) where T : class
        {
            if (matches == null || matches.Count == 0)
            {
                return null;
            }

            if (matches.Count == 1)
            {
                return matches[0];
            }

            // Candidates already arrive in stable id order; the seeded stream breaks the tie (D1).
            return matches[_random.NextInt(matches.Count)];
        }
    }
}
