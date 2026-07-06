using System.Collections.Generic;
using Combat.Core;
using Core.Logging;
using Narrative.Interaction.Core;
using Platform;

namespace Narrative.Interaction
{
    /// <summary>
    /// The seam from proximity into the platform state machine. Talking transitions the NPC's platform
    /// into the dialogue state, which reuses the placement-time casting; aggro spawns the cast enemy and
    /// transitions straight into combat — the exact pattern <see cref="DialogueActiveState"/> uses for an
    /// in-dialogue combat trigger, minus the dialogue. A lone enemy body's handle engages its whole
    /// platform: every <see cref="EnemyContent"/> is latched (<c>Engaged</c>) and its sibling handles are
    /// consumed, so the fight starts once and landing never starts it. Either way the handle is consumed
    /// so it cannot re-trigger, and its quest marker clears.
    /// </summary>
    public sealed class NpcEncounterStarter
    {
        private readonly INpcInteractionRegistry _registry;
        private readonly IGameLogger _logger;

        public NpcEncounterStarter(INpcInteractionRegistry registry = null, IGameLogger logger = null)
        {
            _registry = registry;
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
            if (handle.Npc != null)
            {
                handle.Npc.EncounterStarted = true; // a boss-gated camp platform may auto-combat from here on
            }
            handle.View?.SetIntentMarker(NpcIntent.Plain); // a started conversation is no longer "on offer"
            handle.View?.ShowPrompt(false);

            var platform = handle.Platform;
            platform.TransitionToState(platform.StateFactory.CreateDialogueState());
        }

        /// <summary>Starts the battle on approach for a hostile NPC or a lone enemy body: no prompt, no dialogue.</summary>
        public void StartAggro(NpcInteractionHandle handle)
        {
            if (handle?.Platform == null || handle.Consumed)
            {
                return;
            }

            if (handle.Enemy != null)
            {
                EngageEnemyPlatform(handle);
                return;
            }

            var enemyId = handle.Casting?.OptionalEnemyId;
            if (!int.TryParse(enemyId, out int parsedId))
            {
                _logger?.Error(LogCategory.Narrative,$"[NpcEncounterStarter] Hostile NPC '{handle.Id}' has non-numeric enemy id '{enemyId}' — cannot aggro.");
                return;
            }

            handle.Consumed = true;
            if (handle.Npc != null)
            {
                handle.Npc.EncounterStarted = true; // a boss-gated camp platform may auto-combat from here on
            }
            handle.View?.SetIntentMarker(NpcIntent.Plain);
            handle.View?.ShowPrompt(false);

            var platform = handle.Platform;
            // Ambush: the enemy caused the fight, so the enemy leads the opening round (D2).
            platform.AddContent(new EnemyContent { EnemyId = parsedId, Engaged = true, Initiator = CombatInitiator.Enemy });
            EngageAllEnemies(platform); // the crew behind a camp boss joins his one fight
            handle.Npc?.DestroyNpcVisual(); // unbinds the handle + view; the arena takes over
            platform.TransitionToState(platform.StateFactory.CreateActiveState(platform, ContentType.Enemy));
        }

        /// <summary>
        /// A lone enemy's aggro radius was crossed: latch every enemy on the platform, consume and clear
        /// every sibling enemy-body handle (one trigger = one fight), then enter combat.
        /// </summary>
        private void EngageEnemyPlatform(NpcInteractionHandle handle)
        {
            var platform = handle.Platform;
            EngageAllEnemies(platform);

            if (_registry != null)
            {
                // Snapshot: unregistering mutates the live list.
                var siblings = new List<NpcInteractionHandle>(_registry.Handles);
                foreach (var sibling in siblings)
                {
                    if (sibling.Enemy != null && ReferenceEquals(sibling.Platform, platform))
                    {
                        sibling.Consumed = true;
                        sibling.View?.DestroyView(); // no overhead markers inside the battle
                        _registry.Unregister(sibling.Id);
                    }
                }
            }
            else
            {
                handle.Consumed = true;
                handle.View?.DestroyView();
            }

            platform.TransitionToState(platform.StateFactory.CreateActiveState(platform, ContentType.Enemy));
        }

        private static void EngageAllEnemies(IPlatform platform)
        {
            foreach (var content in platform.Contents)
            {
                if (content is EnemyContent enemy)
                {
                    enemy.Engaged = true;
                }
            }
        }
    }
}
