using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Dissonance.Integrations.Unity_NFGO.Demo
{
    public class NfgoPlayerSpawner
        : MonoBehaviour
    {
        public GameObject PlayerPrefab;

        private void OnEnable()
        {
            StartCoroutine(SpawnCo());
        }

        private IEnumerator SpawnCo()
        {
            var nm = NetworkManager.Singleton;
            if (!nm.IsServer)
                yield break;

            // Wait until Dissonance is created
            DissonanceComms comms = null;
            while (ReferenceEquals(comms, null))
            {
                comms = FindObjectOfType<DissonanceComms>();
                yield return null;
            }

            nm.OnClientConnectedCallback += Spawn;
            Spawn(nm.LocalClientId);
        }

        private void Spawn(ulong owner)
        {
            var pos = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
            var player = Instantiate(PlayerPrefab, pos, Quaternion.identity);
            var net = player.GetComponent<NetworkObject>();
            net.SpawnAsPlayerObject(owner, true);
        }
    }
}
