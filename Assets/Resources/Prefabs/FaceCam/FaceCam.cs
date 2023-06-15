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
using UnityEngine.Serialization;

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
        
        /// <summary>
        /// Texture2D to hold the cutout of the Face
        /// </summary>
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
        string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_6.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

        // Code from the WebCamTextureToMatHelperExample end

        int cutoutTextureX = 0;
        int cutoutTextureY = 0;
        int cutoutTextureWidth = 480; // am anfang die größe der Webcam? sobald webcam gefunden wurde! vorher größé von texture not found bild
        int cutoutTextureHeight = 480; //standartwert für plausible größe damit webcam not found angezeigt wird


        /// <summary>
        /// A timer used to ensure the frame rate of the video transmitted over the network.
        /// It counts the seconds until the video is transmitted. Then it resets.
        /// </summary>
        private float networkVideoTimer;

        /// <summary>
        /// A delay used to ensure the frame rate of the video transmitted over the network.
        /// Seconds until the video is transmitted.
        /// </summary>
        private float networkVideoDelay;

        /// <summary>
        /// Set the frame rate of video network transmission.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Network FPS"), TooltipAttribute("Set the frame rate of Video which will be transmitted over the Network.")]
        protected float _networkFPS = 24f;

        public virtual float networkFPS
        {
            get { return _networkFPS; }
            set
            {
                float _value = Mathf.Clamp(value, 1, float.MaxValue);
                if (_networkFPS != _value)
                {
                    _networkFPS = _value;
                }
            }
        }


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
        }

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        // Start is called before the first frame update
        void Start()
        {
            // Code from the WebCamTextureToMatHelperExample end

            // The Network FPS is used to calculate everything needet to send the video at the specified frame rate.
            networkVideoDelay = 1f / networkFPS;

            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, 0.2f, -1); // z = -1 to face away from the player.

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
            cutoutTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false); //try 0, 0
            // My Code End

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
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
                texture = null;
            }
            if (cutoutTexture != null)
            {
                Texture2D.Destroy(cutoutTexture);
                cutoutTexture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Code from the WebCamTextureToMatHelperExample end

        // Update is called once per frame
        private void Update()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            // Display/Render the video from the Webcam if this is the owner. (For each Player, the Player is the owner of the local FaceCam)
            if (IsOwner)
            {
                DisplayLocalVideo();
            }
        }

        /// <summary>
        /// This renders the video, means the face captured on the WebCam is displayed onto the FaceCam
        /// Only used as owner.
        /// </summary>
        private void DisplayLocalVideo()
        {
            // Used to send video only at specified frame rate.
            networkVideoTimer += Time.deltaTime;

            // Code from the WebCamTextureToMatHelperExample start
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                // My Code
                // This is the Rect which is chosen as the face we want to zoom in.
                UnityEngine.Rect mainRect = new UnityEngine.Rect(0, 0, 0, 0);

                // Is there any rect?
                bool rectFound = false;
                // My Code end
                //Debug.Log("S1");
                foreach (var rect in detectResult)
                {
                    // My Code
                    // find biggest = nearest face, this is the face we want to zoom in
                    //Debug.Log("S2");
                    if (mainRect.height * mainRect.width <= rect.height * rect.width)
                    {
                        //Debug.Log("S3");
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


                /// FIXME NOT FAST IF SEND OVER NETWORTK , ODER? da coutout neu vieliecht geht so
                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);

                // Code from the WebCamTextureToMatHelperExample end


                if (rectFound) // SONST MUSS ALTES GENUZTZ WERDEN
                {
                    int newCutoutTextureX = Mathf.RoundToInt(mainRect.x);
                    int newCutoutTextureY = Mathf.RoundToInt(mainRect.y); // !!!!!!! HIER Y schon umrechnen zu komatiblen wert zwischen TEXTURE und RECT auch danach dann einfachere berechnung
                    int newCutoutTextureWidth = Mathf.RoundToInt(mainRect.width);
                    int newCutoutTextureHeight = Mathf.RoundToInt(mainRect.height); // Right now it is just the size of the cutout face, not yet the cutout texture height.

                    // If rect is inside the texture.
                    //if ((texture.height - newCutoutTextureY - newCutoutTextureHeight) + newCutoutTextureHeight <= texture.height && newCutoutTextureX + newCutoutTextureWidth <= texture.width && newCutoutTextureY >= 0 && newCutoutTextureX >= 0)
                    //{
                        /*
                        Debug.Log("Rect x = " + newCutoutTextureX);
                        Debug.Log("Texture x =  should be 0");
                        Debug.Log("Rect y = " + newCutoutTextureY);
                        Debug.Log("Texture y = should be 0");
                        Debug.Log("Rect width = " + newCutoutTextureWidth);
                        Debug.Log("Texture width= " + texture.width);
                        Debug.Log("Rect height = " + newCutoutTextureHeight);
                        Debug.Log("Texture height = " + texture.height);
                        */
                        cutoutTextureX = newCutoutTextureX;
                        cutoutTextureY = newCutoutTextureY;
                        cutoutTextureWidth = newCutoutTextureWidth;
                        cutoutTextureHeight = newCutoutTextureHeight;
                        // Add a little space over and under the detected head to make it fully visible.
                        int SpaceAbove = cutoutTextureHeight / 2;
                        int SpaceBelow = cutoutTextureHeight / 6;
                        // Add space below for the y position.
                        cutoutTextureY = texture.height - cutoutTextureY - cutoutTextureHeight - SpaceBelow; // Because texture and rect do not both use y the same way, it needs to be converted.
                        // Add space to the height.
                        cutoutTextureHeight = cutoutTextureHeight + SpaceAbove + SpaceBelow; // Now 'cutoutTextureHeight' is the size it should be.
                        // because of addet space, it could be outside
                        cutoutTextureY = Math.Max(0, cutoutTextureY);
                        cutoutTextureX = Math.Max(0, cutoutTextureX);
                        //cutoutTextureHeight = Math.Min(cutoutTextureY + cutoutTextureHeight, texture.height);
                        if (cutoutTextureY + cutoutTextureHeight > texture.height) { cutoutTextureHeight = texture.height - cutoutTextureY; } // falls herausragt über texture, wird abgeschnitten bis rand der textur
                        if (cutoutTextureX + cutoutTextureWidth > texture.width) { cutoutTextureWidth = texture.width - cutoutTextureX; }
                        //cutoutTextureWidth = Math.Min(cutoutTextureX + cutoutTextureWidth, texture.width);

                        // Apply the cutout texture size onto the FacCam
                        // the size is way to big, so it needs to be reduced.
                        transform.localScale = new Vector3(((float)cutoutTextureWidth) / 1000, (float)cutoutTextureHeight / 1000, -1); // without '(float)' the result is just '0'.

                    //}
                }
                // Copy the pixels from the original texture to the cutout texture.

                Debug.Log("Rect Width = " + (cutoutTextureX + cutoutTextureWidth));
                Debug.Log("Text Width = " + texture.width);
                Debug.Log("Rect Height= " + (cutoutTextureY + cutoutTextureHeight));
                Debug.Log("Text Height= " + texture.height);
                Color[] pixels = texture.GetPixels(cutoutTextureX, cutoutTextureY, cutoutTextureWidth, cutoutTextureHeight);
                
                cutoutTexture = new Texture2D(cutoutTextureWidth, cutoutTextureHeight);
                cutoutTexture.SetPixels(pixels);
                cutoutTexture.Apply();
                // Renders the cutout texture onto the FaceCam
                gameObject.GetComponent<Renderer>().material.mainTexture = cutoutTexture;




                // 30 times per second
                if (networkVideoTimer >= networkVideoDelay)
                    {
                        // Perform your action here
                        // This code will execute approximately 30 times per second
                      //  Debug.Log("Time = " + timer);
                        //Debug.Log("FPS = " + 1/networkVideoTimer);
                        //DisplayVideoOnAllOtherClients();
                        networkVideoTimer = 0f; // Reset the timer
                    }
                
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
                transform.Rotate(0, -90, -90); // To face away from the avatars face.
                //FIXME
                //still need to rotate the cam and check which mode (above/front) the FaceCam should be/
                //Maybe update size here, or maybe direct in the video rendering
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void DisplayVideoOnAllOtherClients()
        {
            // A frame of the Video, created from the source video already displayed on this owners client.
            byte[] videoFrame = CreateNetworkFrameFromVideo();

            // videoframe is null if the file size is too big.
            if (videoFrame == null)
            {
                return;
            }
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

        /// <summary>
        /// This creates a frame from the video source.
        /// The frame can be send over the network and is compressed.
        /// </summary>
        private byte[] CreateNetworkFrameFromVideo()
        {
            byte[] networkTexture = ImageConversion.EncodeToJPG(cutoutTexture);
            Debug.Log("Buffer Size: " + networkTexture.Length);
            if (networkTexture.Length <= 32768)
            {
                return networkTexture;
            }
            return null;

        }

        /// <summary>
        /// The owner calls this, to send his video to the server which sends it to all clients.
        /// Also the server and every client will render this video onto the FaceCam.
        /// </summary>
        [ServerRpc]
        private void GetVideoFromClientAndSendItToClientsToRenderIt_ServerRPC(byte[] videoFrame)
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
        private void SendVideoToClientsToRenderIt_ClientRPC(byte[] videoFrame, ClientRpcParams clientRpcParams = default)
        {
            RenderNetworkFrameOnFaceCam(videoFrame);
        }

        /// <summary>
        /// The received frame will be rendered onto the FaceCam
        /// </summary>
        private void RenderNetworkFrameOnFaceCam(byte[] videoFrame)
        {
            ImageConversion.LoadImage(cutoutTexture, videoFrame);
            gameObject.GetComponent<Renderer>().material.mainTexture = cutoutTexture;
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