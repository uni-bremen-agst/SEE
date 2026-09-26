using SEE.UI.RuntimeConfigMenu;
using SEE.Cinemachines.Utility;
using SEE.Cinemachines.UI.PictureInPicture;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using SEE.Game;
using SEE.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE.Cinemachines
{
    /// <summary>
    /// Cinemachines component, that initializes a folder structure inside the project, specifically for
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
        [Title("Cinemachines-Root Maintenance", horizontalLine: true)]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [LabelText("CinemachinesRoot initialized?")]
        [Tooltip("Displays the state of initialization of the CinemachinesRoot.")]
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
                // One of them is to survive, and each of them runs this. Were every
                // one to disable itself, a scene with two roots would end with none
                // working at all. The instance ID settles which stays, being the same
                // answer whichever root asks and whatever order the search returns.
                CinemachinesRoot survivor = possibleRoots.OrderBy(root => root.GetInstanceID()).First();

                if (survivor != this)
                {
                    Debug.LogError("Multiple CinemachinesRoot are not supported. Use only one per "
                                   + $"Unity scene. Disabling this one in favour of {survivor.name}.\n",
                                   gameObject);

                    // Disable GameObject
                    gameObject.SetActive(false);
                    enabled = false;

                    return;
                }
            }

            // If the CinemachinesRoot has not been initialized on Start, initialize it.
            if (!isInitialized)
            {
                AddStructure();
            }
        }

        #region Root Maintenance

        /// <summary>
        /// Builds the structure of this CinemachinesRoot: the Cinemachine brains, the
        /// control camera, the folder the scenes live in, and the render textures the
        /// brains draw into. It does not add a root; the root is this component.
        /// </summary>
        [Button("Add Structure", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Add Structure")]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [HideIf(nameof(isInitialized)), RuntimeHideIf(nameof(isInitialized))]
        [Tooltip("Builds the structure this root needs: the Cinemachine brains, the control camera, the scenes folder and the render textures.")]
        internal void AddStructure()
        {
            // Create the structure of the CinemachinesRoot. It fails, if the prefabs are not available.
            if (!CreateCinemachinesRootStructure())
            {
                return;
            }

            // Build the folder structure under "Assets/Cinemachine".
            CreateCinemachineFolderStructure();

            // Either create or confirm existence of the RenderTextures.
            CreateRenderTextures();

            isInitialized = true;

            // Make sure, that this object doesn't get put into a build.
            tag = Tags.EditorOnly;
        }

        /// <summary>
        /// Resets the CinemachinesRoot.
        /// </summary>
        [Button("Reset Cinemachines", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Reset Cinemachines")]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [Tooltip("Resets the root for the Cinemachines. This will also remove any created scenes.")]
        [ShowIf(nameof(isInitialized)), RuntimeShowIf(nameof(isInitialized))]
        internal void ResetCinemachinesRoot()
        {
            // Reset initialization in case the root could not be reset.
            isInitialized = false;

            // Clear children of CinemachinesRoot
            List<Transform> rootChildren = new();

            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesBrainsName));
            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesScenesName));
            rootChildren.Add(transform.Find(CinemachinesUtility.CinemachinesControlCameraName));

            foreach (Transform child in rootChildren.Where(c => c != null))
            {
                // The whole of this class is compiled for the editor only, so
                // immediate destruction is the only case there is to handle.
                Debug.Log("Immediate destroying Cinemachine children within editor\n", child.gameObject);
                Destroyer.Destroy(child.gameObject);
            }

            // Remove every scene folder from Assets/Cinemachines/Scenes
            string[] sceneFolders = AssetDatabase.GetSubFolders
                                          ($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}");
            foreach (string sceneFolder in sceneFolders)
            {
                if (sceneFolder != $"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}/general")
                {
                    Debug.Log($"Removing {sceneFolder} from project.\n");
                    AssetDatabase.DeleteAsset(sceneFolder);
                }
            }

            sceneCounter = 0;

            AddStructure();
        }

        #endregion Root Maintenance

        #region Scene Creation

        /// <summary>
        /// Text field for adding a suffix to a scene name.
        /// </summary>
        [SerializeField]
        [Title("Scene Creation", horizontalLine: true)]
        [LabelText("Scene Name")]
        [PropertyOrder(CinemachineSceneConfigOrderCreate), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [Tooltip("Name of the scene to be added as a suffix to the GameObject.")]
        private string sceneNameSuffix = "";

        /// <summary>
        /// Creates a new Cinemachine scene structure inside the <see cref="CinemachinesRoot">.
        /// </summary>
        [Button("Add Scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Add Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderCreate + 1), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate + 1)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [Tooltip("Creates a new Cinemachine scene structure inside the Unity scene.")]
        internal void AddScene()
        {
            // find the Scenes Transform within the CinemachinesRoot-Prefab
            Transform? scenesTransform = transform.Find("Scenes");

            // Generate the Scene Name with the sceneCounter and the optional SceneNameSuffix
            string sceneName = $"Scene{sceneCounter}";
            if (!String.IsNullOrWhiteSpace(sceneNameSuffix))
            {
                sceneName += $" - {sceneNameSuffix}";
            }
            sceneCounter += 1;

            // Clear text input.
            sceneNameSuffix = "";

            // Create Prefab inside Cinemachines -> Scenes.
            GameObject newScene = new(sceneName, typeof(CinemachinesScene));

            // Check whether the scenes GameObject exists.
            if (scenesTransform)
            {
                newScene.transform.SetParent(scenesTransform);
            }
            else
            {
                newScene.transform.SetParent(transform);

                Debug.LogWarning("Missing structure. Reset CinemachinesRoot to repair.\n");
            }

            // Confirm, that the underlining structure exists.
            CreateCinemachineFolderStructure();

            // Setup scene structure.
            CinemachinesUtility.GenerateSceneStructure(newScene, sceneName);

            // Create new Timeline asset and store it in the newly created scenes folder.
            TimelineAsset newTimeline = ScriptableObject.CreateInstance<TimelineAsset>();

            string scenePath = AssetDatabase.GUIDToAssetPath(newScene.GetComponent<CinemachinesScene>().SceneGUID);

            Debug.Log($"Creating Timeline asset in: \"{scenePath}\"\n");
            AssetDatabase.CreateAsset(newTimeline, $"{scenePath}/Timeline.playable");

            // Assign to SceneRoot -> Playable Director
            newScene.GetComponent<PlayableDirector>().playableAsset = newTimeline;

            // Open Timeline window with the currently selected scene.
            newScene.GetComponent<CinemachinesScene>().OpenTimelineWindow();
        }

        #endregion Scene Creation

        #region Helper-Functions

        /// <summary>
        /// Checks for missing prefabs and generates the CinemachinesRoot structure.
        /// </summary>
        /// <returns> True if creation of the structure was successful, false otherwise. </returns>
        private bool CreateCinemachinesRootStructure()
        {
            // Pre-load any of the required sub-prefabs.
            GameObject brains = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesBrainsName}");
            GameObject controlCamera = Resources.Load<GameObject>($"{CinemachinesUtility.CinemachinesRootPrefabsRoot}/{CinemachinesUtility.CinemachinesControlCameraName}");

            // Check for missing prefabs.
            if (!brains || !controlCamera)
            {
                Debug.LogError("Unable to reconstruct the CinemachinesRoot. Missing prefabs.\n");

                // Log which prefabs are missing.
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

            // Create GameObject structure under CincemachinesRoot.
            cinemachineBrainsGameObject = Instantiate(brains, transform, false);
            cinemachineControlCameraGameObject = Instantiate(controlCamera, transform, false);

            cinemachineScenesGameObject = new GameObject(CinemachinesUtility.CinemachinesScenesName);
            cinemachineScenesGameObject.transform.SetParent(transform);

            // Correct their names, so that they don't include the "(Clone)" suffix.
            cinemachineBrainsGameObject.name  = $"{CinemachinesUtility.CinemachinesBrainsName}";
            cinemachineControlCameraGameObject.name = $"{CinemachinesUtility.CinemachinesControlCameraName}";

            // Don't save these GameObjects into the build by marking them as EditorOnly.
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
            // Create new folder for scene in Assets/Cinemachines/Scenes
            if (!AssetDatabase.IsValidFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}"))
            {
                // Mind that IsValidFolder wants a path from the project root, that is,
                // one beginning with "Assets", and that CreateFolder does not fail on a
                // name already taken: it makes a unique one beside it. A check asking
                // the wrong question therefore leaves a "Cinemachines 1" behind on every
                // call rather than doing nothing.
                if (!AssetDatabase.IsValidFolder(CinemachinesUtility.CinemachinesAssetsRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "Cinemachines");
                }

                if (!AssetDatabase.IsValidFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes"))
                {
                    AssetDatabase.CreateFolder(CinemachinesUtility.CinemachinesAssetsRoot, "Scenes");
                }

                AssetDatabase.CreateFolder($"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes", $"{SceneManager.GetActiveScene().name}");
                Debug.Log($"Created scenes root folder in \"{CinemachinesUtility.CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}\"\n");
            }
        }

        /// <summary>
        /// Helper function to create the required RenderTextures and storing them as Assets.
        /// If these RenderTextures already exist, they will be overwritten
        /// </summary>
        private void CreateRenderTextures()
        {
            // Create the RenderTextureDescriptor that both RenderTextures should abide by.
            RenderTextureDescriptor renderTextureDescriptor = new(1920, 1080, RenderTextureFormat.ARGB32);
            renderTextureDescriptor.depthStencilFormat = GraphicsFormat.D16_UNorm;

            // Create the RenderTexture for main Cinemachines output, if none exists.
            string pathCinemachineMain = $"{CinemachinesUtility.CinemachinesAssetsRoot}/{CinemachinesUtility.CinemachinesMainOutputName}";
            if (!AssetDatabase.AssetPathExists(pathCinemachineMain))
            {
                AssetDatabase.CreateAsset(new RenderTexture(renderTextureDescriptor), pathCinemachineMain);
            }
            mainOutputGUID = AssetDatabase.GUIDFromAssetPath(pathCinemachineMain);

            // Create the RenderTexture for PIP Cinemachines output, if none exists.
            string pathCinemachinePIP = $"{CinemachinesUtility.CinemachinesAssetsRoot}/{CinemachinesUtility.CinemachinesPIPOutputName}";
            if (!AssetDatabase.AssetPathExists(pathCinemachinePIP))
            {
                AssetDatabase.CreateAsset(new RenderTexture(renderTextureDescriptor), pathCinemachinePIP);
            }
            pictureInPictureGUID = AssetDatabase.GUIDFromAssetPath(pathCinemachinePIP);

            // Mind that Resources.Load wants a path without a file extension; one
            // carrying ".asset" loads nothing and the guard below would then pass the
            // failure off as an absent asset.
            PIPDataSource controlDataSource = Resources.Load<PIPDataSource>("UI/Cinemachines/ControlCameraDataSource");
            if (controlDataSource != null)
            {
                controlDataSource.PIPImage = AssetDatabase.LoadAssetByGUID<RenderTexture>(mainOutputGUID);
            }
            else
            {
                Debug.LogWarning("Missing UI/Cinemachines/ControlCameraDataSource. The control camera will show no picture.\n");
            }

            // Find the Cinemachine brains in the child GameObjects.
            foreach (Transform child in cinemachineBrainsGameObject.transform)
            {
                switch (child.name)
                {
                    case "CMC_MainPicture":
                        Debug.Log("Found main CinemachineBrain.\n");
                        child.GetComponent<Camera>().targetTexture = AssetDatabase.LoadAssetByGUID<RenderTexture>(mainOutputGUID);
                        break;
                    case "CMC_PictureInPicture":
                        Debug.Log("Found PIP CinemachineBrain.\n");
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

        /// <summary>
        /// Button group name for the maintenance of the CinemachinesRoot. This ensures that the setup and
        /// reset buttons are displayed in the correct order in the Odin Inspector.
        /// </summary>
        protected const string CinemachinesRootMaintenance = "CinemachinesRootMaintenance";

        /// <summary>
        /// Property order for the maintenance of the CinemachinesRoot. This ensures that the setup and
        /// reset buttons are displayed in the correct order in the Odin Inspector.
        /// </summary>
        protected const int CinemachinesRootMaintenanceOrderSetupReset = 0;

        #endregion Maintenance of CinemachinesRoot

        #region Scene Creation

        /// <summary>
        /// Button group name for the scene creation configuration.
        /// </summary>
        protected const string CinemachineSceneConfig = "CinemachineSceneConfig";

        /// <summary>
        /// Property order for the scene creation configuration. This ensures that the scene name input field and
        /// create button are displayed in the correct order in the Odin Inspector.
        /// </summary>
        protected const int CinemachineSceneConfigOrderCreate = 10;

        #endregion Scene Creation

        #endregion Odin Inspector Attributes

        #endif
    }
}
