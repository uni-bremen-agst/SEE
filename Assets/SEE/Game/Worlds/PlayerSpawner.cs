using Dissonance;
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
        [Tooltip("The prefab to be used to spawn a player.")]
        public List<GameObject> PlayerPrefab = new List<GameObject>();

        [Tooltip("Ground position where a player is spawned.")]
        public Vector3 InitialPosition = Vector3.zero;

        [Tooltip("Initial rotation of a spawned player.")]
        public Vector3 InitialRotation = Vector3.zero;

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

            // Terminate this co-routine.
            if (!networkManager.IsServer)
            {
                yield break;
            }
            // The following code will be executed only on the server.

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
            // Spawn the local player.
            Spawn(networkManager.LocalClientId);
        }

        /// <summary>
        /// The number of players spawned so far.
        /// </summary>
        private int numberOfSpawnedPlayers = 0;

        /// <summary>
        /// Spawns a player at <see cref="InitialPosition"/> and <see cref="InitialRotation"/>
        /// using the <see cref="PlayerPrefab"/>.
        /// </summary>
        /// <param name="owner">the network ID of the owner</param>
        private void Spawn(ulong owner)
        {
            GameObject player = Instantiate(PlayerPrefab[numberOfSpawnedPlayers % PlayerPrefab.Count],
                                            InitialPosition, Quaternion.Euler(InitialRotation));
            numberOfSpawnedPlayers++;
            player.name = "Player " + numberOfSpawnedPlayers;
            Debug.Log($"Spawned {player.name} (local: {IsLocal(owner)}) at position {player.transform.position}.\n");
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
