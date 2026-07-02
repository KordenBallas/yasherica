using Combat.Core;
using Combat.Controller;
using Core.Logging;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Combat.Networking
{
    /// <summary>
    /// NetworkBehaviour for server-authoritative action processing.
    /// Clients send actions via ServerRpc, server validates and broadcasts state via ClientRpc.
    /// </summary>
    public class NetworkCombatStateSync : NetworkBehaviour
    {
        private ICombatController _gameController;
        private ActionSerializer _actionSerializer;
        [Inject] private IGameLogger _logger;
        
        /// <summary>
        /// Event fired when the server updates game state.
        /// </summary>
        public event System.Action<ICombatState> OnServerStateChanged;
        
        public void Initialize(ICombatController gameController)
        {
            _gameController = gameController;
            _actionSerializer = new ActionSerializer();
            
            if (IsServer)
            {
                // Subscribe to state changes on server
                _gameController.OnStateChanged += OnCombatStateChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (_gameController != null && IsServer)
            {
                _gameController.OnStateChanged -= OnCombatStateChanged;
            }
        }
        
        /// <summary>
        /// Server-side: Called when game state changes.
        /// Broadcasts new state to all clients.
        /// </summary>
        private void OnCombatStateChanged(ICombatState newState)
        {
            if (!IsServer)
                return;
            
            // In a real implementation, this would serialize the full game state
            // For now, we rely on deterministic action execution
            // Clients will maintain the same state if they execute the same actions
            
            OnServerStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// Client sends an action to the server for validation and execution.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SubmitActionServerRpc(ActionData actionData, ServerRpcParams rpcParams = default)
        {
            if (!IsServer)
                return;
            
            // Get the player associated with this client
            ulong clientId = rpcParams.Receive.SenderClientId;
            var player = GetPlayerForClient(clientId);
            
            if (player == null)
            {
                _logger?.Warning(LogCategory.Combat,$"No player found for client {clientId}");
                return;
            }
            
            // Verify the action is from the correct player
            if (player.Id != actionData.PlayerId)
            {
                _logger?.Warning(LogCategory.Combat,$"Player ID mismatch: {player.Id} vs {actionData.PlayerId}");
                return;
            }
            
            // Deserialize and execute action
            var action = _actionSerializer.Deserialize(actionData, player);
            var result = _gameController.ProcessAction(action);
            
            if (result.Success)
            {
                // Broadcast action to all clients for execution
                BroadcastActionClientRpc(actionData);
            }
            else
            {
                // Send error back to client
                SendActionErrorClientRpc(result.ErrorMessage, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }
        
        /// <summary>
        /// Server broadcasts an action to all clients for execution.
        /// </summary>
        [ClientRpc]
        private void BroadcastActionClientRpc(ActionData actionData)
        {
            if (IsServer)
                return; // Server already executed the action
            
            // Client executes the action locally
            var player = GetPlayerById(actionData.PlayerId);
            if (player == null)
            {
                _logger?.Warning(LogCategory.Combat,$"No player found with ID {actionData.PlayerId}");
                return;
            }
            
            var action = _actionSerializer.Deserialize(actionData, player);
            _gameController.ProcessAction(action);
        }
        
        /// <summary>
        /// Server sends an error message to a specific client.
        /// </summary>
        [ClientRpc]
        private void SendActionErrorClientRpc(string errorMessage, ClientRpcParams rpcParams = default)
        {
            _logger?.Warning(LogCategory.Combat,$"Action failed: {errorMessage}");
        }
        
        /// <summary>
        /// Server periodically syncs the full game state to clients.
        /// Used for late-joiners or desync recovery.
        /// </summary>
        [ClientRpc]
        public void SyncFullStateClientRpc(/* CombatStateData would go here */)
        {
            // In a full implementation, this would deserialize and apply the full game state
            // For now, we rely on deterministic execution
        }
        
        /// <summary>
        /// Gets the player associated with a client ID.
        /// This needs to be implemented based on your player management system.
        /// </summary>
        private IPlayer GetPlayerForClient(ulong clientId)
        {
            // Placeholder - implement based on your player tracking
            // Could use a dictionary mapping clientId to player
            if (_gameController.CombatState.Players.Count > (int)clientId)
            {
                return _gameController.CombatState.Players[(int)clientId];
            }
            return null;
        }
        
        /// <summary>
        /// Gets a player by their ID.
        /// </summary>
        private IPlayer GetPlayerById(int playerId)
        {
            foreach (var player in _gameController.CombatState.Players)
            {
                if (player.Id == playerId)
                    return player;
            }
            return null;
        }
    }
}

