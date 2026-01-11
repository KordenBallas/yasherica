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
        
        public IPlayer GetLocalPlayer()
        {
            if (_localPlayer == null)
            {
                Debug.LogWarning("[PlayerRegistry] No local player registered");
            }
            return _localPlayer;
        }
        
        public void RegisterLocalPlayer(IPlayer player)
        {
            if (player == null)
            {
                Debug.LogWarning("[PlayerRegistry] Attempted to register null player");
                return;
            }
            
            if (_localPlayer != null)
            {
                Debug.LogWarning($"[PlayerRegistry] Replacing existing player {_localPlayer.Name} with {player.Name}");
            }
            
            _localPlayer = player;
            Debug.Log($"[PlayerRegistry] Registered local player: {player.Name} (ID: {player.Id})");
        }
        
        public void UnregisterLocalPlayer()
        {
            if (_localPlayer != null)
            {
                Debug.Log($"[PlayerRegistry] Unregistered local player: {_localPlayer.Name}");
                _localPlayer = null;
            }
        }
    }
}

