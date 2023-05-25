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
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
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
        ClientRpcParams otherNetworkIds;

        /// <summary>
        /// Id of this Client.
        /// </summary>
        ulong ownClientId;

        //TEST

        Renderer textureRenderer;

        // Create a new Texture2D to hold the cutout
        Texture2D cutoutTexture;


        // Code from the WebCamTextureToMatHelperExample start

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        // Code from the WebCamTextureToMatHelperExample end

        // Called on Network Spawn before Start.
        public override void OnNetworkSpawn()
        {
            // Add own ClientId to list of Clients, to which the video should be broadcasted.
            ownClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsServer && !IsOwner)
            {
                AddClientIdToList_ServerRPC(ownClientId);
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

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        // Start is called before the first frame update
        void Start()
        {
            // Code from the WebCamTextureToMatHelperExample end

            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, -0.2f, -1); // z = -1 to face away from the player. y is negative for simpler calculation later on.

            // For the location of the face of the player we use his his nose. This makes the FaceCam also aprox. centered to his face.
            playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");

            // Code from the WebCamTextureToMatHelperExample start

            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL
            getFilePath_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync(dlibShapePredictorFileName, (result) =>
            {
                getFilePath_Coroutine = null;

                dlibShapePredictorFilePath = result;
                Run();
            });
            StartCoroutine(getFilePath_Coroutine);
#else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            Run();
#endif
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(webCamTextureMat, texture);

            // My Code
            //cutoutTexture = texture; // ?? referenzen??
            cutoutTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false); //try 0, 0
            //textureRenderer = gameObject.GetComponent<Renderer>();
            //textureRenderer.material.mainTexture = cutoutTexture; // changed from texture to cutoutTexture
            gameObject.GetComponent<Renderer>().material.mainTexture = cutoutTexture;
            // My Code End

            //delete gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            // damit kann man aber jetzt groesse berechnen
            //delete Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            //FIXME Maybe not needet? throws null
            /*
            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
            */
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                //Destroy cutouttextuyre?
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Code from the WebCamTextureToMatHelperExample end

        // FIXME my Code in WebCamTextureToMatHelperExample start
        public void SendFrame()
        {
            byte[] networkTexture = ImageConversion.EncodeToJPG(cutoutTexture);
            Debug.Log("Buffer Size: " + networkTexture.Length);
            if (networkTexture.Length <= 32768)
            {
                SendNetworkTextureClientRPC(networkTexture);
            }
        }

        [ClientRpc]
        private void SendNetworkTextureClientRPC(byte[] networkTexture)
        {
            //texture.LoadRawTextureData(networkTexture);
            //texture.LoadImage(networkTexture);
            ImageConversion.LoadImage(cutoutTexture, networkTexture);
        }
        // FIXME my Code in WebCamTextureToMatHelperExample end

        // Update is called once per frame
        private void Update()
        {
            // Code from the WebCamTextureToMatHelperExample start

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                // My Code
                // This is the Rect which is chosen as the face we want to zoom in.
                UnityEngine.Rect mainRect = new UnityEngine.Rect(0,0,0,0);
                
                // Is there any rect?
                bool rectFound = false;
                // My Code end
                Debug.Log("S1");
                foreach (var rect in detectResult)
                {
                    // My Code
                    // find biggest = nearest face, this is the face we want to zoom in
                    Debug.Log("S2");
                    if (mainRect.height * mainRect.width <= rect.height * rect.width) {
                        Debug.Log("S3");
                        mainRect = rect;
                        rectFound = true;
                    }
                    // My Code end

                    //detect landmark points
                    //List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    //OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);

                // Code from the WebCamTextureToMatHelperExample end

                //Cutour texture?

                // Specify the rectangular region to cut
                if (rectFound)
                {
                    int x = Mathf.RoundToInt(mainRect.x); // Starting x-coordinate of the region
                    int y = Mathf.RoundToInt(mainRect.y); // Starting y-coordinate of the region
                    int width = Mathf.RoundToInt(mainRect.width); // Width of the region
                    int height = Mathf.RoundToInt(mainRect.height); // Height of the region

                    if (y+height <= texture.height && x+width <= texture.width && y > 0 && x > 0) 
                    {
                        cutoutTexture = new Texture2D(width, height);
                        //cutoutTexture.width = width;
                        //cutoutTexture.height = height;

                        // Copy the pixels from the original texture to the cutout texture
                        Color[] pixels = texture.GetPixels(x, y, width, height);
                        cutoutTexture.SetPixels(pixels);
                        cutoutTexture.Apply();
                        //cutoutTexture.Apply(false,false);
                    }

                    
                }


                // Create a new Texture2D to hold the cutout
                //muss das sein?
                gameObject.GetComponent<Renderer>().material.mainTexture = cutoutTexture;
                //textureRenderer.material.mainTexture = cutoutTexture;
                //OpenCVForUnity.UnityUtils.Utils.matToTexture2D(rgbaMat, cutoutTexture);

                Debug.Log("S6");
                // Update size if a face is found. Use ratio to make the bigger of height or width 0.2f big but keep aspect ratio.
                if (rectFound)
                {
                    Debug.Log("S7");
                    if (mainRect.width > mainRect.height)
                    {
                        Debug.Log("S8");
                    //    float ratio = mainRect.width / mainRect.height;
                    //    transform.localScale = new Vector3(0.2f, (-0.2f / ratio) - 0.06f, -1); // - 0.06f because we cut of a little bit more of the height.
                    }
                    else {
                        Debug.Log("S9");
                        // is this right?
                     //   float ratio = mainRect.height / mainRect.width;
                    //    transform.localScale = new Vector3((0.2f / ratio), -0.2f - 0.06f, -1); // - 0.06f because we cut of a little bit more of the height.
                    }
                }

                //FIXME
                if (IsSpawned)
                {
                 //   SendFrame(); // My method
                }
            }

            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            // Display/Render the video from the Webcam if this is the owner. (For each Player, there is only one owner of the own FaceCam at the time)
            if (IsOwner)
            {
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
            switch (ownClientId)
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
            // refresh the position of the WebCamTextureToMatHelperExample
            RefreshFaceCamPosition();
        }

        /// <summary>
        /// Refreshes the position of the FaceCam
        /// </summary>
        private void RefreshFaceCamPosition()
        {
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
                    SendVideoToClientsToRenderIt_ClientRPC(videoFrame, otherNetworkIds);
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
            //Texture2D webtexture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

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
            SendVideoToClientsToRenderIt_ClientRPC(videoFrame, otherNetworkIds);
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
                RemoveClientFromList_ServerRPC(ownClientId);
            }

            // Code from the WebCamTextureToMatHelperExample start

            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif

            // Code from the WebCamTextureToMatHelperExample end

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
            otherNetworkIds = new ClientRpcParams
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
            if (IsOwner)
            {
                Debug.Log("O W N E R !");
            }

            // Run your client-side logic here!!
            Debug.LogFormat("GameObject: {0} has received a randomInteger with value: {1}", gameObject.name, randomInteger);
        }

        // Code from the WebCamTextureToMatHelperExample start

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonCkick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif
// Code from the WebCamTextureToMatHelperExample end