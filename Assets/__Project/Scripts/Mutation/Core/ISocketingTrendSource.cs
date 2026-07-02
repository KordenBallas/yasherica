using System;

namespace Mutation.Core
{
    /// <summary>
    /// Seam for the cauldron-voice hint channel: raised as socketing changes a
    /// blank's combined trait direction. No consumer ships yet - the narrative
    /// bark delivery is a separate ROADMAP item that subscribes here.
    /// </summary>
    public interface ISocketingTrendSource
    {
        event Action<SocketingTrend> OnTrendChanged;
    }
}
