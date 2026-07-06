using System.Collections.Generic;

namespace Platform
{
    /// <summary>
    /// The landing rule for combat platforms (bandit-camp brief req 10, extended by PO decision
    /// 2026-07-05 to ALL combat content): landing on a platform never starts a fight. Combat is
    /// entered by engagement only — a lone enemy's aggro radius, the camp boss's circle, or a
    /// dialogue combat branch — which latches <see cref="EnemyContent.Engaged"/>. Activation
    /// auto-enters combat only for an already-engaged platform (re-landing resumes the battle).
    /// Pure logic, no Unity.
    /// </summary>
    public static class CombatAutoStartRule
    {
        public static bool ShouldAutoStartCombat(IReadOnlyList<IPlatformContent> contents)
        {
            if (contents == null)
            {
                return false;
            }

            for (int i = 0; i < contents.Count; i++)
            {
                if (contents[i] is EnemyContent enemy && enemy.Engaged)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
