using Core.Logging;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// Singleton service for managing player references.
    /// Managed by Zenject, not a static singleton.
    /// </summary>
    public class PlayerRegistry : IPlayerRegistry
    {
        private IPlayer _localPlayer;
        private readonly IGameLogger _logger;

        public PlayerRegistry(IGameLogger logger)
        {
            _logger = logger;
        }

        public IPlayer GetLocalPlayer()
        {
            if (_localPlayer == null)
            {
                _logger.Warning(LogCategory.Combat,"[PlayerRegistry] No local player registered");
            }
            return _localPlayer;
        }
        
        public void RegisterLocalPlayer(IPlayer player)
        {
            if (player == null)
            {
                _logger.Warning(LogCategory.Combat,"[PlayerRegistry] Attempted to register null player");
                return;
            }
            
            if (_localPlayer != null)
            {
                _logger.Warning(LogCategory.Combat,$"[PlayerRegistry] Replacing existing player {_localPlayer.Name} with {player.Name}");
            }
            
            _localPlayer = player;
            _logger.Info(LogCategory.Combat,$"[PlayerRegistry] Registered local player: {player.Name} (ID: {player.Id})");
        }
        
        public void UnregisterLocalPlayer()
        {
            if (_localPlayer != null)
            {
                _logger.Info(LogCategory.Combat,$"[PlayerRegistry] Unregistered local player: {_localPlayer.Name}");
                _localPlayer = null;
            }
        }
    }
}

