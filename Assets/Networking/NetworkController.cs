using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://doc.photonengine.com/en-us/pun/current/getting-started/pun-intro
public class NetworkController : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Region: " + PhotonNetwork.CloudRegion);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
