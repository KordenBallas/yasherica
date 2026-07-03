using Combat.Battlefield;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// One resolved seat of the match: who (player), which unit id, where it spawns, and whether
    /// this machine's input drives it. Built deterministically from the match seed + roster on
    /// every client (spawn cells never travel on the wire).
    /// </summary>
    public class ArenaSpawnSlot
    {
        public IPlayer Owner { get; }
        public int UnitId { get; }
        public HexCoordinates SpawnCell { get; }
        public bool IsLocal { get; }

        public ArenaSpawnSlot(IPlayer owner, int unitId, HexCoordinates spawnCell, bool isLocal)
        {
            Owner = owner;
            UnitId = unitId;
            SpawnCell = spawnCell;
            IsLocal = isLocal;
        }
    }
}
