using System;

namespace Core.Persistence
{
    /// <summary>
    /// The Area → Hub arrival marker (O1): why the player landed on the Hub. Today the only flag is
    /// the death return (the cauldron greets a reformed corpse differently from a fresh visit).
    /// </summary>
    [Serializable]
    public class HubArrivalSnapshot : IVersionedSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public bool DeathReturn;

        int IVersionedSnapshot.Version => Version;
    }
}
