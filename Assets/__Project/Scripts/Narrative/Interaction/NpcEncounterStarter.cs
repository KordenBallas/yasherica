using Core.Logging;
using Narrative.Interaction.Core;
using Platform;

namespace Narrative.Interaction
{
    /// <summary>
    /// The seam from proximity into the platform state machine. Talking transitions the NPC's platform
    /// into the dialogue state, which reuses the placement-time casting; aggro spawns the cast enemy and
    /// transitions straight into combat — the exact pattern <see cref="DialogueActiveState"/> uses for an
    /// in-dialogue combat trigger, minus the dialogue. Either way the handle is consumed so it cannot
    /// re-trigger, and its quest marker clears.
    /// </summary>
    public sealed class NpcEncounterStarter
    {
        private readonly IGameLogger _logger;

        public NpcEncounterStarter(IGameLogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>Opens the NPC's conversation (quest-bearer / plain): the F-prompt path.</summary>
        public void StartTalk(NpcInteractionHandle handle)
        {
            if (handle?.Platform == null || handle.Consumed)
            {
                return;
            }

            handle.Consumed = true;
            handle.View?.SetIntentMarker(NpcIntent.Plain); // a started conversation is no longer "on offer"
            handle.View?.ShowPrompt(false);

            var platform = handle.Platform;
            platform.TransitionToState(platform.StateFactory.CreateDialogueState());
        }

        /// <summary>Starts the battle on approach for a hostile NPC: no prompt, no dialogue.</summary>
        public void StartAggro(NpcInteractionHandle handle)
        {
            if (handle?.Platform == null || handle.Consumed)
            {
                return;
            }

            var enemyId = handle.Casting?.OptionalEnemyId;
            if (!int.TryParse(enemyId, out int parsedId))
            {
                _logger?.Error(LogCategory.Narrative,$"[NpcEncounterStarter] Hostile NPC '{handle.Id}' has non-numeric enemy id '{enemyId}' — cannot aggro.");
                return;
            }

            handle.Consumed = true;
            handle.View?.SetIntentMarker(NpcIntent.Plain);
            handle.View?.ShowPrompt(false);

            var platform = handle.Platform;
            platform.AddContent(new EnemyContent { EnemyId = parsedId });
            handle.Npc?.DestroyNpcVisual(); // unbinds the handle + view; the arena takes over
            platform.TransitionToState(platform.StateFactory.CreateActiveState(platform, ContentType.Enemy));
        }
    }
}
