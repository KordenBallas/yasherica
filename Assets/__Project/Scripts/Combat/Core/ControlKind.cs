namespace Combat.Core
{
    /// <summary>
    /// What a Control-type status restricts on the afflicted unit.
    /// </summary>
    public enum ControlKind
    {
        /// <summary>The unit loses its turn entirely (cannot move or act).</summary>
        Stun = 0,

        /// <summary>The unit cannot move but may still act (queue and fire abilities).</summary>
        Root = 1,

        /// <summary>The unit's movement range is reduced by the status's movement penalty.</summary>
        Slow = 2
    }
}
