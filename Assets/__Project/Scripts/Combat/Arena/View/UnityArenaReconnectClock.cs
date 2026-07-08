using Combat.Arena.Core;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>Production reconnect clock: unscaled time — a hitch must not eat the retry window.</summary>
    public class UnityArenaReconnectClock : IArenaReconnectClock
    {
        public float Now => Time.unscaledTime;
    }
}
