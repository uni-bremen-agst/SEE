using UnityEngine;
using TMPro;
using Unity.Netcode;
using SEE.GO;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Sends and shows the player name for each player.
    /// </summary>
    /// <remarks>This behaviour is attached to all player prefabs.
    /// A player prefab will be instantiated for remote players and
    /// local players. That is, for instance, if there are three clients,
    /// we have altogether nine player instances, three for each client,
    /// where one of the three is the local player and the other two are
    /// remote players. Whether a player is local or remote is determined
    /// by the <see cref="NetworkObject.IsLocalPlayer"/> property (if true,
    /// it is the local player).
    /// </remarks>
    /// <remarks>Players are identified here by <see cref="NetworkBehaviour.NetworkObjectId"/>s,
    /// not be confused with client ids.</remarks>
    public class PlayerName : NetworkBehaviour
    {
        /// <summary>
        /// Reference to the TextMeshPro component to display the player's name.
        /// </summary>
        [SerializeField]
        private TMP_Text displayNameText;

        /// <summary>
        /// Sets the name of this player.
        /// If this is executed by the local player, the player's name is retrieved from the config.
        /// If this is executed by a remote player, the player name is requested from the server.
        /// </summary>
        /// <remarks>
        /// Note: This code must be executed in Start(); it would not work within OnNetworkSpawn().
        /// OnNetworkSpawn() is invoked on each NetworkBehaviour associated with a NetworkObject
        /// when it's spawned. For dynamically spawned objects such as this one, Start() is called
        /// *after* OnNetworkSpawn(). During OnNetworkSpawn(), the server has not yet put the player's
        /// name into <see cref="PlayerNameMap"/>. That is why, we need to wait a little longer;
        /// hence, we use Start().
        /// </remarks>
        private void Start()
        {
            Log($"{nameof(Start)}(IsLocalPlayer={IsLocalPlayer})\n");
            // If this player is the local player, its name was already sent to the server during spawning.
            // Only in the case of a remote player, we need to request the player name from the server.
            if (IsLocalPlayer)
            {
                Log($"{nameof(Start)}() uses GetLocalPlayerName()\n");
                SetPlayerName(User.UserSettings.Instance.Player.PlayerName);
            }
            else
            {
                Log($"{nameof(Start)}() uses {nameof(RequestPlayerNameFromServerRpc)}(NetworkObjectId={NetworkObjectId})\n");
                RequestPlayerNameFromServerRpc(NetworkObjectId);
            }
        }

        /// <summary>
        /// Called by Unity Netcode when the network object is despawned. Removes the player's name
        /// from the server.
        /// </summary>
        /// <remarks><see cref="NetworkBehaviour.OnNetworkDespawn"/> is invoked on each
        /// <see cref=">NetworkBehaviour"/> associated with a <see cref="NetworkObject"/>
        /// when it's despawned. This is where all netcode cleanup code should occur,
        /// but isn't to be confused with destroying.</remarks>
        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                // It is sufficent to remove the name from the server only
                // once for all instances of the player on all clients.
                // We let the local player do this.
                RemovePlayerNameFromServerRpc(NetworkObjectId);
            }
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// RPC method to remove a player's name from the server.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player whose
        /// player name should be deregistered</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void RemovePlayerNameFromServerRpc(ulong networkObjectId)
        {
            Log($"{nameof(RemovePlayerNameFromServerRpc)}(networkObjectId={networkObjectId})\n");
            PlayerNameMap.RemovePlayerName(networkObjectId);
        }

        /// <summary>
        /// Called by a client to request its player name from the server. The server response is
        /// not immediate, but will be sent by the server to the requesting client via
        /// <see cref="SendPlayerNameToClientRpc"/>.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player requesting its
        /// player name</param>
        /// <param name="rpcParams">additional RPC parameters managed by NetCode; used to retrieve
        /// the sender client id</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void RequestPlayerNameFromServerRpc(ulong networkObjectId, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            string playerName = PlayerNameMap.GetPlayerName(networkObjectId);
            Log($"{nameof(RequestPlayerNameFromServerRpc)}(networkObjectId={networkObjectId}, senderClientId={senderClientId}) => playerName={playerName}\n");
            SendPlayerNameToClientRpc(networkObjectId, playerName, RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Called by the server to send the player's name to the specific client requesting it
        /// (<seealso cref="RequestPlayerNameFromServerRpc"/>) but only if its <see cref="NetworkObjectId"/>
        /// matches the given <paramref name="networkObjectId"/>.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player whose player name
        /// was requested</param>
        /// <param name="playerName">New player name for <paramref name="networkObjectId"/></param>
        /// <remarks>This method is called by the server, but executed on the client which
        /// called <see cref="RequestPlayerNameFromServerRpc"/>.</remarks>
        [Rpc(SendTo.SpecifiedInParams)]
        void SendPlayerNameToClientRpc(ulong networkObjectId, string playerName, RpcParams _)
        {
            Log($"{nameof(SendPlayerNameToClientRpc)}(networkObjectId={networkObjectId}, playername={playerName})\n");
            if (NetworkObjectId == networkObjectId)
            {
                SetPlayerName(playerName);
            }
        }

        /// <summary>
        /// Renders the player's name on the TextMeshPro component <see cref="displayNameText"/>
        /// (and also renames the top-most game object this component is attached to accordingly).
        /// </summary>
        /// <param name="playerName">Player name that should be set</param>
        private void SetPlayerName(string playerName)
        {
            Log($"{nameof(SetPlayerName)}(playername={playerName})\n");

            displayNameText.text = playerName;
            gameObject.transform.root.name = playerName;
        }

        /// <summary>
        /// Renders the player's name on the TextMeshPro component <see cref="displayNameText"/>
        /// of the child game object of <paramref name="player"/> representing the player's name
        /// (and also renames the top-most game object this component is attached to accordingly).
        /// </summary>
        /// <param name="player">game object representing the player and having a child
        /// for rendering the player's name</param>
        /// <param name="nameOfPlayer">Player name that should be set</param>
        internal static void SetPlayerName(GameObject player, string nameOfPlayer)
        {
            Log($"{nameof(SetPlayerName)}(player={player.FullName()}, nameOfPlayer={nameOfPlayer})\n");

            // Name of the immediate child of player representing the player's name.
            // See the prefabs for players for the structure.
            const string playerNameGameObjectName = "Playername";

            GameObject child = player.transform.Find(playerNameGameObjectName)?.gameObject;

            if (child == null)
            {
                throw new($"Player game object {player.FullName()} does not have a child named {playerNameGameObjectName}.");
            }
            if (child.TryGetComponentOrLog(out PlayerName playerName))
            {
                playerName.SetPlayerName(nameOfPlayer);
            }
        }

        /// <summary>
        /// Logs given <paramref name="message"/> to the console.
        /// </summary>
        /// <param name="message">message to be logged</param>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string message)
        {
            Debug.Log($"[{nameof(PlayerName)}] {message}\n");
        }
    }
}
