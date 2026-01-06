using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// Human player implementation (pure C#).
    /// </summary>
    public class HumanPlayer : IPlayer
    {
        public int Id { get; }
        public PlayerType Type => PlayerType.Human;
        public string Name { get; }
        
        public HumanPlayer(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}

