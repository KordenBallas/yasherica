using Combat.Core;
using Combat.Controller;
using Core.Logging;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Combat.Networking
{
    /// <summary>
    /// Client-side controller for sending actions to the server.
    /// </summary>
    public class NetworkActionSender : NetworkBehaviour
    {
        private NetworkCombatStateSync _networkSync;
        private ActionSerializer _actionSerializer;
        private IPlayer _localPlayer;
        [Inject] private IGameLogger _logger;
        
        public void Initialize(NetworkCombatStateSync networkSync, IPlayer localPlayer)
        {
            _networkSync = networkSync;
            _localPlayer = localPlayer;
            _actionSerializer = new ActionSerializer();
        }
        
        /// <summary>
        /// Sends an action to the server for validation and execution.
        /// </summary>
        public void SendAction(IAction action)
        {
            if (!IsClient)
            {
                _logger?.Warning(LogCategory.Combat,"Cannot send action: not a client");
                return;
            }
            
            // Verify this is the local player's action
            if (action.Player.Id != _localPlayer.Id)
            {
                _logger?.Warning(LogCategory.Combat,"Cannot send action for another player");
                return;
            }
            
            // Serialize and send to server
            var actionData = _actionSerializer.Serialize(action);
            _networkSync.SubmitActionServerRpc(actionData);
        }
    }
}

