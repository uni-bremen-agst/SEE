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

// Only use UnityEditor-Namespaces when inside the Unity-Editor
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
        // Encasing Class content inside the UNITY_EDITOR directive to ensure, that these Components only activly work inside the Unity-Editor
        #if UNITY_EDITOR

        /// <summary>
        /// Boolean value to keep track, if the Root of the Cinemachines is fully initialized.
        /// </summary>
        [SerializeField, DisableInPlayMode, DisableInEditorMode]
        [Title("Cinemachines-Root Mainenance", horizontalLine: true)]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [LabelText("CinemachinesRoot initialized?")]
        [Tooltip("Displays the State of Initialization of the CinemachinesRoot.")]
        private bool isInitialized = false;

        /// <summary>
        /// Amount of Scenes inside this CinemachinesRoot. Doesn't decrement on scene-deletion to prevent duplicates.
        /// </summary>
        [HideInInspector, SerializeField]
        private int sceneCounter;

        /// <summary>
        /// The GUID of the RenderTexture assign with capturing the Cinemachine-Output.
        /// </summary>
        [HideInInspector, SerializeField]
        private GUID mainOutputGUID;

        /// <summary>
        /// The GUID of the RenderTexture associated with the Picture-In-Picture Option.
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
        /// Function to be run once on creation/startup of the Component.
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
        /// Sets-up the CinemachinesRoot Prefab.
        /// </summary>
        [Button("Setup Cinemachines-Root", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Setup Cinemachines-Root")]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [HideIf(nameof(isInitialized)), RuntimeHideIf(nameof(isInitialized))]
        [Tooltip("Sets up the Root for the Cinemachines. Generates the Structure for crucial Elements and Organization.")]
        internal void SetupCinemachinesRoot()
        {
            // Create the Structure of the CinemachinesRoot. It fails, if the Prefabs are not awailable.
            if (!CreateCinemachinesRootStructure())
            {
                return;
            }

            // build the folder structure under "Assets/Cinemachine"
            CreateCinemachineFolderStructure();

            // Either Create or Confirm existence of the RenderTextures.
            CreateRenderTextures();

            isInitialized = true;

            // Make sure, that this Object doesn't get put into a build
            tag = "EditorOnly";
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
            string[] sceneFolders = AssetDatabase.GetSubFolders($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}");
            foreach (string sceneFolder in sceneFolders)
            {
                if (sceneFolder == $"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}/general")
                {
                    continue;
                }

                Debug.Log($"Removing {sceneFolder} from Project\n");
                AssetDatabase.DeleteAsset(sceneFolder);
            }

            // Reset Scene Counter
            sceneCounter = 0;

            // Create CinemachinesRoot
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

                Debug.LogWarning("Missing Structure. Reset CinemachinesRoot to repair.");
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
        /// Checks for missing Prefabs and generates the CinemachinesRoot Structure.
        /// </summary>
        /// <returns> True, if creation of the Structure was successful, false otherwise. </returns>
        private bool CreateCinemachinesRootStructure()
        {
            // Pre-Load any of the required sub-Prefabs
            GameObject brains = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesBrainsName}");
            GameObject controlCamera = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesControlCameraName}");

            // check for missing Prefabs
            if (!brains || !controlCamera)
            {
                // report with error
                Debug.LogError("Unable to reconstruct the CinemachinesRoot. Missing Prefabs.");

                // Log, which Prefabs are missing
                if (!brains)
                {
                    Debug.LogError($"Missing {CinemachinesUtility.CinemachinesBrainsName} Prefab");
                }

                if (!controlCamera)
                {
                    Debug.LogError($"Missing {CinemachinesUtility.CinemachinesControlCameraName} Prefab");
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
            cinemachineBrainsGameObject.tag = "EditorOnly";

            cinemachineScenesGameObject.tag = "EditorOnly";

            cinemachineControlCameraGameObject.tag = "EditorOnly";

            return true;
        }

        /// <summary>
        /// Checks, if the required Folder-Structure exists.
        /// If not, it will create the Folder Structure.
        /// </summary>
        private void CreateCinemachineFolderStructure()
        {
            // create new Folder for Scene in Assets/Cinemachines/Scenes
            if (!AssetDatabase.IsValidFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}"))
            {
                if (!AssetDatabase.IsValidFolder("Cinemachines"))
                {
                    AssetDatabase.CreateFolder("Assets", "Cinemachines");
                }

                if (!AssetDatabase.IsValidFolder($"CinemachinesUtility.CinemachinesAssetsRoot"))
                {
                    AssetDatabase.CreateFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}", "Scenes");
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

            PIPDataSource controlDataSource = Resources.Load<PIPDataSource>("UI/Cinemachines/ControlCameraDataSource.asset");
            if (controlDataSource != null)
            {
                controlDataSource.PIPImage = AssetDatabase.LoadAssetByGUID<RenderTexture>(mainOutputGUID);
            }

            // Find the Cinemachine-Brains in the Child-GameObjects
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
