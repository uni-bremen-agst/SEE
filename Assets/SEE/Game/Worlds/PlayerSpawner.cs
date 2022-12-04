using Dissonance;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    /// <summary>
    /// Spawns players in multi-player mode.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
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

        [Tooltip("The prefabs to be used to spawn players."), ShowInInspector, SerializeField]
        private List<SpawnInfo> PlayerSpawns = new List<SpawnInfo>();

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// </summary>
        private DissonanceComms dissonanceComms = null;

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
            NetworkManager networkManager = NetworkManager.Singleton;
            while (ReferenceEquals(networkManager, null))
            {
                networkManager = NetworkManager.Singleton;
                yield return null;
            }

            // Terminate this co-routine if not run by the server.
            if (!networkManager.IsServer)
            {
                yield break;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // The following code will be executed only on the server.
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // Wait until Dissonance is created
            while (ReferenceEquals(dissonanceComms, null))
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                yield return null;
            }

            // The callback to invoke once a client connects. This callback is only
            // ran on the server and on the local client that connects. We want
            // to spawn a player whenever a client connects.
            networkManager.OnClientConnectedCallback += Spawn;
            networkManager.OnClientDisconnectCallback += ClientDisconnects;
            // Spawn the local player.
            Spawn(networkManager.LocalClientId);
        }

        /// <summary>
        /// The number of players spawned so far.
        /// </summary>
        private int numberOfSpawnedPlayers = 0;

        /// <summary>
        /// Spawns a player using the <see cref="PlayerSpawns"/>.
        /// </summary>
        /// <param name="owner">the network ID of the owner</param>
        private void Spawn(ulong owner)
        {
            int index = numberOfSpawnedPlayers % PlayerSpawns.Count;
            GameObject player = Instantiate(PlayerSpawns[index].PlayerPrefab,
                                            PlayerSpawns[index].Position,
                                            Quaternion.Euler(new Vector3(0, PlayerSpawns[index].Rotation, 0)));
            numberOfSpawnedPlayers++;
            player.name = "Player " + numberOfSpawnedPlayers;
            Debug.Log($"Spawned {player.name} (network id: {owner}, local: {IsLocal(owner)}) at position {player.transform.position}.\n");
            if (player.TryGetComponent(out NetworkObject net))
            {
                net.SpawnAsPlayerObject(owner, destroyWithScene: true);
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
        private void ClientDisconnects(ulong networkID)
        {
            Debug.Log($"Player with ID {networkID} (local: {IsLocal(networkID)}) disconnects.\n");
        }

        /// <summary>
        /// True if <paramref name="owner"/> identifies the local player.
        /// </summary>
        /// <param name="owner">the network ID of the owner</param>
        /// <returns>true iff <paramref name="owner"/> identifies
        /// <see cref="NetworkManager.Singleton.LocalClientId"/></returns>
        private bool IsLocal(ulong owner)
        {
            return owner == NetworkManager.Singleton.LocalClientId;
        }
    }
}
