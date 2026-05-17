using Platform;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Interface for handling combat transitions from dialogue.
    /// Responsible for preparing enemy content and executing transitions.
    /// </summary>
    public interface ICombatTransitionHandler
    {
        /// <summary>
        /// Checks if the platform can transition to combat.
        /// </summary>
        /// <param name="platform">The current platform</param>
        /// <param name="context">The dialogue context</param>
        /// <param name="combatTriggered">Whether combat was explicitly triggered via Ink</param>
        /// <returns>True if combat transition is possible</returns>
        bool CanTransitionToCombat(IPlatform platform, IDialogueContext context, bool combatTriggered);

        /// <summary>
        /// Prepares enemy content from NPC content.
        /// </summary>
        /// <param name="npcContent">The NPC content to transition</param>
        /// <returns>The prepared enemy content, or null if not possible</returns>
        EnemyContent PrepareEnemyContent(NpcContent npcContent);

        /// <summary>
        /// Executes the combat transition.
        /// </summary>
        /// <param name="platform">The platform to transition</param>
        /// <param name="npcContent">Optional NPC content being transitioned</param>
        /// <param name="enemyContent">Optional enemy content prepared</param>
        void ExecuteTransition(IPlatform platform, NpcContent npcContent, EnemyContent enemyContent);
    }
}
