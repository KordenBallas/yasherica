using System;
using System.Collections.Generic;
using LevelGeneration;

namespace Loot.Core
{
    /// <summary>
    /// Inputs of one loot roll. ContextKey must be stable across runs for the same
    /// game situation (e.g. "quest:{storyId}:{npcId}") — it anchors determinism.
    /// </summary>
    public class LootRollContext
    {
        public LevelTheme Theme { get; }
        public string ContextKey { get; }
        public IReadOnlyList<string> Tags { get; }
        public PlayerLootState Player { get; }

        public LootRollContext(
            LevelTheme theme,
            string contextKey,
            IReadOnlyList<string> tags = null,
            PlayerLootState player = null)
        {
            if (string.IsNullOrEmpty(contextKey))
            {
                throw new ArgumentException("Context key must not be null or empty.", nameof(contextKey));
            }

            Theme = theme;
            ContextKey = contextKey;
            Tags = tags ?? Array.Empty<string>();
            Player = player ?? PlayerLootState.Empty;
        }
    }
}
