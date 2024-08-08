using Dissonance;
using SEE.Game.Drawable;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    /// <summary>
    /// Spawns players in multi-player mode.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        /// <summary>
        /// Information needed to spawn a player avatar.
        /// </summary>
        [Serializable]
        private class SpawnInfo
        {
            [Tooltip("Avatar game object used as prefab")]
            public GameObject PlayerPrefab;
            [Tooltip("World-space position at which to spawn")]
            public Vector3 Position;
            [Tooltip("Rotation in degree along the y axis")]
            public float Rotation;
        }

        /// <summary>
        /// The information needed to spawn player avatars.
        /// </summary>
        [Tooltip("The information to be used to spawn players."), ShowInInspector, SerializeField]
        private List<SpawnInfo> playerSpawns = new();

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// </summary>
        private DissonanceComms dissonanceComms = null;

        /// <summary>
        /// Instance of the NetworkManager
        /// </summary>
        private NetworkManager networkManager = NetworkManager.Singleton;

        /// <summary>
        /// network config to read playername
        /// </summary>
        private SEE.Net.Network networkConfig;

        /// <summary>
        /// Starts the co-routine <see cref="SpawnPlayerCoroutine"/>.
        /// </summary>
        private void OnEnable()
        {
            StartCoroutine(SpawnPlayerCoroutine());
        }

        /// <summary>
        /// This co-routine sets <see cref="dissonanceComms"/>, registers <see cref="Spawn(ulong)"/>
        /// on the <see cref="NetworkManager.Singleton.OnClientConnectedCallback"/> and spawns
        /// the first local client.
        /// </summary>
        /// <returns>enumerator as to how to continue this co-routine</returns>
        private IEnumerator SpawnPlayerCoroutine()
        {
            networkConfig = FindObjectOfType<SEE.Net.Network>();
            if (networkConfig == null)
            {
                Debug.LogError("Network configuration not found");
                yield return null;
            }

            while (ReferenceEquals(networkManager, null))
            {
                networkManager = NetworkManager.Singleton;
                yield return null;
            }

            // Terminate this co-routine if not run by the server, but first set client playername for chat-function.
            if (!networkManager.IsServer)
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                dissonanceComms.LocalPlayerName = networkConfig.PlayerName;
                yield break;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // The following code will be executed only on the server.
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // Wait until Dissonance is created
            while (ReferenceEquals(dissonanceComms, null))
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                dissonanceComms.LocalPlayerName = networkConfig.PlayerName;
                yield return null;
            }

            // The callback to invoke once a client connects. This callback is only
            // ran on the server and on the local client that connects. We want
            // to spawn a player whenever a client connects.
            networkManager.OnClientConnectedCallback += Spawn;
            networkManager.OnClientDisconnectCallback += ClientDisconnects;
            // Spawn the local player. This code is executed by the server.
            Spawn(networkManager.LocalClientId);
        }

        /// <summary>
        /// The number of players spawned so far.
        /// </summary>
        private int numberOfSpawnedPlayers = 0;

        /// <summary>
        /// Spawns a player using the <see cref="playerSpawns"/>.
        /// </summary>
        /// <param name="owner">the network ID of the owner</param>
        /// <remarks>This code can be executed only on the server.</remarks>
        private void Spawn(ulong owner)
        {
            if (owner == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
            {
                return;
            }
            int index = numberOfSpawnedPlayers % playerSpawns.Count;
            GameObject player = Instantiate(playerSpawns[index].PlayerPrefab,
                                            playerSpawns[index].Position,
                                            Quaternion.Euler(new Vector3(0, playerSpawns[index].Rotation, 0)));
            numberOfSpawnedPlayers++;
            player.name = "Player " + numberOfSpawnedPlayers;


#if DEBUG
            Debug.Log($"Spawned {player.name} (network id of owner: {owner}, local: {IsLocal(owner)}) at position {player.transform.position}.\n");
#endif
            if (player.TryGetComponent(out NetworkObject net))
            {
                // By default a newly spawned network Prefab instance is owned by the server
                // unless otherwise specified.
                net.SpawnAsPlayerObject(owner, destroyWithScene: true);
#if DEBUG
                Debug.Log($"Is local player: {net.IsLocalPlayer}. Owner of player {player.name} is server: {net.IsOwnedByServer} or is local client: {net.IsOwner}\n");
#endif
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
            else
            {
                Debug.LogError($"Spawned player {player.name} does not have a {typeof(NetworkObject)} component.\n");
            }
        }

        /// <summary>
        /// Emits that the client with given <paramref name="networkID"/> has disconnected.
        /// </summary>
        /// <param name="networkID">the network ID of the disconnecting client</param>
        private static void ClientDisconnects(ulong networkID)
        {
            Debug.Log($"Player with ID {networkID} (local: {IsLocal(networkID)}) disconnects.\n");
        }

        /// <summary>
        /// True if <paramref name="owner"/> identifies the local player.
        /// </summary>
        /// <param name="owner">the network ID of the owner</param>
        /// <returns>true iff <paramref name="owner"/> identifies
        /// <see cref="NetworkManager.Singleton.LocalClientId"/></returns>
        private static bool IsLocal(ulong owner)
        {
            return owner == NetworkManager.Singleton.LocalClientId;
        }
    }
}
