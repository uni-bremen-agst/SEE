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
    /// <summary>
    /// This component is assumed to be attached to a game object representing
    /// an avatar (representing a local or remote player). It can display an 
    /// WebCam image of the tracked face of the user over the network.
    /// It Also can be switched off. And it can toggle the position between
    /// above the player always facing the camera, and the front of the player.
    /// </summary>
    public class FaceCam : NetworkBehaviour
    {
        /// <summary>
        /// The object with the position where the face/nose of the player is at.
        /// </summary>
        Transform playersFace;

        /// <summary>
        /// All Network Ids, but not the owner (where the video is recordet) or the server.
        /// </summary>
        List<ulong> allClientIdsList = new List<ulong>();

        /// <summary>
        /// All Network Ids, but not the owner (where the video is recordet) or the server.
        /// </summary>
        ClientRpcParams OtherNetworkIds;

        /// <summary>
        /// Id of this Client.
        /// </summary>
        ulong OwnClientId;

        // Called on Network Spawn before Start.
        public override void OnNetworkSpawn()
        {
            // Add own ClientId to list of Clients, to which the video should be broadcasted.
            OwnClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsServer && !IsOwner)
            {
                AddClientIdToList_ServerRPC(OwnClientId);
            }

            // Always invoked the base.
            base.OnNetworkSpawn();

            /// /// Network Test Examples - DELETE LATER - /// ///
            /// /// Network Test Examples - DELETE LATER - /// ///
            /// /// Network Test Examples - DELETE LATER - /// ///
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

        }

        // Start is called before the first frame update
        private void Start()
        {
            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, -0.48f, -1); // z = -1 to face away from the player. y is negative for simpler calculation later on.

            // For the location of the face of the player we use his his nose. This makes the FaceCam also aprox. centered to his face.
            playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");
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

            // Display/Render the video from the Webcam if this is the owner. (For each Player, there is only one owner of the own FaceCam at the time)
            if (IsOwner) {
                DisplayLocalVideo();
            }

            /// /// Network Test Examples - DELETE LATER - /// ///
            /// /// Network Test Examples - DELETE LATER - /// ///
            /// /// Network Test Examples - DELETE LATER - /// ///

            // Send an RPC as Server to Clients
            if (IsServer) // Only the Server is able to do so even without this if statement // really? JA fa nur server normale client rpcs senden kann
                          //allerdings würden ohne dieses if jeder client/instanz diese nachricht vom server aus senden.
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    PongClientRpc(Time.frameCount, "hello clients, this is server"); // Server -> Client (To all Clients including Server)

                    //Only sent to client number 1
                    DoSomethingServerSide(1);
                }
            }

            // Send to Server
            if (IsClient)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    MyGlobalServerRpc(); // serverRpcParams will be filled in automatically
                }
            }
        }

        /// <summary>
        /// This renders the video, means the face captured on the WebCam is displayed onto the FaceCam
        /// Only used as owner.
        /// </summary>
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

        // Called each frame after the Update() function
        private void LateUpdate()
        {
            // refresh the position of the FaceCam
            RefreshFaceCamPosition();
        }

        /// <summary>
        /// Refreshes the position of the FaceCam
        /// </summary>
        private void RefreshFaceCamPosition() {
            if (playersFace != null) // Sometimes the playersFace seems to be null, i can't find out why. Should have nothing to do with this class.
            {
                transform.position = playersFace.position;
                transform.rotation = playersFace.rotation;
                //FIXME
                //still need to rotate the cam and check which mode (above/front) the FaceCam should be/
                //Maybe update size here, or maybe direct in the video rendering
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
                // A frame of the Video, created from the source video already displayed on this owners client.
                Color videoFrame = CreateNetworkFrameFromVideo();
                //FIXME
                // ggf video ausschneiden oder andere infos wie tile/offset auch übermitteln

                // Send the frame to the to server, unless this is the server.
                if (!IsServer)
                {
                    GetVideoFromClientAndSendItToClientsToRenderIt_ServerRPC(videoFrame);
                }
                else // the owner (creator of video) and also the server
                {
                    // If this is the server, send the frame to all clients ( but not the server and owner, which in this case, is the server).
                    SendVideoToClientsToRenderIt_ClientRPC(videoFrame, OtherNetworkIds);
                }
            }
        }

        /// <summary>
        /// This creates a frame from the video sourcw.
        /// The frame can be send over the network and is compressed.
        /// </summary>
        private Color CreateNetworkFrameFromVideo()
        {
            //return mainColor;
            return new Color(); // is null
            //FIXME
            // hier texture zu JPG machen, und als byte[] übergeben, das sollte reichen.

        }

        /// <summary>
        /// The owner calls this, to send his video to the server which sends it to all clients.
        /// Also the server and every client will render this video onto the FaceCam.
        /// </summary>
        [ServerRpc]
        private void GetVideoFromClientAndSendItToClientsToRenderIt_ServerRPC(Color videoFrame)
        {
            // The server will render this video onto his instance of the FaceCam.
            RenderNetworkFrameOnFaceCam(videoFrame);

            // The server will send the video to all other clients (not the owner and server) so they can display it. 
            SendVideoToClientsToRenderIt_ClientRPC(videoFrame, OtherNetworkIds);
        }

        /// <summary>
        /// The Server calls this, to send his video  to all clients.
        /// Also every client will render this video onto the FaceCam.
        /// </summary>
        [ClientRpc]
        private void SendVideoToClientsToRenderIt_ClientRPC(Color videoFrame, ClientRpcParams clientRpcParams = default)
        {
            RenderNetworkFrameOnFaceCam(videoFrame);
        }

        /// <summary>
        /// The received frame will be rendered onto the FaceCam
        /// </summary>
        private void RenderNetworkFrameOnFaceCam(Color videoFrame)
        {
            //GetComponent<Renderer>().material.color = videoFrame;
            //FIXME
            //Hier den Frame aus JPG zu texture konvertieren, das sollte reichen.
        }

        // Happens on destroying
        public override void OnDestroy()
        {
            // Remove own ClientId from the list of connected ClientIds
            if (!IsServer && !IsOwner) // Owner and server is not in the list.
            {
                RemoveClientFromList_ServerRPC(OwnClientId);
            }

            // Always invoked the base 
            base.OnDestroy();
        }


        /// <summary>
        /// The clients call this to add their ClientId to the list on the Server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void AddClientIdToList_ServerRPC(ulong clientId)
        {
            allClientIdsList.Add(clientId);
            // Create the RpcParams to make this list usable.
            CreateClientRpcParams();
        }

        /// <summary>
        /// The clients call this to remove their ClientId to the list on the Server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RemoveClientFromList_ServerRPC(ulong clientId)
        {
            allClientIdsList.Remove(clientId);
            // Create the RpcParams to make this list usable.
            CreateClientRpcParams();
        }

        /// <summary>
        /// This creates RpcParams from the list of ClientIds to make it usable.
        /// Only the server needs to work with this list.
        /// RpcParams is used to send RPC calls only to few Clients, and not all.
        /// </summary>
        private void CreateClientRpcParams()
        {
            // Creates the needed array from the editable list.
            ulong[] allOtherClientIds = allClientIdsList.ToArray();

            // Creates the RpcParams with the array of ClientIds
            OtherNetworkIds = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = allOtherClientIds
                }
            };
        }


        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        // On the Server send a random value to the client id in the parameter and client id 2
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

        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        // RPC Client method which might be invoked, only runs on Clients
        // from Server to all
        // sends some text and time
        [ClientRpc]
        private void PongClientRpc(int somenumber, string sometext)
        {
            Debug.Log("RPC: " + sometext + " @ Time: " + somenumber);
        }

        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        // RPC Server method which might be invoked, only runs on Server
        // every client can call this
        // save client to do something with it
        // print client id on server
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

        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        /// /// Network Test Examples - DELETE LATER - /// ///
        // from server to all clients
        // On client:
        //  client tell if it's owner 
        //  get random number from server
        [ClientRpc]
        private void DoSomethingClientRpc(int randomInteger, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) {
            Debug.Log("O W N E R !");
            }

            // Run your client-side logic here!!
            Debug.LogFormat("GameObject: {0} has received a randomInteger with value: {1}", gameObject.name, randomInteger);
        }
    }
}

#endif