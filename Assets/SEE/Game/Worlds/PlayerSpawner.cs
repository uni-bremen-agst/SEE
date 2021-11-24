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

        [Tooltip("Position where a player is spawned.")]
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
            var nm = NetworkManager.Singleton;
            while (ReferenceEquals(nm, null))
            {
                nm = NetworkManager.Singleton;
                yield return null;
            }

            if (!nm.IsServer)
                yield break;

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
            nm.OnClientConnectedCallback += Spawn;
            // Spawn the local player.
            Spawn(nm.LocalClientId);
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
            Debug.Log($"Spawned {player.name}.\n");
            if (player.TryGetComponent(out NetworkObject net))
            {
                net.SpawnAsPlayerObject(owner, destroyWithScene: true);
            }
            else
            {
                Debug.LogError($"Spawned player {player.name} does not have a {typeof(NetworkObject)} component.\n");
            }
        }
    }
}
