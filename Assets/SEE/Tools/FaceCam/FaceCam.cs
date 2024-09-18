#if !PLATFORM_LUMIN || UNITY_EDITOR // This Line of code is from the WebCamTextureToMatHelperExample.
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
using SEE.Controls;
using SEE.GO;

namespace SEE.Tools.FaceCam
{
    /// <summary>
    /// This component is attached to a FaceCam.prefab, which will be instantiated
    /// as an immediate child to a game object representing an avatar (a local or
    /// remote player). It can be used to display a WebCam image of the tracked
    /// face of the user over the network.
    /// It can be switched off, and it can toggle the position between being
    /// above the player always facing the camera, and the front of the player's face.
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
        /// This attribute is assigned in <see cref="CreateClientRpcParams()"/>, but never
        /// read.
        /// TODO: Is it really needed? Maybe the assigned value is kept in this field
        /// such that it will not be cleaned up by the garbage collector.
        /// </summary>
        private ClientRpcParams clientsIdsRpcParams;

        /// <summary>
        /// Network id of this client. After instantiated locally, each NetworkObject is assigned a
        /// NetworkObjectId that's used to associate NetworkObjects across the network. For example,
        /// one peer can say "Send this RPC to the object with the NetworkObjectId 103," and everyone
        /// knows what object it's referring to. A NetworkObject is spawned on a client is when it's
        /// instantiated and assigned a unique NetworkObjectId.
        /// </summary>
        private ulong ownClientId;

        /// <summary>
        /// A frame of the webcam video as texture.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// Texture2D of the cropped webcam frame, containing the face.
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
        private int croppedTextureWidth = 480; // 480 is a reasonable size to
                                               // display the 'webcam not found' image.

        /// <summary>
        /// Height of the cropped texture.
        /// </summary>
        private int croppedTextureHeight = 480; // 480 is a reasonable size to display
                                                // the 'webcam not found' image.

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
        private const float maxHeight = 0.24f;

        /// <summary>
        /// This seems to be the maximum size for files in bytes to be sent over the network.
        /// (No documentation found regarding this limitation).
        /// </summary>
        private const int maximumNetworkByteSize = 32768;

        /// <summary>
        /// The relative path to the bone of the face/nose of the player.
        /// This will be used to position the FaceCam.
        /// </summary>
        private const string faceCamOrientationBone = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

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
        [SerializeField, FormerlySerializedAs("Face tracking speed"),
            Tooltip("Set the speed which the face tracking will use to follow the face if it detects one.")]
        private float moveStartSpeed;
        public float MoveStartSpeed
        {
            get => moveStartSpeed;
            set => moveStartSpeed = Mathf.Abs(value);
        }

        /// <summary>
        /// The acceleration which occurs after the face tracking found a face.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Face tracking acceleration"),
            Tooltip("Set the acceleration which occurs after the face tracking found a face.")]
        private float moveAcceleration;
        public float MoveAcceleration
        {
            get => moveAcceleration;
            set => moveAcceleration = Mathf.Abs(value);
        }

        /// <summary>
        /// The speed which the face tracking will use to follow the face.
        /// </summary>
        private float faceTrackingSpeed;

        /// <summary>
        /// An interpolation factor, determining how close our position (cropped texture)
        /// is to the detected face.
        /// If it is 0 it is just our position on the webcam frame.
        /// If it is 1 our position is exactly the same as the detected face.
        /// </summary>
        private float interpolationFactor;

        /// <summary>
        /// The on/off state of the FaceCam.
        /// </summary>
        private bool faceCamOn;

        /// <summary>
        /// The state of the position of the FaceCam.
        /// Can be on front of the face or above the face, tilted to the observer.
        /// </summary>
        private bool faceCamOnFront = true;

        /// <summary>
        /// The mesh renderer of the FaceCam, used to hide it.
        /// </summary>
        private MeshRenderer meshRenderer;

        /// <summary>
        /// The material of the FaceCam, its texture displaying a default picture, or
        /// the face of the user.
        /// </summary>
        private Material mainMaterial;

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
        [SerializeField, FormerlySerializedAs("Network FPS"),
            Tooltip("Set the frame rate of the video which will be transmitted over the Network.")]
        private float networkFPS;
        public float NetworkFPS
        {
            get => networkFPS;
            set => networkFPS = Mathf.Clamp(value, 1, float.MaxValue);
        }

        /// <summary>
        /// Adds the own client id to the server's list of clients to which the video should
        /// be broadcasted if and only
        ///
        /// </summary>
        /// <remarks>Called on network spawn before Start through NetCode.</remarks>
        public override void OnNetworkSpawn()
        {
            Initialize();

            // IsOwner is true if the local client is the owner of this NetworkObject.
            // IsServer is true if this code runs on the server. Note: a host can be
            // a server and a local client at the same time, in which case IsServer
            // would also be true.
            // The default NetworkObject.Spawn method assumes server-side ownership,
            // but the ownership can be transferred to a client (and also returned to
            // the server again). We do not do that. That is, our server always owns
            // all network objects.

            // Add own ClientId to list of Clients, to which the video should be broadcasted.
            ownClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsServer && !IsOwner)
            {
                AddClientIdToListServerRPC(ownClientId);
            }

            // Always invoke the base.
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// Initializes <see cref="webCamTextureToMatHelper"/> if not already set.
        /// </summary>
        private void Initialize()
        {
            // For dynamically spawned NetworkObjects (instantiating a network Prefab
            // during runtime) the OnNetworkSpawn method is invoked before the Start
            // method is invoked. So, it's important to be aware of this because finding
            // and assigning components to a local property within the Start method exclusively
            // will result in that property not being set in a NetworkBehaviour component's
            // OnNetworkSpawn method when the NetworkObject is dynamically spawned. To
            // circumvent this issue, you can have a common method that initializes the
            // components and is invoked both during the Start method and the
            // OnNetworkSpawned method. That's the purpose of this method.

            if (webCamTextureToMatHelper == null)
            {
                if (!gameObject.TryGetComponentOrLog(out webCamTextureToMatHelper))
                {
                    enabled = false;
                }
            }

            if (!gameObject.TryGetComponentOrLog(out meshRenderer))
            {
                gameObject.SetActive(false);
                return;
            }
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
            // The network FPS is used to calculate everything needed to send the video
            // at the specified frame rate.
            networkVideoDelay = 1f / NetworkFPS;

            // This is the size of the FaceCam at the start
            transform.localScale = new Vector3(0.2f, 0.2f, -1); // z = -1 to face away from the player.

            // For the location of the face of the player we use his right eye. This makes
            // the FaceCam also aprox. centered to his face.
            playersFace = transform.parent.Find(faceCamOrientationBone);
            if (playersFace == null)
            {
                Debug.LogError($"[FaceCam.Start] Could not find the bone {faceCamOrientationBone}.\n");
                enabled = false;
                return;
            }

            Initialize();

            // The startup code from the WebCamTextureToMatHelperExample.
            StartupCodeFromWebCamTextureToMatHelperExample();

            // New texture for the cropped texture only displaying the face, resp. the final texture.
            croppedTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false);

            // Receive the status of the FaceCam if this is not the owner.
            if (!IsOwner)
            {
                GetFaceCamStatusServerRpc();
            }

            // Set the speed of the face tracking.
            faceTrackingSpeed = MoveStartSpeed;

            // Cache the material of the FaceCam to change its texture later. (Display a default
            // picture or the face of the user).
            mainMaterial = meshRenderer.material;
        }

        /// <summary>
        /// The startup Code from the WebCamTextureToMatHelperExample.
        /// </summary>
        private void StartupCodeFromWebCamTextureToMatHelperExample()
        {
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

            /// <summary>
            /// The 'run' code from the WebCamTextureToMatHelperExample.
            /// </summary>
            void Run()
            {
                if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
                {
                    throw new InvalidOperationException
                        ("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/DlibFaceLandmarkDetector/” to “Assets/StreamingAssets/DlibFaceLandmarkDetector/” folder. ");
                }

                faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

                webCamTextureToMatHelper.Initialize();
            }
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
                Destroyer.Destroy(texture);
                texture = null;
            }
            if (croppedTexture != null)
            {
                Destroyer.Destroy(croppedTexture);
                croppedTexture = null;
            }
        }

        /// <summary>
        /// Code from the WebCamTextureToMatHelperExample.
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public static void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.LogError($"OnWebCamTextureToMatHelperErrorOccurred {errorCode}\n");
        }

        /// <summary>
        /// Once per frame, the local video is displayed.
        /// Switches the FaceCam on and off, if the 'I' key is pressed.
        /// It also checks whether the video should be sent to the clients in this frame - based
        /// on the specified network FPS - and transmits it.
        /// </summary>
        private void Update()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }
            // Netcode specific logic executed when spawned.

            // Display/render the video from the Webcam if this is the owner.
            // The local client owns the player object the NetworkObject is attached to.
            // The FaceCam is attached to a child of the local player.
            // Hence, the local player (client) is the owner of the local FaceCam.
            // NetworkBehaviour.IsOwner is true if the local client is the owner of this NetworkObject.
            if (IsOwner)
            {
                // Switch the FaceCam on or off.
                if (SEEInput.ToggleFaceCam())
                {
                    FaceCamOnOffServerRpc(faceCamOn);
                }

                if (faceCamOn)
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
                    int nextRectX = Mathf.RoundToInt(mainRect.x);
                    int nextRectY = Mathf.RoundToInt(mainRect.y);
                    int nextRectWidth = Mathf.RoundToInt(mainRect.width);
                    int nextRectHeight = Mathf.RoundToInt(mainRect.height);

                    // calculate the space over and under the detected head to make it fully visible.
                    int spaceAbove = nextRectHeight / 2;
                    int spaceBelow = nextRectHeight / 6;

                    // Add the Space above and below to the dimension of the cropped texture.
                    int nextCutoutTextureX = nextRectX;
                    // Because texture and rect do not both use y the same way, it needs to be converted.
                    int nextCutoutTextureY = Math.Max(0, texture.height - nextRectY - nextRectHeight - spaceBelow);
                    int nextCutoutTextureWidth = nextRectWidth;
                    int nextCutoutTextureHeight = nextRectHeight + spaceAbove + spaceBelow;

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
                    int rectMoveOffset = nextRectWidth / 11;
                    // This is the distance which means the face is at a completely new position.
                    int rectPositionOffset = nextRectWidth;

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

                    // Calculate the distance and size difference from the new cropped texture towards the actual
                    // rectangle of the face. (There will always be some distance, but more if the face is further away)
                    float distancePosition = Vector2.Distance(new Vector2(croppedTextureX, croppedTextureY), mainRect.position);
                    float distanceSize = Vector2.Distance(new Vector2(croppedTextureWidth, croppedTextureHeight), mainRect.size);

                    // Calculate the interpolation factor for the next frame.
                    // If the new rectangle is further away than the actual cropped texture plus half the size of the rectangle,
                    // move faster towards the rectangle.
                    if (distancePosition >= nextRectWidth / 2.0 || distanceSize >= nextRectWidth / 2.0)
                    {
                        faceTrackingSpeed += MoveAcceleration * Time.deltaTime;
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
                    float divisor = croppedTextureHeight / maxHeight;
                    transform.localScale = new Vector3(croppedTextureWidth / divisor, croppedTextureHeight / divisor, -1);
                }

                // Copy the pixels from the original texture to the cutout texture.
                Color[] pixels = texture.GetPixels(croppedTextureX, croppedTextureY, croppedTextureWidth, croppedTextureHeight);
                croppedTexture = new Texture2D(croppedTextureWidth, croppedTextureHeight);
                croppedTexture.SetPixels(pixels);
                croppedTexture.Apply();

                // Renders the cutout texture onto the FaceCam.
                mainMaterial.mainTexture = croppedTexture;
            }
        }

        /// <summary>
        /// Updates the position of the FaceCam if it is turned on.
        /// </summary>
        /// <remarks>Called by Unity each frame after the Update() function.</remarks>
        ///
        private void LateUpdate()
        {
            if (faceCamOn)
            {
                RefreshFaceCamPosition();
            }
        }

        /// <summary>
        /// Refresh the position of the FaceCam.
        /// The position can be toggled with <see cref="SEEInput.ToggleFaceCamPosition"/>.
        /// This means switching the position between above the avatars face and in front of it.
        /// </summary>
        private void RefreshFaceCamPosition()
        {
            // Switch the position of the FaceCam.
            if (SEEInput.ToggleFaceCamPosition())
            {
                FaceCamOnFrontToggleServerRpc(faceCamOnFront);
            }

            // Calculate the position of the FaceCam
            if (playersFace != null) // Sometimes the playersFace seems to be null, i can't find out why.
                                     // Seems to have nothing to do with this class.
            {
                // Put it where the player's face is.
                transform.SetPositionAndRotation(playersFace.position, playersFace.rotation);
                if (faceCamOnFront)
                {
                    // Move it a bit up and a bit forward.
                    transform.position += transform.forward * 0.15f;
                    transform.position += transform.up * 0.06f;
                }
                else
                {
                    // Move it a bit up and a bit forward.
                    transform.position += transform.forward * 0.1f;
                    transform.position += transform.up * 0.3f;
                    if (!IsOwner) // If this is the owner the FaceCam should just face forward and
                                  // not down to the own camera.
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
        /// Tell the server to toggle the FaceCam on/off for all clients.
        /// </summary>
        /// <remarks>
        /// This call will be sent to the server. The default [ServerRpc] attribute
        /// setting allows only a client owner (client that owns the NetworkObject associated
        /// with the NetworkBehaviour containing the ServerRpc method) invocation rights.
        /// Any client that isn't the owner won't be allowed to invoke the ServerRpc.
        /// By setting the ServerRpc attribute's RequireOwnership parameter to false,
        /// any client has ServerRpc invocation rights.
        /// </remarks>
        [ServerRpc(RequireOwnership = false)]
        private void FaceCamOnOffServerRpc(bool networkFaceCamOn, ServerRpcParams serverRpcParams = default)
        {
            // A ServerRpc is a remote procedure call (RPC) that can be only invoked
            // by a client and will always be received and executed on the server/host.
#if DEBUG
            Debug.Log($"[RPC] Server received FaceCamOnOffServerRpc from {serverRpcParams.Receive.SenderClientId} with networkFaceCamOn={networkFaceCamOn}\n");
#endif
            FaceCamOnOffClientRpc(networkFaceCamOn);
        }

        /// <summary>
        /// Toggle the FaceCam on/off for all clients.
        /// (Can only be used by the server).
        /// </summary>
        /// <remarks>This call is sent from the server to all its clients.</remarks>
        [ClientRpc]
        private void FaceCamOnOffClientRpc(bool networkFaceCamOn)
        {
#if DEBUG
            Debug.Log($"[RPC] Client {NetworkManager.Singleton.LocalClientId} received FaceCamOnOffClientRpc from server with networkFaceCamOn={networkFaceCamOn}\n");
#endif

            // Note: The host is both a client and a server. If a host invokes a client RPC,
            // it triggers the call on all clients, including the host.
            //
            // When running as a host, Netcode for GameObjects invokes RPCs immediately within the
            // same stack as the method invoking the RPC. Since a host is both considered a server
            // and a client, you should avoid design patterns where a ClientRpc invokes a ServerRpc
            // that invokes the same ClientRpc as this can end up in a stack overflow (infinite
            // recursion).

            // NetworkFaceCamOn, resp. FaceCamOn has the value which should be inverted.
            if (faceCamOn == networkFaceCamOn)
            {
                faceCamOn = !faceCamOn;
                FaceCamOnOffToggle();
            }
        }

        /// <summary>
        /// Toggle the FaceCam on off state.
        /// </summary>
        private void FaceCamOnOffToggle()
        {
            if (faceCamOn)
            {
                webCamTextureToMatHelper.Play();
            }
            else
            {
                webCamTextureToMatHelper.Stop();
            }
            // Hide the FaceCam if it's deactivated.
            meshRenderer.enabled = faceCamOn;
        }

        /// <summary>
        /// Tell the server to toggle the FaceCam position of all clients.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void FaceCamOnFrontToggleServerRpc(bool networkFaceCamOnFront, ServerRpcParams serverRpcParams = default)
        {
#if DEBUG
            Debug.Log($"[RPC] Server received FaceCamOnFrontToggleServerRpc from {serverRpcParams.Receive.SenderClientId} with networkFaceCamOn={networkFaceCamOnFront}\n");
#endif
            FaceCamOnFrontToggleClientRpc(networkFaceCamOnFront);
        }

        /// <summary>
        /// Toggle the FaceCam position of all clients.
        /// (Can only be used by the server).
        /// </summary
        [ClientRpc]
        private void FaceCamOnFrontToggleClientRpc(bool networkFaceCamOnFront)
        {
#if DEBUG
            Debug.Log($"[RPC] Client {NetworkManager.Singleton.LocalClientId} received FaceCamOnFrontToggleClientRpc from server with networkFaceCamOnFront={networkFaceCamOnFront}\n");
#endif

            if (faceCamOnFront == networkFaceCamOnFront)
            {
                faceCamOnFront = !faceCamOnFront;
            }
        }

        /// <summary>
        /// Get the FaceCam status from the server to all clients.
        /// </summary
        [ServerRpc(RequireOwnership = false)]
        private void GetFaceCamStatusServerRpc(ServerRpcParams serverRpcParams = default)
        {
#if DEBUG
            Debug.Log($"[RPC] Server received GetFaceCamStatusServerRpc from {serverRpcParams.Receive.SenderClientId}\n");
#endif
            SetFaceCamStatusClientRpc(faceCamOn, faceCamOnFront);
        }

        /// <summary>
        /// Set the FaceCam status on all clients.
        /// (Can only be used by the server).
        /// </summary
        [ClientRpc]
        private void SetFaceCamStatusClientRpc(bool faceCamOn, bool faceCamOnFront)
        {
#if DEBUG
            Debug.Log($"[RPC] Client {NetworkManager.Singleton.LocalClientId} received SetFaceCamStatusClientRpc from server with faceCamOn={faceCamOn} and faceCamOnFront={faceCamOnFront}\n");
#endif
            this.faceCamOn = faceCamOn;
            this.faceCamOnFront = faceCamOnFront;
            // Make the FaceCam visible/invisible and/or start/stop it.
            FaceCamOnOffToggle();
        }

        /// <summary>
        /// Displays the video on any client, but not where the video is recorded.
        /// </summary>
        private void DisplayVideoOnAllOtherClients()
        {
            // A frame of the video, created from the source video already displayed on
            // this owners client.
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
                // Send the frame to all clients. (But not the server and owner, which in
                // this case, is the server.)
                SendVideoToClientsToRenderItClientRPC(videoFrame);
            }
        }

        /// <summary>
        /// This creates a frame from the video source.
        /// The frame can be send over the network and is compressed.
        /// </summary>
        private byte[] CreateNetworkFrameFromVideo()
        {
            // Converts the texture to an byte array containing an JPG.
            byte[] networkTexture = croppedTexture.EncodeToJPG();
            // Only return the array if it's not too big.
            if (networkTexture != null && networkTexture.Length <= maximumNetworkByteSize)
            {
                return networkTexture;
            }
            return null;
        }

        /// <summary>
        /// The owner calls this, to send his video to the server which sends it to all clients.
        /// Also the server and every client will render this video onto the FaceCam.
        /// </summary>
        //[ServerRpc(Delivery = RpcDelivery.Unreliable)]
        // Large files not supported by unreliable Rpc. (No documentation found regarding this limitation).
        [ServerRpc]
        private void GetVideoFromClientAndSendItToClientsToRenderItServerRPC(byte[] videoFrame, ServerRpcParams serverRpcParams = default)
        {
#if DEBUG
            Debug.Log($"[RPC] Server received GetVideoFromClientAndSendItToClientsToRenderItServerRPC from {serverRpcParams.Receive.SenderClientId}\n");
#endif

            // The server will render this video onto his instance of the FaceCam.
            RenderNetworkFrameOnFaceCam(videoFrame);

            // The server will send the video to all other clients (not the owner and server)
            // so they can render it.
            SendVideoToClientsToRenderItClientRPC(videoFrame);
        }

        /// <summary>
        /// The Server calls this, to send his video to all clients.
        /// Also every client will render this video onto the FaceCam.
        /// </summary>
        //[ClientRpc(Delivery = RpcDelivery.Unreliable)]
        // Large files not supported by unreliable Rpc. (No documentation found regarding this limitation).
        [ClientRpc]
        private void SendVideoToClientsToRenderItClientRPC(byte[] videoFrame)
        {
#if DEBUG
            Debug.Log($"[RPC] Client {NetworkManager.Singleton.LocalClientId} received SendVideoToClientsToRenderItClientRPC from server\n");
#endif
            RenderNetworkFrameOnFaceCam(videoFrame);
        }

        /// <summary>
        /// The received frame will be rendered onto the FaceCam
        /// </summary>
        private void RenderNetworkFrameOnFaceCam(byte[] videoFrame)
        {
            croppedTexture.LoadImage(videoFrame);
            mainMaterial.mainTexture = croppedTexture;
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
            faceLandmarkDetector?.Dispose();

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
        private void AddClientIdToListServerRPC(ulong clientId, ServerRpcParams serverRpcParams = default)
        {
#if DEBUG
            Debug.Log($"[RPC] Server received AddClientIdToListServerRPC from {serverRpcParams.Receive.SenderClientId} with clientId={clientId}\n");
#endif
            clientsIdsList.Add(clientId);
            // Create the RpcParams from the list to make the list usable as RpcParams.
            CreateClientRpcParams();
        }

        /// <summary>
        /// The clients call this to remove their ClientId from the list on the Server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RemoveClientFromListServerRPC(ulong clientId, ServerRpcParams serverRpcParams = default)
        {
#if DEBUG
            Debug.Log($"[RPC] Server received RemoveClientFromListServerRPC from {serverRpcParams.Receive.SenderClientId} with clientId={clientId}\n");
#endif
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

// Author of WebCamTextureToMatHelperExample.cs: Enox Software,
// enoxsoftware.com/, enox.software@gmail.com
