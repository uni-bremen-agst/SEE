using Photon.Pun;
using UnityEngine;

namespace SEE
{

    public class PlayerController : MonoBehaviour
    {
        private PhotonView photonView;

        void Start()
        {
            photonView = GetComponent<PhotonView>();
        }

        void Update()
        {
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }

            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
        }
    }

}
