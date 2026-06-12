using System;
using System.Collections.Generic;
using Loot.Core;

namespace Platform
{
    /// <summary>
    /// Discoverable artifacts placed on a platform during generation.
    /// Items are rolled once by the loot service; collected flags prevent
    /// respawning when the platform is re-entered.
    /// </summary>
    public class LootContent : PlatformContentBase
    {
        private readonly IReadOnlyList<LootRollResult> _items;
        private readonly bool[] _collected;

        public override ContentType Type => ContentType.Loot;

        public IReadOnlyList<LootRollResult> Items => _items;

        public LootContent(IReadOnlyList<LootRollResult> items)
        {
            _items = items ?? Array.Empty<LootRollResult>();
            _collected = new bool[_items.Count];
        }

        public bool IsCollected(int itemIndex)
        {
            return _collected[itemIndex];
        }

        public void MarkCollected(int itemIndex)
        {
            _collected[itemIndex] = true;
        }

        public override void Initialize(IPlatform platform)
        {
        }

        public override void OnPlatformEntered(IPlatform platform)
        {
        }
    }
}
