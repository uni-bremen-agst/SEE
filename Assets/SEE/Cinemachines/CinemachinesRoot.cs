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
        [Title("Cinemachines Root Maintenance", horizontalLine: true)]
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
        /// Root of the GameObjects carrying a Cinemachine Brain.
        /// </summary>
        private GameObject cinemachineBrainsGameObject;

        /// <summary>
        /// GameObject for the ControlCamera.
        /// </summary>
        private GameObject cinemachineControlCameraGameObject;

        /// <summary>
        /// Root of the GameObjects holding the Cinemachines scenes.
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

        /// <summary>
        /// Stores this whole root — its brains, its control camera and every scene under it —
        /// as a prefab, to be brought back later with
        /// <c>SEE > Cinemachines > Add Cinemachines Root from Backup</c>.
        /// </summary>
        /// <remarks>Backing up a single scene carries that scene to another Unity scene.
        /// This carries the whole piece of filming, which is what is wanted before the root
        /// is taken out of a Unity scene that is committed without it: the brains and the
        /// control camera are rebuilt from prefabs by <see cref="AddStructure"/>, so any
        /// tuning of them is kept by nothing else.</remarks>
        [Button("Backup Cinemachines Root", ButtonSizes.Small), RuntimeButton(CinemachinesRootMaintenance, "Backup Cinemachines Root")]
        [ButtonGroup(CinemachinesRootMaintenance)]
        [PropertyOrder(CinemachinesRootMaintenanceOrderSetupReset), RuntimeGroupOrder(CinemachinesRootMaintenanceOrderSetupReset)]
        [ShowIf(nameof(isInitialized)), RuntimeShowIf(nameof(isInitialized))]
        [Tooltip("Stores this root and everything under it as a prefab, to be brought back later from the SEE menu.")]
        internal void BackupCinemachinesRoot()
        {
            CinemachinesUtility.GenerateCinemachinesPrefabFolder(CinemachinesUtility.CinemachinesRootsName);

            string path = $"{CinemachinesUtility.CinemachinesPrefabsRoot}/"
                          + $"{CinemachinesUtility.CinemachinesRootsName}/"
                          + $"{SceneManager.GetActiveScene().name} - {gameObject.name}.prefab";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            // Taken from the root as it stands: the prefab loses these as it is written and
            // will not say afterwards which they were.
            List<string> leaving = CinemachinesUtility.ReferencesLeaving(gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path, out bool saved);
            if (!saved || prefab == null)
            {
                Debug.LogError($"Failed to store the Cinemachines root under {path}.\n", gameObject);
                return;
            }

            // Every scene in the copy owns no folder: the folders belong to the scenes this
            // was copied from, and an instance that remembered them would have Delete Scene
            // destroy the originals. See CinemachinesScene.ForgetSceneFolder.
            CinemachinesScene[] copies = prefab.GetComponentsInChildren<CinemachinesScene>(includeInactive: true);
            foreach (CinemachinesScene copy in copies)
            {
                copy.ForgetSceneFolder();
            }

            // The losses are recorded against the first scene, there being one report for
            // the whole root rather than one for each of its scenes.
            if (copies.Length > 0)
            {
                copies[0].RememberLostReferences(leaving);
            }
            else if (leaving.Count > 0)
            {
                Debug.LogWarning($"The stored root referred to {leaving.Count} object(s) of the Unity scene, "
                                 + "which a backup cannot hold:\n  " + String.Join("\n  ", leaving) + "\n", gameObject);
            }

            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log($"The Cinemachines root has been stored under {path}.\n", gameObject);
        }

        /// <summary>
        /// The Cinemachines scene stored earlier that <see cref="AddSceneFromBackup"/> is to
        /// bring back, named as the prefab holding it is named.
        /// </summary>
        [SerializeField]
        [LabelText("Backup")]
        [PropertyOrder(CinemachineSceneConfigOrderCreate + 2), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate + 2)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [ValueDropdown(nameof(Backups))]
        [Tooltip("A Cinemachines scene stored earlier with Backup Scene.")]
        private string backup = "";

        /// <summary>
        /// The names of the Cinemachines scenes stored with <c>Backup Scene</c>.
        /// </summary>
        /// <returns>The name of every prefab under the backup folder, without its extension.
        /// They are unique, the folder being written with a unique path each time.</returns>
        private static IEnumerable<string> Backups()
        {
            string folder = $"{CinemachinesUtility.CinemachinesPrefabsRoot}/{CinemachinesUtility.CinemachinesScenesName}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return Enumerable.Empty<string>();
            }
            return AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                                .Select(AssetDatabase.GUIDToAssetPath)
                                .Select(System.IO.Path.GetFileNameWithoutExtension)
                                .OrderBy(name => name);
        }

        /// <summary>
        /// Brings the Cinemachines scene named by <see cref="backup"/> back into this root,
        /// and reports the references that could not be brought back with it.
        /// </summary>
        /// <remarks>The copy is detached from the prefab, so that working on it does not
        /// alter the backup, and is given a scene folder of its own. Its timeline is the one
        /// the backup refers to; where that has been deleted meanwhile, which
        /// <c>Delete Scene</c> does, the copy arrives without a timeline and says so.</remarks>
        [Button("Add Scene from Backup", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Add Scene from Backup")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderCreate + 3), RuntimeGroupOrder(CinemachineSceneConfigOrderCreate + 3)]
        [EnableIf(nameof(isInitialized)), RuntimeEnableIf(nameof(isInitialized))]
        [Tooltip("Brings a Cinemachines scene stored earlier with Backup Scene back into this root.")]
        internal void AddSceneFromBackup()
        {
            List<string> stored = Backups().ToList();
            if (stored.Count == 0)
            {
                EditorUtility.DisplayDialog("Add Scene from Backup",
                                            "No Cinemachines scene has been backed up.\n\n"
                                            + "Press Backup Scene on a scene to store it; it is kept as a prefab "
                                            + "under " + CinemachinesUtility.CinemachinesPrefabsRoot + "/"
                                            + CinemachinesUtility.CinemachinesScenesName + " until you delete it.",
                                            "Okay");
                return;
            }
            if (String.IsNullOrWhiteSpace(backup) || !stored.Contains(backup))
            {
                EditorUtility.DisplayDialog("Add Scene from Backup",
                                            "Choose one of the stored scenes in the Backup field first.",
                                            "Okay");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOfBackup(backup));
            if (prefab == null)
            {
                Debug.LogError($"The backup {backup} could not be loaded.\n");
                return;
            }

            Transform scenes = transform.Find(CinemachinesUtility.CinemachinesScenesName);
            GameObject restored = PrefabUtility.InstantiatePrefab(prefab, scenes != null ? scenes : transform) as GameObject;
            if (restored == null)
            {
                Debug.LogError($"The backup {backup} could not be instantiated.\n");
                return;
            }
            Undo.RegisterCreatedObjectUndo(restored, "Add Scene from Backup");

            // Detached from the prefab, so that arranging the restored scene leaves the
            // backup as it was.
            PrefabUtility.UnpackPrefabInstance(restored, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            restored.name = prefab.name;
            restored.tag = Tags.EditorOnly;

            CinemachinesScene scene = restored.GetComponent<CinemachinesScene>();
            if (scene != null)
            {
                // A folder of its own, the backup owning none: see CinemachinesScene.ForgetSceneFolder.
                CreateCinemachineFolderStructure();
                CinemachinesUtility.GenerateSceneStructure(restored, restored.name);
                CinemachinesUtility.ReportLostReferences("Add Scene from Backup",
                                                         restored.name,
                                                         new[] { scene });
            }

            if (restored.TryGetComponent(out PlayableDirector director) && director.playableAsset == null)
            {
                Debug.LogWarning($"The backup {backup} has no timeline. It was deleted after the backup was "
                                 + "taken, which is what Delete Scene does to the timeline and signals of a "
                                 + "scene. The cameras and splines are here; the sequence is not.\n", restored);
            }

            Selection.activeGameObject = restored;
            Debug.Log($"Scene {restored.name} has been restored from its backup.\n", restored);
        }

        /// <summary>
        /// The asset path of the backup prefab called <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the backup, without its extension.</param>
        /// <returns>The path of the prefab.</returns>
        private static string PathOfBackup(string name)
        {
            return $"{CinemachinesUtility.CinemachinesPrefabsRoot}/"
                   + $"{CinemachinesUtility.CinemachinesScenesName}/{name}.prefab";
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
