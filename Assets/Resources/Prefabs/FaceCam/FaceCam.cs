#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace DlibFaceLandmarkDetectorExample
{
    public class FaceCam : NetworkBehaviour
    {
        // The eobject with the position where the face of the player is at.
        Transform playersFace;
        bool testetForOwner = false;
        Color mainColor;
        List<ulong> allClientIdsList = new List<ulong>();
        ClientRpcParams OtherNetworkIds; // Not Server and not the Owner
        ulong OwnClientId;

        // Called on Network Spawn before Start
        public override void OnNetworkSpawn()
        {
            // Do things
            if (IsServer)
            {
                Debug.Log("IsServer");
            }
            else
            {
                Debug.Log("IsClient (But not the Server)");
            }
            if (IsClient)
            {
                Debug.Log("IsClient (Server is a Client too)");
            }


            OwnClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsServer && !IsOwner)
            {
                AddClientIdToListServerRPC(OwnClientId);
            }
            //UpdateOtherNetworkIDsNoServer();


            // Always invoked the base 
            base.OnNetworkSpawn();
        }

        // Start is called before the first frame update
        private void Start()
        {
            transform.localScale = new Vector3(0.2f, -0.48f, -1); // z = -1 to face away from the player.
            //transform.position = transform.parent.position;
            playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");
            Debug.Log("playersFace: " + playersFace);
            //mainColor = GetComponent<Renderer>().material.color;
        }


        // Update is called once per frame
        private void Update()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            //transform.parent = transform.parent.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase").transform;
            //Debug.Log("Nose");



            // Send an RPC
            if (IsServer) // Only the Server is able to do so even without this if statement
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    PongClientRpc(Time.frameCount, "hello clients, this is server"); // Server -> Client (To all Clients including Server)

                    //Only sent to client number 1
                    DoSomethingServerSide(1);
                }


            }

            if (IsClient)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    MyGlobalServerRpc(); // serverRpcParams will be filled in automatically
                }
            }

            // Mark this network instance as owner on the client it belongs to.
            if (!testetForOwner)
            {
                if (IsOwner)
                {
                    //GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/LSHMetal/29");
                    //GetComponent<Renderer>().material.color = new Color(255, 0, 0);
                }
                testetForOwner = true;
            }

            if (IsOwner) {
                DisplayLocalVideo();
            }
        }

        private void DisplayLocalVideo()
        {
            //geht davon aus das man Owner ist
            //Debug.Log("Network Id: " + NetworkManager.Singleton.LocalClientId);
            switch (OwnClientId)
            {
                case 0:
                    //mainColor = new Color(Random.Range(100, 255), 0, 0);
                    break;
                case 1:
                    //mainColor = new Color(0, Random.Range(100, 255), 0);
                    break;
                default:
                    //mainColor = new Color(0, 0, Random.Range(100, 255));
                    break;
            }
        }


        private void LateUpdate()
        {
            FixToNose();
        }

        // Follow the position of the players nose
        private void FixToNose() {
            if (playersFace != null)
            {
                transform.position = playersFace.position;
                transform.rotation = playersFace.rotation;
            }
        }

        // FixedUpdate is normally called 50 times per Second
        private void FixedUpdate()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            // If local, record and send video to server (only send unless this is the server);
            if (IsOwner)
            {
                // The video Fähig übers netz gesendet zu werden
                Color videoFrame = CreateNetworkVideoFromSource();

                // Sent to server, unless this is the server
                if (!IsServer)
                {
                    GetVideoFromClients_ServerRPC(videoFrame);
                }
                else //Falls Server, Rendere eigenes Video, und sende zu allen Clients
                {
                    //RenderVideoOnFaceCam(videoFrame);
                    SendVideoToClients_ClientRPC(videoFrame, OtherNetworkIds);// An Alllle! (auuser immer denm serverclient) und dem server
                }
            }
            //else
            // if not local, get video from server;
            //{
            //        SendVideoToClients_ClientRPC(videoFrame);
            //}

        }

        private Color CreateNetworkVideoFromSource()
        {
            return mainColor;
        }


        [ServerRpc]
        private void GetVideoFromClients_ServerRPC(Color videoFrame)
        {
            RenderNetworkVideoOnFaceCam(videoFrame);
            SendVideoToClients_ClientRPC(videoFrame, OtherNetworkIds);
            //Send to all clients but not the received one (auuser immer denm serverclient den auch nicht natürlich)
            //erstmal alle
        }

        [ClientRpc]
        private void SendVideoToClients_ClientRPC(Color videoFrame, ClientRpcParams clientRpcParams = default)
        {
            RenderNetworkVideoOnFaceCam(videoFrame);
        }


        private void RenderNetworkVideoOnFaceCam(Color videoFrame)
        {
            //GetComponent<Renderer>().material.color = videoFrame;
        }

        // Happens on destroying
        public override void OnDestroy()
        {
            // Clean up your NetworkBehaviour
            if (!IsServer && !IsOwner)
            {
                RemoveClientFromListServerRPC(OwnClientId);
            }
            //UpdateOtherNetworkIDsNoServer();

            // Always invoked the base 
            base.OnDestroy();
        }

        /*
        ulong[] allNetworkIds;
        ClientRpcParams otherNetworkIDsNoServer;
        ulong ServerID;
        ulong OwnClientId;

        ClientRpcParams otherNetworkIDsNoServer;
        ulong ServerID;
        */

        //RPC!
        [ServerRpc(RequireOwnership = false)]
        private void AddClientIdToListServerRPC(ulong clientId)
        {
            allClientIdsList.Add(clientId);
            CreateClientRpcParams();
        }
        [ServerRpc(RequireOwnership = false)]
        private void RemoveClientFromListServerRPC(ulong clientId)
        {
            allClientIdsList.Remove(clientId);
            CreateClientRpcParams();
        }
        private void CreateClientRpcParams()
        {
            ulong[] allOtherClientIds = allClientIdsList.ToArray();
            OtherNetworkIds = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = allOtherClientIds
                }
            };
        }



        private void DoSomethingServerSide(int clientId)
        {
            // If isn't the Server/Host then we should early return here!
            if (!IsServer) return;


            // NOTE! In case you know a list of ClientId's ahead of time, that does not need change,
            // Then please consider caching this (as a member variable), to avoid Allocating Memory every time you run this function
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { (ulong)clientId, (ulong)2 }
                }
            };

            // Let's imagine that you need to compute a Random integer and want to send that to a client
            const int maxValue = 4;
            int randomInteger = UnityEngine.Random.Range(0, maxValue);
            DoSomethingClientRpc(randomInteger, clientRpcParams);
        }

        // RPC Client method which might be invoked, only runs on Clients
        [ClientRpc]
        private void PongClientRpc(int somenumber, string sometext)
        {
            Debug.Log("RPC: " + sometext + " @ Time: " + somenumber);
        }

        // RPC Server method which might be invoked, only runs on Server
        [ServerRpc(RequireOwnership = false)]
        private void MyGlobalServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                var client = NetworkManager.ConnectedClients[clientId];
                // Do things for this client

                // On Server only, display Client
                Debug.Log("RPC: This is Client: " + clientId);
            }
        }

        [ClientRpc]
        private void DoSomethingClientRpc(int randomInteger, ClientRpcParams clientRpcParams = default)
        {
            //if (IsOwner) return;
            Debug.Log("O W N E R !");

            // Run your client-side logic here!!
            Debug.LogFormat("GameObject: {0} has received a randomInteger with value: {1}", gameObject.name, randomInteger);
        }
    }
}

#endif