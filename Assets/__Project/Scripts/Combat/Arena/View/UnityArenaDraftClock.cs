using Combat.Arena.Core;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>Production draft clock: unscaled time, so pause tricks cannot stall the deadline.</summary>
    public class UnityArenaDraftClock : IArenaDraftClock
    {
        public float Now => Time.unscaledTime;
    }
}
