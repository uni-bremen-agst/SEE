using SEE.UI.RuntimeConfigMenu;
using SEE.Cinemachines.Utility;
using SEE.Cinemachines.UI.PictureInPicture;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using SEE.Game;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE.Cinemachines
{
    /// <summary>
    /// Cinemachines Component, that initializes a Folder Structure inside the Project, specifically for
    /// Cinemachine and all of its associated components and elements.
    /// </summary>
    [Serializable]
    [ExecuteInEditMode]
    internal class CinemachinesRoot : SerializedMonoBehaviour
    {
        #if UNITY_EDITOR

        /// <summary>
        /// True if the root of the Cinemachines is fully initialized.
        /// </summary>
        [SerializeField, DisableInPlayMode, DisableInEditorMode]
        [Title("Cinemachines-Root Mainenance", horizontalLine: true)]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [LabelText("CinemachinesRoot initialized?")]
        [Tooltip("Displays the State of Initialization of the CinemachinesRoot.")]
        private bool isInitialized = false;

        /// <summary>
        /// Number of scenes inside this CinemachinesRoot. Doesn't decrement on scene deletion to prevent duplicates.
        /// </summary>
        [HideInInspector, SerializeField]
        private int sceneCounter;

        /// <summary>
        /// The GUID of the RenderTexture assign with capturing the Cinemachine output.
        /// </summary>
        [HideInInspector, SerializeField]
        private GUID mainOutputGUID;

        /// <summary>
        /// The GUID of the RenderTexture associated with the Picture-In-Picture option.
        /// </summary>
        [HideInInspector, SerializeField]
        private GUID pictureInPictureGUID;

        /// <summary>
        /// Root of the Cinemachine-Brains GameObjects.
        /// </summary>
        private GameObject cinemachineBrainsGameObject;

        /// <summary>
        /// GameObject for the ControlCamera.
        /// </summary>
        private GameObject cinemachineControlCameraGameObject;

        /// <summary>
        /// Root of the Cinemachine-Scene GameObjects.
        /// </summary>
        private GameObject cinemachineScenesGameObject;

        /// <summary>
        /// Ensures that only one CinemachinesRoot exists per scene and
        /// initializes the CinemachinesRoot if it has not been initialized yet.
        /// </summary>
        protected void Start()
        {
            // Ensure, that only one CinemachinesRoot exists per scene
            CinemachinesRoot[] possibleRoots = CinemachinesUtility.FindAllCinemachinesRootsInScene();

            if (possibleRoots.Length > 1)
            {
                Debug.LogError("Multiple CinemachinesRoot are not supported. Only use one per Unity-Scene.\n");

                // Disable GameObject
                gameObject.SetActive(false);
                enabled = false;

                return;
            }

            // If the CinemachinesRoot has not been initialized on Start, initialize it.
            if (!isInitialized)
            {
                SetupCinemachinesRoot();
            }
        }

        #region Root Maintenance

        /// <summary>
        /// Sets up the CinemachinesRoot prefab.
        /// </summary>
        [Button("Setup Cinemachines-Root", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Setup Cinemachines-Root")]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [HideIf(nameof(isInitialized)), RuntimeHideIf(nameof(isInitialized))]
        [Tooltip("Sets up the Root for the Cinemachines. Generates the Structure for crucial Elements and Organization.")]
        internal void SetupCinemachinesRoot()
        {
            // Create the Structure of the CinemachinesRoot. It fails, if the Prefabs are not available.
            if (!CreateCinemachinesRootStructure())
            {
                return;
            }

            // build the folder structure under "Assets/Cinemachine"
            CreateCinemachineFolderStructure();

            // Either Create or Confirm existence of the RenderTextures.
            CreateRenderTextures();

            isInitialized = true;

            // Make sure, that this Object doesn't get put into a build.
            tag = Tags.EditorOnly;
        }

        /// <summary>
        /// Resets the CinemachinesRoot.
        /// </summary>
        [Button("Reset Cinemachines", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Reset Cinemachines")]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [Tooltip("Resets the Root for the Cinemachines. This will also remove any created Scenes.")]
        [ShowIf(nameof(isInitialized)), RuntimeShowIf(nameof(isInitialized))]
        internal void ResetCinemachinesRoot()
        {
            // reset initialization, in case the root could not be reset.
            isInitialized = false;

            // clear children of CinemachinesRoot
            List<Transform> rootChildren = new();

            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesBrainsName));
            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesScenesName));
            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesControlCameraName));

            foreach (Transform child in rootChildren)
            {
                if (child == null)
                {
                    continue;
                }

                #if UNITY_EDITOR
                Debug.Log("Immediate Destroying Cinemachine-Children within Editor\n", child.gameObject);
                DestroyImmediate(child.gameObject);
                #else
                Debug.Log("Destroying Cinemachine-Children during Runtime\n", child.gameObject);
                Destroyer.Destroy(child.gameObject);
                #endif
            }

            // Remove every Scene-Folder from Assets/Cinemachines/Scenes
            string[] sceneFolders = AssetDatabase.GetSubFolders
                                          ($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}");
            foreach (string sceneFolder in sceneFolders)
            {
                if (sceneFolder == $"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}/general")
                {
                    continue;
                }

                Debug.Log($"Removing {sceneFolder} from project.\n");
                AssetDatabase.DeleteAsset(sceneFolder);
            }

            sceneCounter = 0;

            SetupCinemachinesRoot();
        }

        #endregion Root Maintenance

        #region Scene Creation

        /// <summary>
        /// Text-Field for adding a Suffix to a Scene name.
        /// </summary>
        [SerializeField]
        [Title("Scene Creation", horizontalLine: true)]
        [LabelText("Scene Name")]
        [PropertyOrder(CinemachineSceneConfigOrderCreate), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [Tooltip("Name of the Scene to be added as a Suffix to the GameObject.")]
        private string SceneNameSuffix = "";

        /// <summary>
        /// Creates a new Cinemachine-Scene Structure inside the <see cref="CinemachinesRoot">.
        /// </summary>
        [Button("Create new Scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Create new Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderCreate + 1), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate + 1)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [Tooltip("Creates a new Cinemachine-Scene Structure inside the Unity-Scene.")]
        internal void CreateNewScene()
        {
            // find the Scenes Transform within the CinemachinesRoot-Prefab
            Transform? scenesTransform = transform.Find("Scenes");

            // Generate the Scene Name with the sceneCounter and the optional SceneNameSuffix
            string sceneName = $"Scene{sceneCounter}";
            if (!String.IsNullOrWhiteSpace(SceneNameSuffix))
            {
                sceneName += $" - {SceneNameSuffix}";
            }
            sceneCounter += 1;

            // Clear Text Input
            SceneNameSuffix = "";

            // Create Prefab inside Cinemachines -> Scenes
            GameObject newScene = new(sceneName, typeof(CinemachinesScene));

            // check, if the Scenes GameObject exists
            if (scenesTransform)
            {
                newScene.transform.SetParent(scenesTransform);
            }
            else
            {
                newScene.transform.SetParent(transform);

                Debug.LogWarning("Missing Structure. Reset CinemachinesRoot to repair.\n");
            }

            // confirm, that the underlining Structure exists
            CreateCinemachineFolderStructure();

            // Setup Scene Structure
            CinemachinesUtility.GenerateSceneStructure(newScene, sceneName);

            // create new Timeline-Asset and store it in the newly created Scenes-Folder
            TimelineAsset newTimeline = ScriptableObject.CreateInstance<TimelineAsset>();

            string scenePath = AssetDatabase.GUIDToAssetPath(newScene.GetComponent<CinemachinesScene>().SceneGUID);

            Debug.Log($"Creating TimelineAsset in: \"{scenePath}\"\n");
            AssetDatabase.CreateAsset(newTimeline, $"{scenePath}/Timeline.playable");

            // assign to SceneRoot -> Playable Director
            newScene.GetComponent<PlayableDirector>().playableAsset = newTimeline;

            // Open Timeline Window with the current Scene Selected
            newScene.GetComponent<CinemachinesScene>().OpenTimelineWindow();
        }

        #endregion Scene Creation

        #region Helper-Functions

        /// <summary>
        /// Checks for missing prefabs and generates the CinemachinesRoot structure.
        /// </summary>
        /// <returns> True, if creation of the structure was successful, false otherwise. </returns>
        private bool CreateCinemachinesRootStructure()
        {
            // Pre-load any of the required sub-prefabs
            GameObject brains = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesBrainsName}");
            GameObject controlCamera = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesControlCameraName}");

            // check for missing prefabs
            if (!brains || !controlCamera)
            {
                // report with error
                Debug.LogError("Unable to reconstruct the CinemachinesRoot. Missing prefabs.\n");

                // Log, which Prefabs are missing
                if (!brains)
                {
                    Debug.LogError($"Missing {CinemachinesUtility.CinemachinesBrainsName} prefab.\n");
                }

                if (!controlCamera)
                {
                    Debug.LogError($"Missing {CinemachinesUtility.CinemachinesControlCameraName} prefab.\n");
                }

                return false;
            }

            // Create GameObject Structure under CincemachinesRoot
            cinemachineBrainsGameObject = Instantiate(brains, transform, false);
            cinemachineControlCameraGameObject = Instantiate(controlCamera, transform, false);

            cinemachineScenesGameObject = new GameObject(CinemachinesUtility.CinemachinesScenesName);
            cinemachineScenesGameObject.transform.SetParent(transform);

            // Correct their Names, so that they don't include the "(Clone)" suffix
            cinemachineBrainsGameObject.name  = $"{CinemachinesUtility.CinemachinesBrainsName}";
            cinemachineControlCameraGameObject.name = $"{CinemachinesUtility.CinemachinesControlCameraName}";

            // Dont Save these GameObjects into the Build by marking these as EditorOnly
            cinemachineBrainsGameObject.tag = Tags.EditorOnly;

            cinemachineScenesGameObject.tag = Tags.EditorOnly;

            cinemachineControlCameraGameObject.tag = Tags.EditorOnly;
            return true;
        }

        /// <summary>
        /// Checks whether the required folder structure exists.
        /// If not, it will create the folder structure.
        /// </summary>
        private void CreateCinemachineFolderStructure()
        {
            // create new Folder for Scene in Assets/Cinemachines/Scenes
            if (!AssetDatabase.IsValidFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}"))
            {
                // Mind that IsValidFolder wants a path from the root of the project,
                // that is, one beginning with "Assets", and that CreateFolder does not
                // fail on a name already taken: it makes a unique one beside it. So a
                // check asking the wrong question leaves a "Cinemachines 1" behind on
                // every call rather than quietly doing nothing.
                if (!AssetDatabase.IsValidFolder(CinemachinesUtility.CinemachinesAssetsRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "Cinemachines");
                }

                // Two faults in the one line. The interpolation had no braces, so this
                // asked after a folder named literally for the constant; and the folder
                // it meant to ask after is the Scenes folder that the body creates.
                if (!AssetDatabase.IsValidFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes"))
                {
                    AssetDatabase.CreateFolder(CinemachinesUtility.CinemachinesAssetsRoot, "Scenes");
                }

                AssetDatabase.CreateFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes", $"{SceneManager.GetActiveScene().name}");
                Debug.Log($"Created Scenes-Root Folder in \"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}\"\n");
            }
        }

        /// <summary>
        /// Helper Function to create the required RenderTextures and storing them as Assets.
        /// If these RenderTextures already exist, they will be overwritten
        /// </summary>
        private void CreateRenderTextures()
        {
            // Create the RenderTextureDescriptor, that both RenderTextures should abide by
            RenderTextureDescriptor renderTextureDescriptor = new(1920, 1080, RenderTextureFormat.ARGB32);
            renderTextureDescriptor.depthStencilFormat = GraphicsFormat.D16_UNorm;

            // Create the RenderTexture for Main Cinemachines-Output, if none exists
            string pathCinemachineMain = $"{CinemachinesUtility.CinemachinesAssetsRoot}/{CinemachinesUtility.CinemachinesMainOutputName}";
            if (!AssetDatabase.AssetPathExists(pathCinemachineMain))
            {
                AssetDatabase.CreateAsset(new RenderTexture(renderTextureDescriptor), pathCinemachineMain);
            }
            mainOutputGUID = AssetDatabase.GUIDFromAssetPath(pathCinemachineMain);

            // Create the RenderTexture for PIP Cinemachines-Output, if none exists
            string pathCinemachinePIP = $"{CinemachinesUtility.CinemachinesAssetsRoot}/{CinemachinesUtility.CinemachinesPIPOutputName}";
            if (!AssetDatabase.AssetPathExists(pathCinemachinePIP))
            {
                AssetDatabase.CreateAsset(new RenderTexture(renderTextureDescriptor), pathCinemachinePIP);
            }
            pictureInPictureGUID = AssetDatabase.GUIDFromAssetPath(pathCinemachinePIP);

            // Mind that Resources.Load wants a path without a file extension; one
            // carrying ".asset" loads nothing at all, and the guard below would then
            // pass that failure off as an absent asset.
            PIPDataSource controlDataSource = Resources.Load<PIPDataSource>("UI/Cinemachines/ControlCameraDataSource");
            if (controlDataSource != null)
            {
                controlDataSource.PIPImage = AssetDatabase.LoadAssetByGUID<RenderTexture>(mainOutputGUID);
            }
            else
            {
                Debug.LogWarning("Missing UI/Cinemachines/ControlCameraDataSource. "
                                 + "The control camera will show no picture.\n");
            }

            // Find the Cinemachine brains in the child GameObjects.
            foreach (Transform child in cinemachineBrainsGameObject.transform)
            {
                switch (child.name)
                {
                    case "CMC_MainPicture":
                        Debug.Log("Found Main Cinemachine Brain.\n");
                        child.GetComponent<Camera>().targetTexture = AssetDatabase.LoadAssetByGUID<RenderTexture>(mainOutputGUID);
                        break;
                    case "CMC_PictureInPicture":
                        Debug.Log("Found PIP Cinemachine Brain.\n");
                        child.GetComponent<Camera>().targetTexture = AssetDatabase.LoadAssetByGUID<RenderTexture>(pictureInPictureGUID);
                        break;
                    default:
                        Debug.LogWarning("Failed to find CinemachineBrains.\n");
                        break;
                }
            }
        }

        #endregion

        #region Odin Inspector Attributes

        #region Maintenance of CinemachinesRoot

        protected const string CinemachinesRootMaintenance = "CinemachinesRootMaintenance";

        protected const int CinemachinesRootMaintenanceOrderSetupReset = 0;

        #endregion Maintenance of CinemachinesRoot

        #region Scene Creation

        protected const string CinemachineSceneConfig = "CinemachineSceneConfig";

        protected const int CinemachineSceneConfigOrderCreate = 10;

        #endregion Scene Creation

        #endregion Odin Inspector Attributes

        #endif
    }
}
