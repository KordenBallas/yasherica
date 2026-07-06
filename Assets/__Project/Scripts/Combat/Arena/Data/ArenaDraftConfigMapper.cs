using System.Collections.Generic;
using Combat.Arena.Core;
using Core.Logging;

namespace Combat.Arena.Data
{
    /// <summary>
    /// The one bridge from the ArenaDraftConfig SO into Core draft settings (install time).
    /// Drops misauthored rows loudly: frame-changing floor parts (the first draft stays on the
    /// base body-plan), parts whose slot is missing or outside the loadout, null entries. Also
    /// warns when the floor cannot cover every seat (P4-5 req 6 viability).
    /// </summary>
    public static class ArenaDraftConfigMapper
    {
        public static ArenaDraftSettings ToSettings(ArenaDraftConfig config, int maxPlayers, IGameLogger logger)
        {
            var slotLoadout = new List<string>();
            foreach (var slot in config.SlotLoadout)
            {
                if (slot == null || string.IsNullOrEmpty(slot.Id))
                {
                    logger.Warning(LogCategory.Combat,
                        "[ArenaDraftConfigMapper] Null/id-less slot in the loadout — dropped.");
                    continue;
                }

                slotLoadout.Add(slot.Id);
            }

            var floorParts = new List<ArenaDraftPartInfo>();
            var floorCountBySlot = new Dictionary<string, int>();
            foreach (var part in config.FloorParts)
            {
                if (part == null || part.Slot == null)
                {
                    logger.Warning(LogCategory.Combat,
                        "[ArenaDraftConfigMapper] Null/slot-less floor part — dropped.");
                    continue;
                }

                if (part.GovernsBodyPlan)
                {
                    logger.Warning(LogCategory.Combat,
                        $"[ArenaDraftConfigMapper] Floor part '{part.Id}' governs a body plan — " +
                        "frame-changers are not draftable (base body-plan only); dropped.");
                    continue;
                }

                if (!slotLoadout.Contains(part.Slot.Id))
                {
                    logger.Warning(LogCategory.Combat,
                        $"[ArenaDraftConfigMapper] Floor part '{part.Id}' targets slot " +
                        $"'{part.Slot.Id}' outside the loadout — dropped.");
                    continue;
                }

                floorParts.Add(new ArenaDraftPartInfo(part.Id, part.Slot.Id));
                floorCountBySlot.TryGetValue(part.Slot.Id, out var count);
                floorCountBySlot[part.Slot.Id] = count + 1;
            }

            foreach (var slotId in slotLoadout)
            {
                floorCountBySlot.TryGetValue(slotId, out var count);
                if (count < maxPlayers)
                {
                    logger.Warning(LogCategory.Combat,
                        $"[ArenaDraftConfigMapper] Floor stocks only {count} '{slotId}' parts for " +
                        $"up to {maxPlayers} players — a full lobby could strand a seat (P4-5 req 6).");
                }
            }

            return new ArenaDraftSettings(
                slotLoadout,
                floorParts,
                config.CatalogSampleSize,
                config.PickTimerSeconds,
                config.AiPickDelaySeconds);
        }
    }
}
