#if !(PLATFORM_LUMIN && !UNITY_EDITOR) // This Line of code is from the WebCamTextureToMatHelperExample.
using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils.Helper;
using SEE.Utils;
using System;
using System.Collections.Generic;
using FaceMaskExample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Rect = UnityEngine.Rect;

namespace SEE.Tools.FaceCam
{
    /// <summary>
    /// This component is assumed to be attached to a game object representing
    /// an avatar (a local or remote player), which can display a
    /// WebCam image of the tracked face of the user over the network.
    /// It can be switched off, and it can toggle the position between being
    /// above the player always facing the camera, and the front of the players face.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class FaceCam : NetworkBehaviour
    {
        /// <summary>
        /// The object with the position where the face/nose of the player is at.
        /// </summary>
        private Transform playersFace;

        /// <summary>
        /// All Network Ids, but not the owner (where the video is recorded) or the server.
        /// </summary>
        private readonly List<ulong> clientsIdsList = new();

        /// <summary>
        /// All Network Ids, but not the owner (where the video is recorded) or the server.
        /// </summary>
        private ClientRpcParams clientsIdsRpcParams;

        /// <summary>
        /// Id of this Client.
        /// </summary>
        private ulong ownClientId;

        /// <summary>
        /// A frame of the webcam video as texture.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// Texture2D of the cropped webcam frame, containing the Face.
        /// </summary>
        private Texture2D croppedTexture;

        /// <summary>
        /// X position of the cropped texture.
        /// </summary>
        private int croppedTextureX;

        /// <summary>
        /// Y position of the cropped texture.
        /// </summary>
        private int croppedTextureY;

        /// <summary>
        /// Width of the cropped texture.
        /// </summary>
        private int croppedTextureWidth = 480; // 480 is a reasonable size to display the 'webcam not found' image.

        /// <summary>
        /// Height of the cropped texture.
        /// </summary>
        private int croppedTextureHeight = 480; // 480 is a reasonable size to display the 'webcam not found' image.

        /// <summary>
        /// X position of the cropped texture of the last frame.
        /// </summary>
        private int lastFrameCutoutTextureX;

        /// <summary>
        /// Y position of the cropped texture of the last frame.
        /// </summary>
        private int lastFrameCutoutTextureY;

        /// <summary>
        /// Width of the cropped texture of the last frame.
        /// </summary>
        private int lastFrameCutoutTextureWidth;

        /// <summary>
        /// Height of the cropped texture of the last frame.
        /// </summary>
        private int lastFrameCutoutTextureHeight;

        /// <summary>
        /// This is the final maximum height of the FaceCam.
        /// </summary>
        private const float MaxHeight = 0.24f;

        /// <summary>
        /// This seems to be the maximum size for files in bytes to be sent over the network.
        /// (No documentation found regarding this limitation).
        /// </summary>
        private const int MaximumNetworkByteSize = 32768;

        /// <summary>
        /// The webcam texture to mat helper from the WebCamTextureToMatHelperExample.
        /// </summary>
        private WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector from the WebCamTextureToMatHelperExample.
        /// </summary>
        private FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The dlib shape predictor file name from the WebCamTextureToMatHelperExample.
        /// </summary>
        private string dlibShapePredictorFileName = "DlibFaceLandmarkDetector/sp_human_face_6.dat";

        /// <summary>
        /// The dlib shape predictor complete file path from the WebCamTextureToMatHelperExample.
        /// </summary>
        private string dlibShapePredictorFilePath;

        /// <summary>
        /// The WebGL coroutine to get the dlib shape predictor file path.
        /// </summary>
#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        /// <summary>
        /// The speed which the face tracking will use to follow the face if it detects one.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Face tracking speed"), Tooltip("Set the speed which the face tracking will use to follow the face if it detects one.")]
        public float _MoveStartSpeed;
        public float MoveStartSpeed
        {
            get => _MoveStartSpeed;
            set
            {
                float _value = Mathf.Abs(value);
                _MoveStartSpeed = _value;
            }
        }

        /// <summary>
        /// The acceleration which occurs after the face tracking found a face.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Face tracking acceleration"), Tooltip("Set the acceleration which occours after the face tracking found a face.")]
        public float _MoveAcceleration;
        public float moveAcceleration
        {
            get => _MoveAcceleration;
            set
            {
                float _value = Mathf.Abs(value);
                _MoveAcceleration = _value;
            }
        }

        /// <summary>
        /// The speed which the face tracking will use to follow the face.
        /// </summary>
        private float faceTrackingSpeed;

        /// <summary>
        /// An interpolation factor, determining how close our position (cropped texture) is to the detected face.
        /// If it is 0 it is just our position on the webcam frame.
        /// If it is 1 our position is exactly the same as the detected face.
        /// </summary>
        private float interpolationFactor;

        /// <summary>
        /// The on/off state of the FaceCam.
        /// </summary>
        private bool FaceCamOn;

        /// <summary>
        /// The state of the position of the FaceCam.
        /// Can be on front of the face or above the face, tilted to the observer.
        /// </summary>
        private bool FaceCamOnFront = true;

        /// <summary>
        /// The mesh renderer of the FaceCam, used to hide it.
        /// </summary>
        public MeshRenderer MeshRenderer;

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
        [SerializeField, FormerlySerializedAs("Network FPS"), Tooltip("Set the frame rate of Video which will be transmitted over the Network.")]
        protected float _NetworkFPS;
        public float networkFPS
        {
            get => _NetworkFPS;
            set
            {
                float _value = Mathf.Clamp(value, 1, float.MaxValue);
                _NetworkFPS = _value;
            }
        }


        // Called on Network Spawn before Start.
        public override void OnNetworkSpawn()
        {
            // Add own ClientId to list of Clients, to which the video should be broadcasted.
            ownClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsServer && !IsOwner)
            {
                AddClientIdToListServerRPC(ownClientId);
            }

            // Always invoked the base.
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// The 'Start' code, called before the first frame update.
        /// The network FPS, size of the FaceCam, and speed of the face tracking is set.
        /// The location of the players face is saved in a variable.
        /// A cropped texture is created.
        /// The startup code from the WebCamTextureToMatHelperExample is executed.
        /// The status of the FaceCam is received it this is not the owner.
        /// </summary>
        private void Start()
        {

            // The network FPS is used to calculate everything needet to send the video at the specified frame rate.
            networkVideoDelay = 1f / networkFPS;

            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, 0.2f, -1); // z = -1 to face away from the player.

            // For the location of the face of the player we use his his nose. This makes the FaceCam also aprox. centered to his face.
            playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");

            // The startup code from the WebCamTextureToMatHelperExample.
            StartupCodeFromWebCamTextureToMatHelperExample();

            // New texture for the cropped texture only displaying the face, resp. the final texture.
            croppedTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false);

            // Receive the status of the FaceCam if this is not the owner.
            if (!IsOwner) {
                GetFaceCamStatusServerRpc();
            }

            // Set the speed of the face tracking.
            faceTrackingSpeed = MoveStartSpeed;
        }

        /// <summary>
        /// The startup Code from the WebCamTextureToMatHelperExample.
        /// </summary>
        private void StartupCodeFromWebCamTextureToMatHelperExample()
        {

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
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
        /// The 'run' code from the WebCamTextureToMatHelperExample.
        /// </summary>
        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                throw new InvalidOperationException("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(webCamTextureMat, texture);

        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }
            if (croppedTexture != null)
            {
                Destroy(croppedTexture);
                croppedTexture = null;
            }
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.LogError("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        /// <summary>
        /// Once per frame, the local video is displayed.
        /// Switches the FaceCam on and off, if the 'I' key is pressed.
        /// It also checks whether the video should be sent to the clients in this frame based on the specified network FPS, and transmit it.
        /// </summary>
        private void Update()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            // Display/render the video from the Webcam if this is the owner. (For each player, the player is the owner of the local FaceCam)
            if (IsOwner)
            {
                // Switch the FaceCam on or off.
                if (Input.GetKeyDown(KeyCode.I))
                {
                    FaceCamOnOffServerRpc(FaceCamOn);
                }

                if (FaceCamOn)
                {
                    // The local video is displayed.
                    DisplayLocalVideo();
                    // Used to send video only at specified frame rate.
                    networkVideoTimer += Time.deltaTime;
                    // Check if this is a Frame in which the video should be transmitted
                    if (networkVideoTimer >= networkVideoDelay)
                    {
                        // Transmit and display the frame on all other clients.
                        DisplayVideoOnAllOtherClients();
                        networkVideoTimer = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Render the video.
        /// The face captured on the Webcam is displayed onto the FaceCam.
        /// (Only executed as owner.)
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
                List<Rect> detectResult = faceLandmarkDetector.Detect();

                // This is the rectangle which is selected to be the face we want to zoom in.
                Rect mainRect = new(0, 0, 0, 0);

                // bool, true if there is there any rectangle found.
                bool rectFound = false;

                // Find the biggest, resp. closest Face
                foreach (Rect rect in detectResult)
                {
                    if (mainRect.height * mainRect.width <= rect.height * rect.width)
                    {
                        mainRect = rect;
                        rectFound = true;
                    }
                }

                // Code from the WebCamTextureToMatHelperExample.
                // Convert the material to a 2D texture.
                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);

                // If a face is found, calculate the area of the texture which should be displayed.
                // This should be the face with a little bit extra space.
                if (rectFound)
                {
                    // Dimensions of the new found face.
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
                    if (nextCutoutTextureY + nextCutoutTextureHeight > texture.height)
                    {
                        nextCutoutTextureHeight = texture.height - nextCutoutTextureY;
                    }
                    if (nextCutoutTextureX + nextCutoutTextureWidth > texture.width)
                    {
                        nextCutoutTextureWidth = texture.width - nextCutoutTextureX;
                    }

                    // This is the distance which will be ignored, if a face moves.
                    int rectMoveOffset = NextRectWidth / 11;
                    // This is the distance which means the face is at a completely new position.
                    int rectPositionOffset = NextRectWidth;

                    // Reset the interpolation factor if the cropped texture already is at the face,
                    // or otherwise if the face moves a significant amount.
                    if (// Reset the interpolation factor if the rectangle of the face is already at the cropped texture.
                        Math.Abs(nextCutoutTextureX - croppedTextureX) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureY - croppedTextureY) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureWidth - croppedTextureWidth) <= rectMoveOffset &&
                        Math.Abs(nextCutoutTextureHeight - croppedTextureHeight) <= rectMoveOffset ||
                        // Or reset the interpolation factor if the rectangle of the face gets a new position.
                        Math.Abs(nextCutoutTextureX - lastFrameCutoutTextureX) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureY - lastFrameCutoutTextureY) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureWidth - lastFrameCutoutTextureWidth) > rectPositionOffset ||
                        Math.Abs(nextCutoutTextureHeight - lastFrameCutoutTextureHeight) > rectPositionOffset)
                    {
                        interpolationFactor = 0;
                    }

                    // Remember the position of the cropped texture of the last frame, resp. the position right now for the next frame.
                    lastFrameCutoutTextureX = nextCutoutTextureX;
                    lastFrameCutoutTextureY = nextCutoutTextureY;
                    lastFrameCutoutTextureWidth = nextCutoutTextureWidth;
                    lastFrameCutoutTextureHeight = nextCutoutTextureHeight;

                    // Calculate the position, if necessary moving towards the new found face with the interpolation factor.
                    croppedTextureX = Mathf.RoundToInt(Mathf.Lerp(croppedTextureX, nextCutoutTextureX, interpolationFactor));
                    croppedTextureY = Mathf.RoundToInt(Mathf.Lerp(croppedTextureY, nextCutoutTextureY, interpolationFactor));
                    croppedTextureWidth = Mathf.RoundToInt(Mathf.Lerp(croppedTextureWidth, nextCutoutTextureWidth, interpolationFactor));
                    croppedTextureHeight = Mathf.RoundToInt(Mathf.Lerp(croppedTextureHeight, nextCutoutTextureHeight, interpolationFactor));

                    // Calculate the distance and size difference from the new cropped texture towards the actual rectangle of the face. (There will always be some distance, but more if the face is further away)
                    float distancePosition = Vector2.Distance(new Vector2(croppedTextureX, croppedTextureY), mainRect.position);
                    float distanceSize = Vector2.Distance(new Vector2(croppedTextureWidth, croppedTextureHeight), mainRect.size);

                    // Calculate the interpolation factor for the next frame.
                    // If the new rectangle is further aways than the actual cropped texture plus half the size of the rectangle, move faster towards the rectangle.
                    if (distancePosition >= NextRectWidth / 2.0 || distanceSize >= NextRectWidth / 2.0)
                    {
                        faceTrackingSpeed += moveAcceleration * Time.deltaTime;
                    }
                    // Otherwise reset the acceleration.
                    else
                    {
                        faceTrackingSpeed = MoveStartSpeed;
                    }

                    // Move towards the rectangle of the face.
                    // Resp. update the interpolation factor which might be reset to 0.
                    interpolationFactor += faceTrackingSpeed * Time.deltaTime;

                    // Apply the cutout texture size to the FacCam prefab.
                    // The size is way to big, so it needs to be reduced. A maximum height is used.
                    float divisor = croppedTextureHeight / MaxHeight;
                    transform.localScale = new Vector3(((float)croppedTextureWidth) / divisor, (float)croppedTextureHeight / divisor, -1); // Without '(float)' the result is just '0'.
                }

                // Copy the pixels from the original texture to the cutout texture.
                Color[] pixels = texture.GetPixels(croppedTextureX, croppedTextureY, croppedTextureWidth, croppedTextureHeight);
                croppedTexture = new Texture2D(croppedTextureWidth, croppedTextureHeight);
                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply();

                // Renders the cutout texture onto the FaceCam.
                gameObject.GetComponent<Renderer>().material.mainTexture = croppedTexture;
            }
        }

        // Called each frame after the Update() function
        private void LateUpdate()
        {
            // Refresh the position of the FaceCam.
            if (FaceCamOn)
            {
                RefreshFaceCamPosition();
            }
        }

        /// <summary>
        /// Refresh the position of the FaceCam.
        /// The position can toggle with the Key 'O'
        /// This means switching the postion between above the avatars face and in front of it.
        /// </summary>
        private void RefreshFaceCamPosition()
        {
            // Switch the position of the FaceCam, if 'O' is pressed.
            if (Input.GetKeyDown(KeyCode.O))
            {
                FaceCamOnFrontToggleServerRpc(FaceCamOnFront);
            }

            // Calculate the position of the FaceCam
            if (playersFace != null) // Sometimes the playersFace seems to be null, i can't find out why. Seems to have nothing to do with this class.
            {
                // Put it where the players face is.
                transform.position = playersFace.position;
                transform.rotation = playersFace.rotation;
                if (FaceCamOnFront)
                {
                    // Rotate and move it a bit up and a bit forward.
                    transform.Rotate(0, -90, -90); // To face away from the avatars face.
                    transform.position += transform.forward * 0.025f;
                    transform.position += transform.up * 0.03f;
                }
                else
                {
                    // Rotate and move it a up and a bit forward.
                    transform.Rotate(0, -90, -90); // To face away from the avatars face.
                    transform.position -= transform.forward * 0.08f;
                    transform.position += transform.up * 0.3f;
                    if (!IsOwner) // If this is the owner the FaceCam should just face forward and not down to the own camera.
                    {
                        // check if there is any main camera, and face towards it.
                        if (MainCamera.Camera != null)
                        {
                            transform.LookAt(MainCamera.Camera.transform);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tell the server to toggle the FaceCam on off state of all clients.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void FaceCamOnOffServerRpc(bool NetworkFaceCamOn) {
            // Toggle the FaceCam on off state of all clients.
            FaceCamOnOffClientRpc(NetworkFaceCamOn);
        }

        /// <summary>
        /// Toggle the FaceCam on off state of all clients.
        /// (Can only be used by the server).
        /// </summary>
        [ClientRpc]
        private void FaceCamOnOffClientRpc(bool NetworkFaceCamOn)
        {
            // NetworkFaceCamOn, resp. FaceCamOn has the value which should be inverted.
            if (FaceCamOn == NetworkFaceCamOn)
            {
                FaceCamOn = !FaceCamOn;
                FaceCamOnOffToggle();
            }
        }

        /// <summary>
        /// Toggle the FaceCam on off state.
        /// </summary>
        private void FaceCamOnOffToggle() {
            if (FaceCamOn)
            {
                webCamTextureToMatHelper.Play();
            }
            else
            {
                webCamTextureToMatHelper.Stop();
            }
            // Hide the FaceCam if it's deactivated.
            MeshRenderer.enabled = FaceCamOn;
        }

        /// <summary>
        /// Tell the server to toggle the FaceCam position of all clients.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void FaceCamOnFrontToggleServerRpc(bool NetworkFaceCamOnFront)
        {
            FaceCamOnFrontToggleClientRpc(NetworkFaceCamOnFront);
        }

        /// <summary>
        /// Toggle the FaceCam position of all clients.
        /// (Can only be used by the server).
        /// </summary
        [ClientRpc]
        private void FaceCamOnFrontToggleClientRpc(bool NetworkFaceCamOnFront)
        {
            if (FaceCamOnFront == NetworkFaceCamOnFront)
            {
                FaceCamOnFront = !FaceCamOnFront;
            }
        }

        /// <summary>
        /// Get the FaceCam status from the server to all clients.
        /// </summary
        [ServerRpc(RequireOwnership = false)]
        private void GetFaceCamStatusServerRpc()
        {
            SetFaceCamStatusClientRpc(FaceCamOn, FaceCamOnFront);
        }

        /// <summary>
        /// Set the FaceCam status on all clients.
        /// (Can only be used by the server).
        /// </summary
        [ClientRpc]
        private void SetFaceCamStatusClientRpc(bool FaceCamOn, bool FaceCamOnFront) {
            this.FaceCamOn = FaceCamOn;
            this.FaceCamOnFront = FaceCamOnFront;
            // Make the FaceCam visible/invisible and/or start/stop it.
            FaceCamOnOffToggle();
        }

        /// <summary>
        /// Displays the video on any client, but not where the video is recorded.
        /// </summary>
        private void DisplayVideoOnAllOtherClients()
        {
            // A frame of the video, created from the source video already displayed on this owners client.
            byte[] videoFrame = CreateNetworkFrameFromVideo();

            // videoframe is null if the file size is too big.
            if (videoFrame == null)
            {
                return;
            }
            // Send the frame to the to server, unless this is the server.
            if (!IsServer)
            {
                GetVideoFromClientAndSendItToClientsToRenderItServerRPC(videoFrame);
            }
            else // If this is the owner (creator of video) and also the server.
            {
                // Send the frame to all clients. (But not the server and owner, which in this case, is the server.)
                SendVideoToClientsToRenderItClientRPC(videoFrame, clientsIdsRpcParams);
            }
        }

        /// <summary>
        /// This creates a frame from the video source.
        /// The frame can be send over the network and is compressed.
        /// </summary>
        private byte[] CreateNetworkFrameFromVideo()
        {
            // Converts the texture to an byte array containing an JPG.
            byte[] networkTexture = ImageConversion.EncodeToJPG(croppedTexture);
            // Only return the array if it's not too big.
            if (networkTexture.Length <= MaximumNetworkByteSize)
            {
                return networkTexture;
            }
            return null;
        }

        /// <summary>
        /// The owner calls this, to send his video to the server which sends it to all clients.
        /// Also the server and every client will render this video onto the FaceCam.
        /// </summary>
        //[ServerRpc(Delivery = RpcDelivery.Unreliable)] // Large files not supported by unreliable Rpc. (No documentation found regarding this limitation).
        [ServerRpc]
        private void GetVideoFromClientAndSendItToClientsToRenderItServerRPC(byte[] videoFrame)
        {
            // The server will render this video onto his instance of the FaceCam.
            RenderNetworkFrameOnFaceCam(videoFrame);

            // The server will send the video to all other clients (not the owner and server) so they can render it. 
            SendVideoToClientsToRenderItClientRPC(videoFrame, clientsIdsRpcParams);
        }

        /// <summary>
        /// The Server calls this, to send his video to all clients.
        /// Also every client will render this video onto the FaceCam.
        /// </summary>
        //[ClientRpc(Delivery = RpcDelivery.Unreliable)]  // Large files not supported by unreliable Rpc. (No documentation found regarding this limitation).
        [ClientRpc]
        private void SendVideoToClientsToRenderItClientRPC(byte[] videoFrame, ClientRpcParams clientRpcParams = default)
        {
            RenderNetworkFrameOnFaceCam(videoFrame);
        }

        /// <summary>
        /// The received frame will be rendered onto the FaceCam
        /// </summary>
        private void RenderNetworkFrameOnFaceCam(byte[] videoFrame)
        {
            croppedTexture.LoadImage(videoFrame);
            gameObject.GetComponent<Renderer>().material.mainTexture = croppedTexture;
        }

        /// <summary>
        /// If the FaceCam should be destroyed (player disconnects), clean everything up.
        /// </summary>
        public override void OnDestroy()
        {
            // Remove own ClientId from the list of connected ClientIds
            if (!IsServer && !IsOwner) // Owner and server is not in the list.
            {
                RemoveClientFromListServerRPC(ownClientId);
            }

            // Code from the WebCamTextureToMatHelperExample.
            if (webCamTextureToMatHelper != null)
            {
                webCamTextureToMatHelper.Dispose();
            }

            // Code from the WebCamTextureToMatHelperExample.
            if (faceLandmarkDetector != null)
            {
                faceLandmarkDetector.Dispose();
            }

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
        private void AddClientIdToListServerRPC(ulong clientId)
        {
            clientsIdsList.Add(clientId);
            // Create the RpcParams from the list to make the list usable as RpcParams.
            CreateClientRpcParams();
        }

        /// <summary>
        /// The clients call this to remove their ClientId from the list on the Server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RemoveClientFromListServerRPC(ulong clientId)
        {
            clientsIdsList.Remove(clientId);
            // Create the RpcParams to make this list usable.
            CreateClientRpcParams();
        }

        /// <summary>
        /// This creates RpcParams from the list of ClientIds to make it usable.
        /// Only the server needs to work with this list.
        /// RpcParams is used to send RPC calls only to few Clients, and not to all.
        /// </summary>
        private void CreateClientRpcParams()
        {
            // Creates the needed array from the editable list.
            ulong[] allOtherClientIds = clientsIdsList.ToArray();

            // Creates the RpcParams with the array of ClientIds
            clientsIdsRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = allOtherClientIds
                }
            };
        }
    }
}

#endif // This Line of code is from the WebCamTextureToMatHelperExample.

// Autor of WebCamTextureToMatHelperExample.cs: Enox Software, enoxsoftware.com/, enox.software@gmail.com
