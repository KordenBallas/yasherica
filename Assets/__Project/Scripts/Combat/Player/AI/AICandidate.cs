using Combat.Battlefield;
using Combat.Config;
using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// One evaluable option for a unit's round: the committable action plus the data the
    /// scorer needs to simulate it (ability + facing, or move destination).
    /// </summary>
    public sealed class AICandidate
    {
        private AICandidate(
            IAction action,
            AICandidateKind kind,
            IAbility ability,
            HexDirection? facing,
            HexCoordinates moveDestination)
        {
            Action = action;
            Kind = kind;
            Ability = ability;
            Facing = facing;
            MoveDestination = moveDestination;
        }

        public IAction Action { get; }
        public AICandidateKind Kind { get; }

        /// <summary>The ability to simulate; null unless Kind is Ability.</summary>
        public IAbility Ability { get; }

        /// <summary>Line facing; null for ring abilities and non-ability candidates.</summary>
        public HexDirection? Facing { get; }

        /// <summary>Destination cell; only meaningful when Kind is Move.</summary>
        public HexCoordinates MoveDestination { get; }

        public static AICandidate ForAbility(IAction action, IAbility ability, HexDirection? facing)
        {
            return new AICandidate(action, AICandidateKind.Ability, ability, facing, default);
        }

        public static AICandidate ForMove(IAction action, HexCoordinates destination)
        {
            return new AICandidate(action, AICandidateKind.Move, null, null, destination);
        }

        public static AICandidate ForEndTurn(IAction action)
        {
            return new AICandidate(action, AICandidateKind.EndTurn, null, null, default);
        }
    }
}
