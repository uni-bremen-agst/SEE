using Photon.Pun;
using System.IO;
using UnityEngine;

namespace SEE
{

    public class GameSetupController : MonoBehaviour
    {
        void Start()
        {
            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            PhotonNetwork.Instantiate(Path.Combine("Prefabs", "Player"), Vector3.zero, Quaternion.identity);
        }
    }

}// namespace SEE
