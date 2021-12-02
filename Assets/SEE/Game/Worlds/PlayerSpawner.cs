using Dissonance;
using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

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

        [Tooltip("Initial rotation of a spawned player along the y axis."), Range(0, 360-float.Epsilon)]
        public float InitialRotation = 0;

        /// <summary>
        /// Starts the co-routine <see cref="SpawnPlayerCoroutine"/>.
        /// </summary>
        private void OnEnable()
        {
            StartCoroutine(SpawnPlayerCoroutine());
        }

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
            DissonanceComms comms = null;
            while (ReferenceEquals(comms, null))
            {
                comms = FindObjectOfType<DissonanceComms>();
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
            numberOfSpawnedPlayers++;
            GameObject player = Instantiate(PlayerPrefab[numberOfSpawnedPlayers % PlayerPrefab.Count],
                                            InitialPosition, Quaternion.Euler(0, InitialRotation, 0));
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

        private bool IsLocal(ulong owner)
        {
            return owner == NetworkManager.Singleton.LocalClientId;
        }
    }
}
