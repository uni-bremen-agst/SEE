using Dissonance;
using SEE.Game.Avatars;
using SEE.GO;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    /// <summary>
    /// Spawns players in multi-player mode.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        /// <summary>
        /// Information needed to spawn a player avatar.
        /// </summary>
        [Serializable]
        private class SpawnInfo
        {
            /// <summary>
            /// Avatar game object used as prefab.
            /// </summary>
            [Tooltip("Avatar game object used as prefab.")]
            public GameObject PlayerPrefab;

            /// <summary>
            /// World-space position at which to spawn a player.
            /// </summary>
            [Tooltip("World-space position at which to spawn the avatar.")]
            public Vector3 Position;

            /// <summary>
            /// Rotation of the avatar in degrees along the y axis when the avatar is spawned.
            /// </summary>
            [Tooltip("Rotation in degrees along the y axis.")]
            public float Rotation;

            /// <summary>
            /// The folder where the player prefabs are located.
            /// </summary>
            private const string playerPrefabFolder = "Prefabs/Players/CC4";

            /// <summary>
            /// Constructor to create a spawn-information object.
            /// </summary>
            /// <param name="prefabName">Name of the prefab file for the avatar; must be located in <see cref="playerPrefabFolder"/>.</param>
            /// <param name="position">World-space position at which to spawn a player.</param>
            /// <param name="rotation">Rotation of the avatar in degrees along the y axis when the avatar is spawned.</param>
            /// <exception cref="Exception">Thrown in case the <paramref name="prefabName"/> cannot be loaded.</exception>
            public SpawnInfo(string prefabName, Vector3 position, int rotation)
            {
                PlayerPrefab = Resources.Load<GameObject>(Path(prefabName));
                if (PlayerPrefab == null)
                {
                    throw new Exception($"Player prefab {Path(prefabName)} not found.\n");
                }

                Position = position;
                Rotation = rotation;

                return;

                static string Path(string prefabName) => $"{playerPrefabFolder}/{prefabName}";
            }
        }

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// </summary>
        private DissonanceComms dissonanceComms = null;

        /// <summary>
        /// The information needed to spawn player avatars.
        /// </summary>
        /// <remarks>This field must not be readonly. It will be changed by Odin during serialization.</remarks>
        [Tooltip("The information to be used to spawn players."), ShowInInspector, SerializeField]
        private List<SpawnInfo> playerSpawns;

        /// <summary>
        /// The name of the player prefabs used for spawning. These prefabs must be located in the
        /// <see cref="SpawnInfo.playerPrefabFolder"/>.
        /// </summary>
        /// <remarks>Order by male and female and then by name.</remarks>
        public static List<string> Prefabs = new()
        {
            // Males
           "Caleb",
           "Carlos",
           "Dwayne",
           "Eddy",
           "John",
           "Karl",
           "Kevin",
           "Tao",
           "Yvo",
           // Females
           "Hanna",
           "Luise",
           "Paula",
           "Petra",
           "Shi",
           "Susan",
        };

        /// <summary>
        /// Initializes the player spawns if they are not already set by the user in the Unity inspector.
        /// </summary>
        private void Awake()
        {
            if (playerSpawns == null || playerSpawns.Count == 0)
            {
                // TODO (#832): This should be computed.
                playerSpawns = new()
                {
                    new SpawnInfo(Prefabs[0], new Vector3(0.4f, 0f, -5.8f), 270),
                    new SpawnInfo(Prefabs[1], new Vector3(0.4f, 0f, -6.6f), 270),
                    new SpawnInfo(Prefabs[2], new Vector3(0.4f, 0f, -7.8f), 270),
                    new SpawnInfo(Prefabs[3], new Vector3(-3.5f, 0f, -5.8f), 90),
                    new SpawnInfo(Prefabs[4], new Vector3(-3.5f, 0f, -6.8f), 90),
                    new SpawnInfo(Prefabs[5], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[6], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[7], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[8], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[9], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[10], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[11], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[12], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[13], new Vector3(-3.5f, 0f, -7.8f), 90),
                    new SpawnInfo(Prefabs[13], new Vector3(-3.5f, 0f, -7.8f), 90),
                };
            }
        }

        /// <summary>
        /// Runs <see cref="SpawnPlayer"/>.
        /// </summary>
        private void OnEnable()
        {
            SpawnPlayer();
        }

        /// <summary>
        /// RPC method to spawn a player on the server.
        /// </summary>
        /// <param name="clientId">The network ID of the client who is requesting to spawn a local player.</param>
        /// <param name="avatarIndex">The index of the avatar to spawn.</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void SpawnOnServerRpc(ulong clientId, string playerName, uint avatarIndex)
        {
            Spawn(clientId, playerName, avatarIndex);
        }

        /// <summary>
        /// This method sets <see cref="dissonanceComms"/>, registers <see cref="ClientConnects(ulong)"/>
        /// on the <see cref="NetworkManager.Singleton.OnClientConnectedCallback"/> and spawns
        /// the first local client.
        /// </summary>
        private void SpawnPlayer()
        {
            Net.Network networkConfig = User.UserSettings.Instance.Network
                ?? throw new Exception("Network configuration not found.\n");

            // Wait until Dissonance is created.
            if (ReferenceEquals(dissonanceComms, null))
            {
                dissonanceComms = FindFirstObjectByType<DissonanceComms>();
                // We need to set the local player name in DissonanceComms
                // before Dissonance is started. That is why we cannot afford
                // to wait until the next frame.
                dissonanceComms.LocalPlayerName = User.UserSettings.Instance.Player.PlayerName;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            // Terminate this co-routine if not run by the server (or host).
            if (!networkManager.IsServer)
            {
                // Note: When we arrive here, a connection is established only
                // from a "pure" client -- that is, one that is neither server
                // nor host. A pure client is to request spawning a player
                // via the following call back.
                networkManager.OnClientConnectedCallback += OnClientIsConnected;
                return;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // The following code will be executed only on the server (host).
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // The callback to invoke once a client connects. This callback is only
            // run on the server. The following callbacks are just informative.
            // They do not spawn a player.
            networkManager.OnClientConnectedCallback += ClientConnects;
            networkManager.OnClientDisconnectCallback += ClientDisconnects;
            // Spawn the local player. This code is executed by a host only.
            // "Pure" servers (i.e., those that are not hosts) must not spawn a player.
            if (networkManager.IsHost)
            {
                // Spawn the local player for this host.
                Spawn(networkManager.LocalClientId, User.UserSettings.Instance.Player.PlayerName, User.UserSettings.Instance.Player.AvatarIndex);
            }
        }

        /// <summary>
        /// Logs given <paramref name="message"/> to the console.
        /// </summary>
        /// <param name="message">Message to be logged.</param>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string message)
        {
            Debug.Log($"[{nameof(PlayerSpawner)}] {message}\n");
        }

        /// <summary>
        /// Reports that a client with given <paramref name="clientId"/> has connected.
        /// </summary>
        /// <param name="clientId">The network ID of the connecting client.</param>
        /// <remarks>This code is executed only on the server.</remarks>
        /// <remarks>Do not confuse client IDs with <see cref="NetworkBehaviour.NetworkObjectId"/>.</remarks>
        private void ClientConnects(ulong clientId)
        {
            Log($"Player with client id {clientId} connects with server (server side).\n");
        }

        /// <summary>
        /// Reports that a client with given <paramref name="clientId"/> has disconnected.
        /// </summary>
        /// <param name="clientId">The network ID of the disconnecting client.</param>
        /// <remarks>This code is executed only on the server.</remarks>
        /// <remarks>Do not confuse client IDs with <see cref="NetworkBehaviour.NetworkObjectId"/>.</remarks>
        private static void ClientDisconnects(ulong clientId)
        {
            Log($"Player with ID {clientId} disconnects.\n");
        }

        /// <summary>
        /// Requests to spawn a player on the server. Is used as callback registered
        /// at <see cref="SpawnPlayer"/>. The player name and avatar index
        /// are retrieved from the local configuration on the client side.
        /// </summary>
        /// <param name="clientId">The network ID of the connecting client.</param>
        /// <remarks>This code is executed on all connecting "pure" clients, i.e., on
        /// a client that is not also a server (i.e., a host).</remarks>
        private void OnClientIsConnected(ulong clientId)
        {
            Log($"Player with client {clientId} is connected with server (client side).\n");
            SpawnOnServerRpc(clientId, User.UserSettings.Instance.Player.PlayerName, User.UserSettings.Instance.Player.AvatarIndex);
        }

        /// <summary>
        /// Spawns a player for the client with given <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">The network ID of the client for which to spawn a player.</param>
        /// <param name="nameOfPlayer">The name of the player to be spawn.</param>
        /// <param name="avatarIndex">The index of the avatar to be spawned relative to <see cref="playerSpawns"/>.</param>
        /// <remarks>This code is executed on a server. Only servers are allowed to spawn players.</remarks>
        private void Spawn(ulong clientId, string nameOfPlayer, uint avatarIndex)
        {
            Log($"Player with client {clientId} named {nameOfPlayer} requests to spawn an avatar with index {avatarIndex}.\n");

            // Make sure the index is within the bounds of the playerSpawns list.
            int index = (int)avatarIndex % playerSpawns.Count;
            GameObject player = Instantiate(playerSpawns[index].PlayerPrefab,
                                            playerSpawns[index].Position,
                                            Quaternion.Euler(new Vector3(0, playerSpawns[index].Rotation, 0)));
            player.name = nameOfPlayer;

            Log($"Spawned {player.name} for client {clientId}.\n");

            if (player.TryGetComponentOrLog(out NetworkObject net))
            {
                // By default a newly spawned network Prefab instance is owned by the server
                // unless otherwise specified. However, in case of SpawnAsPlayerObject, if
                // the player already had a prefab instance assigned, then the client owns
                // the NetworkObject of that prefab instance unless there's additional
                // server-side specific user code that removes or changes the ownership.
                // Note: The following method can only be called by a server.
                net.SpawnAsPlayerObject(clientId, destroyWithScene: true);

                PlayerNameMap.AddOrUpdatePlayerName(net.NetworkObjectId, nameOfPlayer);

                // A network Prefab is any unity Prefab asset that has one NetworkObject
                // component attached to a GameObject within the prefab.
                // player is a network Prefab, i.e., it has a NetworkObject attached to it.
                // More commonly, the NetworkObject component is attached to the root GameObject
                // of the Prefab asset because this allows any child GameObject to have
                // NetworkBehaviour components automatically assigned to the NetworkObject.
                // The reason for this is that a NetworkObject component attached to a
                // GameObject will be assigned (associated with) any NetworkBehaviour components on:
                //
                // (1) the same GameObject that the NetworkObject component is attached to
                // (2) any child or children of the GameObject that the NetworkObject is attached to.
                //
                // A caveat of the above two rules is when one of the children GameObjects also
                // has a NetworkObject component assigned to it (a.k.a. "Nested NetworkObjects").
                // Nested NetworkObject components aren't permitted in network prefabs.
            }
            // Sets the name of the player on the server side. Instances of the player prefab
            // instantiated on other clients will request their name when they are spawned
            // (see PlayerName.Start()).
            PlayerName.SetPlayerName(player, nameOfPlayer);
        }
    }
}
