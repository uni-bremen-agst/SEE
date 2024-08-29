using Dissonance;
using SEE.Game.Avatars;
using SEE.GO;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    /// <summary>
    /// Information needed to spawn a player avatar.
    /// </summary>
    [Serializable]
    class SpawnInfo
    {
        [Tooltip("Avatar game object used as prefab.")]
        public GameObject PlayerPrefab;

        [Tooltip("World-space position at which to spawn.")]
        public Vector3 Position;

        [Tooltip("Rotation in degree along the y axis.")]
        public float Rotation;

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

            string Path(string prefabName) => $"Prefabs/Players/CC4/{prefabName}";
        }
    }

    /// <summary>
    /// Spawns players in multi-player mode.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        /// <summary>
        /// The information needed to spawn player avatars.
        /// </summary>
        /// <remarks>This field must not be readonly. It will be changed by Odin during serialization.</remarks>
        [Tooltip("The information to be used to spawn players."), ShowInInspector, SerializeField]
        private List<SpawnInfo> playerSpawns;

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// </summary>
        private DissonanceComms dissonanceComms = null;

        private void Awake()
        {
            if (playerSpawns == null || playerSpawns.Count == 0)
            {
                playerSpawns = new()
                {
                    new SpawnInfo("Male1", new Vector3(0.4f, 0f, -5.8f), 270),
                    new SpawnInfo("Female1", new Vector3(0.4f, 0f, -6.6f), 270),
                    new SpawnInfo("Male2", new Vector3(0.4f, 0f, -7.8f), 270),
                    new SpawnInfo("Female2", new Vector3(0.4f, 0f, -5.8f), 270),
                    new SpawnInfo("Male3", new Vector3(0.4f, 0f, -6.8f), 270),
                    new SpawnInfo("Female3", new Vector3(0.4f, 0f, -7.8f), 270),
                };
            }
        }

        /// <summary>
        /// Starts the co-routine <see cref="SpawnPlayerCoroutine"/>.
        /// </summary>
        private void OnEnable()
        {
            StartCoroutine(SpawnPlayerCoroutine());
        }

        /// <summary>
        /// RPC method to spawn a player on the server.
        /// </summary>
        /// <param name="clientId">the network ID of the client who is requesting to spawn a local player</param>
        /// <param name="avatarIndex">the index of the avatar to spawn</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void SpawnOnServerRpc(ulong clientId, string playerName, int avatarIndex)
        {

            Spawn(clientId, playerName, avatarIndex);
        }

        /// <summary>
        /// This co-routine sets <see cref="dissonanceComms"/>, registers <see cref="ClientConnects(ulong)"/>
        /// on the <see cref="NetworkManager.Singleton.OnClientConnectedCallback"/> and spawns
        /// the first local client.
        /// </summary>
        /// <returns>enumerator as to how to continue this co-routine</returns>
        private IEnumerator SpawnPlayerCoroutine()
        {
            Net.Network networkConfig = FindObjectOfType<Net.Network>()
                ?? throw new Exception("Network configuration not found.\n");

            // Wait until Dissonance is created.
            while (ReferenceEquals(dissonanceComms, null))
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                if (ReferenceEquals(dissonanceComms, null))
                {
                    yield return null;
                }
                // We need to set the local player name in DissonanceComms
                // before Dissonance is started. That is why we cannot afford
                // to wait until the next frame.
                dissonanceComms.LocalPlayerName = networkConfig.PlayerName;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            // Terminate this co-routine if not run by the server (or host).
            if (!networkManager.IsServer)
            {
                // Note: A connection is established only from a "pure" client
                // - that is, one that is neither server nor host time. A
                // client who is server at the same time (i.e., a host) does
                // not establish a connection with the OnClientConnectedCallback
                // being triggered.
                networkManager.OnClientConnectedCallback += OnClientIsConnected;
                yield break;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // The following code will be executed only on the server (host).
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // The callback to invoke once a client connects. This callback is only
            // ran on the server and on the local client that connects. We want
            // to spawn a player whenever a client connects.
            networkManager.OnClientConnectedCallback += ClientConnects;
            networkManager.OnClientDisconnectCallback += ClientDisconnects;
            // Spawn the local player. This code is executed by the server.
            // "Pure" servers (i.e., those that are not hosts) must not spawn a player.
            if (networkManager.IsHost)
            {
                // Spawn the local player for this host.
                Spawn(networkManager.LocalClientId, Net.Network.GetLocalPlayerName(), 0);
            }
        }

        /// <summary>
        /// Logs given <paramref name="message"/> to the console.
        /// </summary>
        /// <param name="message">message to be logged</param>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string message)
        {
            Debug.Log($"[{nameof(PlayerSpawner)}] {message}\n");
        }

        /// <summary>
        /// Spawns a player using the <see cref="playerSpawns"/>.
        /// </summary>
        /// <param name="clientId">the network ID of the client</param>
        /// <remarks>This code can be executed only on the server.</remarks>
        /// <remarks>Do not confuse client IDs with <see cref="NetworkBehaviour.NetworkObjectId"/>.</remarks>
        private void ClientConnects(ulong clientId)
        {
            Log($"Player with client id {clientId} connects with server (server side).\n");
        }

        private void OnClientIsConnected(ulong clientId)
        {
            Log($"Player with client {clientId} is connected with server (client side).\n");
            SpawnOnServerRpc(clientId, Net.Network.GetLocalPlayerName(), 0);
        }

        private void Spawn(ulong clientId, string nameOfPlayer, int avatarIndex)
        {
            Log($"Player with client {clientId} named {nameOfPlayer} requests to spawn an avatar with index {avatarIndex}.\n");

            // Make sure the index is within the bounds of the playerSpawns list.
            int index = avatarIndex % playerSpawns.Count;
            GameObject player = Instantiate(playerSpawns[index].PlayerPrefab,
                                            playerSpawns[index].Position,
                                            Quaternion.Euler(new Vector3(0, playerSpawns[index].Rotation, 0)));
            player.name = nameOfPlayer;

            Log($"Spawned {player.name} (network id of owner: {clientId}, "
                + $"local: {IsLocal(clientId)}) at position {player.transform.position}.\n");

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

                //GameObject faceCam = PrefabInstantiator.InstantiatePrefab("Prefabs/FaceCam/FaceCam", parent: player.transform);

#if false // FIXME: The FaceCam is already added in the prefab of the player. No need to add it by the code below.

#if !PLATFORM_LUMIN || UNITY_EDITOR
                if (networkManager.IsServer)
                {
                    // Netcode uses a server authoritative networking model so spawning netcode objects
                    // can only be done on a server or host.
                    // Add the FaceCam to the player.
                    GameObject faceCam = PrefabInstantiator.InstantiatePrefab("Prefabs/FaceCam/FaceCam");
                    faceCam.GetComponent<NetworkObject>().Spawn();
                    faceCam.transform.parent = player.transform;
                }
#endif
#endif
            }

            PlayerName.SetPlayerName(player, nameOfPlayer);
        }

        /// <summary>
        /// Emits that the client with given <paramref name="clientId"/> has disconnected.
        /// </summary>
        /// <param name="clientId">the network ID of the disconnecting client</param>
        /// <remarks>Do not confuse client IDs with <see cref="NetworkBehaviour.NetworkObjectId"/>.</remarks>
        private static void ClientDisconnects(ulong clientId)
        {
            Log($"Player with ID {clientId} disconnects.\n");
        }

        /// <summary>
        /// True if <paramref name="clientId"/> identifies the local player.
        /// </summary>
        /// <param name="clientId">the network ID of the owner</param>
        /// <returns>true iff <paramref name="clientId"/> identifies
        /// <see cref="NetworkManager.Singleton.LocalClientId"/></returns>
        private static bool IsLocal(ulong clientId)
        {
            return clientId == NetworkManager.Singleton.LocalClientId;
        }
    }
}
