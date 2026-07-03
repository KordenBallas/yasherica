namespace Combat.Core
{
    /// <summary>
    /// An ability that displaces the units it strikes. Push is away from the caster along
    /// the line direction; the ghost telegraph reads this to show resulting positions.
    /// </summary>
    public interface IDisplacementAbility
    {
        /// <summary>
        /// Cells a struck unit is pushed; 0 = no displacement.
        /// </summary>
        int PushDistance { get; }
    }
}
