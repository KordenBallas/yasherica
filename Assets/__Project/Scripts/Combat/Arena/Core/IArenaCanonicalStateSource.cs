using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Where the X2 commit validation reads the canonical truth: the host's own sim at the
    /// current round's planning start (the same anchor X1 snapshots from). Implemented by the
    /// arena controller over the flow's retained round-start state.
    /// </summary>
    public interface IArenaCanonicalStateSource
    {
        ICombatState RoundStartState { get; }
    }
}
