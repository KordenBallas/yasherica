using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue.Core;
using Narrative.Quests.Core;
// Inside Narrative.* the bare name "Casting" binds to the Narrative.Casting NAMESPACE, so the
// recombination type needs an explicit alias here.
using CastingBinding = Narrative.Casting.Core.Casting;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Pure bridge between a realized <see cref="Casting"/> and its serializable snapshot. Restore
    /// resolves every fragment BY ID (actor from the restored live-actor registry, dialogue/quest
    /// from the fragment library) and rebuilds the context bag by the exact
    /// <see cref="CastingFactory"/> recipe — consuming ZERO draws from the shared random stream, so
    /// re-realizing already-planned story platforms cannot perturb the restored RNG state (D5).
    /// </summary>
    public static class CastingSnapshotMapper
    {
        public static CastingSnapshot Capture(CastingBinding casting)
        {
            if (casting == null)
            {
                return null;
            }

            var snapshot = new CastingSnapshot
            {
                ActorInstanceId = casting.Actor?.InstanceId ?? string.Empty,
                DialogueId = casting.Dialogue?.DialogueId ?? string.Empty,
                QuestId = casting.OptionalQuest?.QuestId ?? string.Empty,
                EnemyId = casting.OptionalEnemyId ?? string.Empty,
                StoryId = casting.StoryId,
                ThreadId = casting.ThreadId
            };

            // Flattened for file readability; restore derives the bag instead of reading these.
            foreach (var token in new[] { "$self", "$faction" })
            {
                if (casting.Context.TryGet(token, out var subject))
                {
                    snapshot.ContextSubjectTokens.Add(token);
                    snapshot.ContextSubjectValues.Add(subject);
                }
            }

            return snapshot;
        }

        /// <summary>Rebuilds the casting, or null (logged) when a referenced fragment/actor no longer
        /// exists — the caller degrades that platform rather than crashing (FR14).</summary>
        public static CastingBinding Restore(CastingSnapshot snapshot, ILiveActorRegistry actors,
            IFragmentLibrary library, IGameLogger logger = null)
        {
            if (snapshot == null || actors == null || library == null)
            {
                return null;
            }

            var actor = FindActor(actors, snapshot.ActorInstanceId);
            var dialogue = FindDialogue(library, snapshot.DialogueId);
            if (actor == null || dialogue == null)
            {
                logger?.Warning(LogCategory.Persistence,
                    $"[CastingSnapshotMapper] Cannot restore casting (actor '{snapshot.ActorInstanceId}', " +
                    $"dialogue '{snapshot.DialogueId}'): missing from the run registries/library.");
                return null;
            }

            var quest = FindQuest(library, snapshot.QuestId);
            var enemyId = string.IsNullOrEmpty(snapshot.EnemyId) ? null : snapshot.EnemyId;

            // The factory's canonical bag, rebuilt without touching the seeded stream.
            var context = new ContextBag()
                .BindSubject("$self", actor.InstanceId)
                .BindSubject("$faction", actor.FactionId)
                .SetVariable("npc_name", actor.ChosenDisplayName)
                .SetVariable("quest_available", quest != null)
                .SetVariable("combat_available", !string.IsNullOrEmpty(enemyId));

            return new CastingBinding(actor, dialogue, quest, enemyId, context, snapshot.StoryId, snapshot.ThreadId);
        }

        private static NpcInstance FindActor(ILiveActorRegistry actors, string instanceId)
        {
            foreach (var actor in actors.LiveActors)
            {
                if (string.Equals(actor.InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return actor;
                }
            }

            return null;
        }

        private static DialogueData FindDialogue(IFragmentLibrary library, string dialogueId)
        {
            foreach (var dialogue in library.FindDialogues(null))
            {
                if (string.Equals(dialogue.DialogueId, dialogueId, StringComparison.Ordinal))
                {
                    return dialogue;
                }
            }

            return null;
        }

        private static QuestData FindQuest(IFragmentLibrary library, string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            foreach (var quest in library.FindQuests(null))
            {
                if (string.Equals(quest.QuestId, questId, StringComparison.Ordinal))
                {
                    return quest;
                }
            }

            return null;
        }
    }
}
