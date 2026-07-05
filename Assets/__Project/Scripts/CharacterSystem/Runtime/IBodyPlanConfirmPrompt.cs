using System;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Confirm-before-commit prompt for a body-plan change that sheds parts (FR7).
    /// Implemented by the UI presenter; the coordinator treats it as optional — when no
    /// prompt is bound (demo scenes) the change auto-confirms with a logged warning.
    /// </summary>
    public interface IBodyPlanConfirmPrompt
    {
        /// <summary>Shows the summary and calls <paramref name="decision"/> exactly once:
        /// true = commit the change, false = leave the body untouched.</summary>
        void Request(BodyPlanChangeSummary summary, Action<bool> decision);
    }
}
