using System;

namespace GameInput.Core
{
    /// <summary>
    /// The source the player last actuated (Input Foundation R5). The infrastructure tracker updates it
    /// from device events; presenters and the prompt layer only read this interface, so they stay pure
    /// and testable. <see cref="Changed"/> fires only on an actual switch, never on repeat actuation of
    /// the same source.
    /// </summary>
    public interface IActiveInputSource
    {
        InputSource Current { get; }

        event Action<InputSource> Changed;
    }
}
