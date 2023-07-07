#if !(PLATFORM_LUMIN && !UNITY_EDITOR) // This Line of code is from the WebCamTextureToMatHelperExample.

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
using System.Linq;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// This component is assumed to be attached to a game object representing
    /// an avatar (representing a local or remote player). It can display an 
    /// WebCam image of the tracked face of the user over the network.
    /// It Also can be switched off. And it can toggle the position between
    /// above the player always facing the camera, and the front of the players face.
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

        /// <summary>
        /// The WebGL coroutine to get the dlib shape predictor file path.
        /// </summary>
#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Code from the WebCamTextureToMatHelperExample end

        int cutoutTextureX = 0;
        int cutoutTextureY = 0;
        int cutoutTextureWidth = 480; // am anfang die größe der Webcam? sobald webcam gefunden wurde! vorher größé von texture not found bild
        int cutoutTextureHeight = 480; //standartwert für plausible größe damit webcam not found angezeigt wird
        float step = 0.0001F;
        [SerializeField]
        float moveStartSpeed;
        [SerializeField]
        float moveAcceleration;

        int lastFrameNextCutoutTextureX;
         int lastFrameNextCutoutTextureY;
            int        lastFrameNextCutoutTextureWidth;
            int        lastFrameNextCutoutTextureHeight;


        float interpolationFactor = 0;

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
        protected float _networkFPS;

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

        // Start is called before the first frame update.
        void Start()
        {

            // The Network FPS is used to calculate everything needet to send the video at the specified frame rate.
            networkVideoDelay = 1f / networkFPS;

            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, 0.2f, -1); // z = -1 to face away from the player.

            // For the location of the face of the player we use his his nose. This makes the FaceCam also aprox. centered to his face.
            playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");

            // The startup Code from the WebCamTextureToMatHelperExample.
            StartupCodeFromWebCamTextureToMatHelperExample();

            // New texture for the cropped texture only displaying the face, resp. the final texture.
            cutoutTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
        }

        /// <summary>
        /// The startup Code from the WebCamTextureToMatHelperExample.
        /// </summary>
        private void StartupCodeFromWebCamTextureToMatHelperExample()
        {
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

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// </summary>
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
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            //Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(webCamTextureMat, texture);

            //if (fpsMonitor != null)
            //{
            //    fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
            //    fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
            //    fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
            //    fpsMonitor.Add("orientation", Screen.orientation.ToString());
            //}
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            //Debug.Log("OnWebCamTextureToMatHelperDisposed");

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
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        /// <summary>
        /// Once per Frame, the local Video is displayed.
        /// It also checks whether the video should be sent to the clients in this frame based on the specified network FPS.
        /// </summary>
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
                // Used to send video only at specified frame rate.
                networkVideoTimer += Time.deltaTime;
                // Check if this is a Frame in which the video should be transmitted
                if (networkVideoTimer >= networkVideoDelay)
                {
                    DisplayVideoOnAllOtherClients();
                    networkVideoTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Render the video.
        /// The face captured on the WebCam is displayed onto the FaceCam.
        /// (Only used as owner.)
        /// </summary>
        private void DisplayLocalVideo()
        {
            // Code from the WebCamTextureToMatHelperExample.
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                // Code from the WebCamTextureToMatHelperExample.
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                // Code from the WebCamTextureToMatHelperExample.
                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                // Code from the WebCamTextureToMatHelperExample.
                // Detect all face rectangles
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                // This is the rectangle which is selected to be the face we want to zoom in.
                UnityEngine.Rect mainRect = new UnityEngine.Rect(0, 0, 0, 0);

                // Boolean, if there is there any rectangle found.
                bool rectFound = false;

                // Find the biggest, resp. closest Face
                foreach (var rect in detectResult)
                {
                    if (mainRect.height * mainRect.width <= rect.height * rect.width)
                    {
                        mainRect = rect;
                        rectFound = true;
                        //OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                    }
                    //else {
                    //    OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(0, 0, 255, 255), 2);
                    //}

                    //detect landmark points
                    //List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    //OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                    //draw face rect
                    //OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                }

                // Code from the WebCamTextureToMatHelperExample.
                // Convert the material to a 2D texture.
                /// FIXME NOT FAST IF SEND OVER NETWORTK , ODER? da coutout neu vieliecht geht so
                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);

                // If a face is found, calculate the area of the texture which should be displayed.
                // This should be the face with a little bit extra space.
                if (rectFound)
                {
                    // Dimensions of the new found face.
                    /// FIXME: Do i need to clamp to the textures size? The rectangle might maybe be out of the texture.
                    int NextRectX = Mathf.RoundToInt(mainRect.x);
                    int NextRectY = Mathf.RoundToInt(mainRect.y);
                    int NextRectWidth = Mathf.RoundToInt(mainRect.width);
                    int NextRectHeight = Mathf.RoundToInt(mainRect.height);

                    // calculate the space over and under the detected head to make it fully visible.
                    int SpaceAbove = NextRectHeight / 2;
                    int SpaceBelow = NextRectHeight / 6;

                    // Add the Space above and below to the dimension of the cropped texture.
                    int nextCutoutTextureX = NextRectX;
                    int nextCutoutTextureY = Math.Max(0, texture.height - NextRectY - NextRectHeight - SpaceBelow);  // Because texture and rect do not both use y the same way, it needs to be converted.
                    int nextCutoutTextureWidth = NextRectWidth;
                    int nextCutoutTextureHeight = NextRectHeight + SpaceAbove + SpaceBelow;

                    // If the new texture is outside of the original webcam texture, remove the extra space.
                    if (nextCutoutTextureY + nextCutoutTextureHeight > texture.height) { nextCutoutTextureHeight = texture.height - nextCutoutTextureY; }
                    if (nextCutoutTextureX + nextCutoutTextureWidth > texture.width) { nextCutoutTextureWidth = texture.width - nextCutoutTextureX; }

                    // This is the distance which will be ignored, if a face moves.
                    int rectMoveOffset = NextRectWidth/11;
                    // This is the distance which means the face is at a completely new position.
                    int rectPositionOffset = NextRectWidth;

                    // Reset the interpolation factor if the cropped texture already is at the face,
                    // or oterhwise if the face moves a significant amount.
                    if (// Reset the interpolation factor if the rectangle of the face is already at the cropped texture.
                        Math.Abs(nextCutoutTextureX - cutoutTextureX) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureY - cutoutTextureY) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureWidth - cutoutTextureWidth) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureHeight - cutoutTextureHeight) <= rectMoveOffset ||
                        // Or reset the interpolation factor if the rectangle of the face gets a new position.
                        Math.Abs(nextCutoutTextureX - lastFrameNextCutoutTextureX) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureY - lastFrameNextCutoutTextureY) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureWidth - lastFrameNextCutoutTextureWidth) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureHeight - lastFrameNextCutoutTextureHeight) > rectPositionOffset)
                    {
                        interpolationFactor = 0;
                    }

                    // Remember the position of the cropped texture of the last frame, resp. the position right now for the next frame.
                    lastFrameNextCutoutTextureX = nextCutoutTextureX;
                    lastFrameNextCutoutTextureY = nextCutoutTextureY;
                    lastFrameNextCutoutTextureWidth = nextCutoutTextureWidth;
                    lastFrameNextCutoutTextureHeight = nextCutoutTextureHeight;

                    // Calculate the position, if necessary moving towards the new found face with the interpolation factor.
                    cutoutTextureX = Mathf.RoundToInt(Mathf.Lerp(cutoutTextureX, nextCutoutTextureX, interpolationFactor));
                    cutoutTextureY = Mathf.RoundToInt(Mathf.Lerp(cutoutTextureY,nextCutoutTextureY, interpolationFactor));
                    cutoutTextureWidth = Mathf.RoundToInt(Mathf.Lerp(cutoutTextureWidth,nextCutoutTextureWidth, interpolationFactor));
                    cutoutTextureHeight = Mathf.RoundToInt(Mathf.Lerp(cutoutTextureHeight,nextCutoutTextureHeight, interpolationFactor));

                    // Calculate the distance and size difference from the new cropped texture towards the actual rectangle of the face. (There will always be some distance, but more if the face is further away)
                    float distancePosition = Vector2.Distance(new Vector2(cutoutTextureX, cutoutTextureY), mainRect.position);
                    float distanceSize = Vector2.Distance(new Vector2(cutoutTextureWidth, cutoutTextureHeight), mainRect.size);

                    // Calculate the interpolation factor for the next frame.
                    // If the new rectangle is further aways than the actual cropped texture plus half the size of the rectangle, move faster towards the rectangle.
                    if (distancePosition >= NextRectWidth / 2 || distanceSize >= NextRectWidth / 2)
                    {
                        step = step + moveAcceleration * Time.deltaTime;
                    }
                    // Otherwise reset the acceleration.
                    else { step = moveStartSpeed; }

                    // Move towards the rectangle of the face.
                    // Resp. update the interpolation factor which might be reset to 0.
                    interpolationFactor = interpolationFactor + step * Time.deltaTime;

                    // Apply the cutout texture size to the FacCam prefab.
                    // The size is way to big, so it needs to be reduced. A maximum height is used.
                    float maxHeight = 0.24f;
                    float divisor = cutoutTextureHeight / maxHeight;
                    transform.localScale = new Vector3(((float)cutoutTextureWidth) / divisor, (float)cutoutTextureHeight / divisor, -1); // Without '(float)' the result is just '0'.
                }

                // Copy the pixels from the original texture to the cutout texture.
                Color[] pixels = texture.GetPixels(cutoutTextureX, cutoutTextureY, cutoutTextureWidth, cutoutTextureHeight);
                cutoutTexture = new Texture2D(cutoutTextureWidth, cutoutTextureHeight);
                cutoutTexture.SetPixels(pixels);
                cutoutTexture.Apply();

                // Renders the cutout texture onto the FaceCam.
                gameObject.GetComponent<Renderer>().material.mainTexture = cutoutTexture;
            }
        }

        // Called each frame after the Update() function
        private void LateUpdate()
        {
            // Refresh the position of the FaceCam.
            RefreshFaceCamPosition();
        }

        /// <summary>
        /// Refreshes the position of the FaceCam.
        /// </summary>
        private void RefreshFaceCamPosition()
        {
            ///FIXME
            ///Still need to rotate the cam and check which mode (above/front) the FaceCam should be.
            
            if (playersFace != null) // Sometimes the playersFace seems to be null, i can't find out why. Seems to have nothing to do with this class.
            {
                transform.position = playersFace.position;
                transform.rotation = playersFace.rotation;
                transform.Rotate(0, -90, -90); // To face away from the avatars face.
            }
        }

        /// <summary>
        /// Displays the video on any other Client.
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
            else // If this is the owner (creator of video) and also the server.
            {
                // Send the frame to all clients. (But not the server and owner, which in this case, is the server.)
                SendVideoToClientsToRenderIt_ClientRPC(videoFrame, otherNetworkIds);
            }
        }

        /// <summary>
        /// This creates a frame from the video source.
        /// The frame can be send over the network and is compressed.
        /// </summary>
        private byte[] CreateNetworkFrameFromVideo()
        {
            // Converts the Texture to an byte array containing an JPG.
            byte[] networkTexture = ImageConversion.EncodeToJPG(cutoutTexture);
            ///Debug.Log("Buffer Size: " + networkTexture.Length);
            // Only return the array if it's not too big.
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

        /// <summary>
        /// If the FaceCam should be destroyed (player disconnects), clean everything up.
        /// </summary>
        public override void OnDestroy()
        {
            // Remove own ClientId from the list of connected ClientIds
            if (!IsServer && !IsOwner) // Owner and server is not in the list.
            {
                RemoveClientFromList_ServerRPC(ownClientId);
            }

            // Code from the WebCamTextureToMatHelperExample.
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            // Code from the WebCamTextureToMatHelperExample.
            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

            // Code from the WebCamTextureToMatHelperExample.
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
            // Code from the WebCamTextureToMatHelperExample.
            // Always invoke the base.
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

        ///FIXME please use Buttons to do these things or don't do some of them.

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonCkick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif // This Line of code is from the WebCamTextureToMatHelperExample.