using Combat.Controller;
using Combat.Core;
using Unity.Netcode;
using UnityEngine;

namespace Combat.Networking
{
    /// <summary>
    /// Main network manager for combat.
    /// Sets up server/client and manages network game flow.
    /// </summary>
    public class CombatNetworkManager : MonoBehaviour
    {
        [SerializeField] private NetworkCombatStateSync _networkSync;
        [SerializeField] private NetworkActionSender _actionSender;
        
        private ICombatController _gameController;
        private NetworkPlayer _localPlayer;
        
        /// <summary>
        /// Initializes the network manager as a server.
        /// </summary>
        public void InitializeAsServer(ICombatController gameController)
        {
            _gameController = gameController;
            
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("Cannot initialize as server: not running as server");
                return;
            }
            
            _networkSync.Initialize(_gameController);
            
            Debug.Log("Combat network manager initialized as SERVER");
        }
        
        /// <summary>
        /// Initializes the network manager as a client.
        /// </summary>
        public void InitializeAsClient(ICombatController gameController, NetworkPlayer localPlayer)
        {
            _gameController = gameController;
            _localPlayer = localPlayer;
            
            if (!NetworkManager.Singleton.IsClient)
            {
                Debug.LogError("Cannot initialize as client: not running as client");
                return;
            }
            
            _networkSync.Initialize(_gameController);
            _actionSender.Initialize(_networkSync, _localPlayer);
            
            Debug.Log($"Combat network manager initialized as CLIENT (Player: {_localPlayer.Name})");
        }
        
        /// <summary>
        /// Sends an action from the local client to the server.
        /// </summary>
        public void SendLocalAction(IAction action)
        {
            if (_actionSender == null)
            {
                Debug.LogError("Cannot send action: action sender not initialized");
                return;
            }
            
            _actionSender.SendAction(action);
        }
        
        /// <summary>
        /// Starts the game as a host (server + client).
        /// </summary>
        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started as HOST");
        }
        
        /// <summary>
        /// Starts the game as a dedicated server.
        /// </summary>
        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("Started as SERVER");
        }
        
        /// <summary>
        /// Connects to a server as a client.
        /// </summary>
        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started as CLIENT");
        }
        
        /// <summary>
        /// Disconnects from the network.
        /// </summary>
        public void Shutdown()
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Network shutdown");
        }
    }
}

