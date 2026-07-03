using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// FFA win rule (brief R14): exactly one player with a living unit wins; zero → draw
    /// (reported as met with a null winner). Armed only after the full roster has spawned —
    /// heroes are added one by one, and the first spawned hero must not "win" an empty board.
    /// </summary>
    public class LastHeroStandingWinCondition : IWinCondition
    {
        private bool _armed;

        public WinConditionType Type => WinConditionType.LastHeroStanding;

        /// <summary>Call once every roster slot's unit has been added to the combat state.</summary>
        public void Arm()
        {
            _armed = true;
        }

        public bool Check(ICombatState gameState, out IPlayer winningPlayer)
        {
            winningPlayer = null;
            if (!_armed)
                return false;

            var playersWithAliveUnits = gameState.Players
                .Where(p => gameState.GetUnitsByPlayer(p).Any(u => u.IsAlive))
                .ToList();

            if (playersWithAliveUnits.Count == 1)
            {
                winningPlayer = playersWithAliveUnits[0];
                return true;
            }

            // Simultaneous last-two-die: the match is over with no winner (draw).
            return playersWithAliveUnits.Count == 0;
        }
    }
}
