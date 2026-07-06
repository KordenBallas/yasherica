using Combat.Battlefield;

namespace Combat.Player
{
    /// <summary>
    /// One enemy's committed-intent telegraph for the board (D3): whether it is <see cref="IsArmed"/>
    /// (holds a committed intent this round → the "ready" body pose + restless icons) and, if it
    /// committed a move, the from/to cells for the direction arrow. Pure domain data — the view maps
    /// the cells to world space. Only armed enemies are emitted; a cleared enemy simply drops out.
    /// </summary>
    public readonly struct EnemyIntentTelegraphModel
    {
        public int UnitId { get; }
        public bool IsArmed { get; }
        public bool HasMove { get; }
        public HexCoordinates MoveFrom { get; }
        public HexCoordinates MoveTo { get; }

        public EnemyIntentTelegraphModel(int unitId, bool isArmed, bool hasMove,
            HexCoordinates moveFrom, HexCoordinates moveTo)
        {
            UnitId = unitId;
            IsArmed = isArmed;
            HasMove = hasMove;
            MoveFrom = moveFrom;
            MoveTo = moveTo;
        }
    }
}
