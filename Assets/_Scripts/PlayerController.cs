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

            transform.position = Camera.main.transform.position + 3*Camera.main.transform.forward;
            transform.rotation = Camera.main.transform.rotation;
        }
    }

}
