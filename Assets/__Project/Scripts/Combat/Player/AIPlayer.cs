using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// AI player implementation (pure C#).
    /// </summary>
    public class AIPlayer : IPlayer
    {
        public int Id { get; }
        public PlayerType Type => PlayerType.AI;
        public string Name { get; }
        
        private readonly IAIDecisionMaker _decisionMaker;
        
        public AIPlayer(int id, string name, IAIDecisionMaker decisionMaker)
        {
            Id = id;
            Name = name;
            _decisionMaker = decisionMaker;
        }
        
        /// <summary>
        /// Requests an action for a specific unit from the AI.
        /// </summary>
        public IAction RequestAction(ICombatState gameState, IUnit unit)
        {
            return _decisionMaker.DecideAction(gameState, unit);
        }
    }
}

