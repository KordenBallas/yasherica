using System;
using System.Collections.Generic;

namespace Loot.Core
{
    /// <summary>
    /// Player progression snapshot passed into loot rolls.
    /// Reserved for future pool filtering (level, achievements, quest history);
    /// no filter consumes it yet.
    /// </summary>
    public class PlayerLootState
    {
        public static readonly PlayerLootState Empty = new PlayerLootState(0, null, null);

        public int Level { get; }
        public IReadOnlyList<string> CompletedQuestIds { get; }
        public IReadOnlyList<string> AchievementIds { get; }

        public PlayerLootState(
            int level,
            IReadOnlyList<string> completedQuestIds,
            IReadOnlyList<string> achievementIds)
        {
            Level = level;
            CompletedQuestIds = completedQuestIds ?? Array.Empty<string>();
            AchievementIds = achievementIds ?? Array.Empty<string>();
        }
    }
}
