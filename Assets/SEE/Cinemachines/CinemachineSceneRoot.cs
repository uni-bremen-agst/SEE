using SEE.UI.RuntimeConfigMenu;
using SEE.Cinemachines.Utility;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEngine.Splines;
using UnityEditor.Splines;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Unity.Properties;
using Unity.Cinemachine;
using Debug = UnityEngine.Debug;

namespace SEE.Cinemachines
{
    /// <summary>
    /// Scene Component, that controls and handles GameObjects and Assets specific to a Cinemachines-Scene.
    /// </summary>
    [Serializable]
    [ExecuteInEditMode]
    [RequireComponent(typeof(PlayableDirector))]
    [RequireComponent(typeof(SignalReceiver))]
    internal class CinemachinesScene : SerializedMonoBehaviour
    {
        /// <summary>
        /// Incremental Counter for Cinemachines Camera. Counter not decremented on Camera deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int cinemachinesCameraCount;

        /// <summary>
        /// Incremental Counter for Splines. Counter not decremented on Spline deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int splineCount;

        /// <summary>
        /// Incremental Counter for Signals. Counter not decremented on Signal deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int signalCount;

        /// <summary>
        /// Incremental Counter for Focus Objects. Counter not decremented on Object deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int focusObjectCount;

        /// <summary>
        /// The GameObject, that points to the Cinemachines-Cameras Root of the Scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesCamerasGameObject;

        /// <summary>
        /// The GameObject, that points to the Splines Root of the Scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesSplinesGameObject;

        /// <summary>
        /// The GameObject, that points to the Focus-Object Root of the Scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesFocusObjectGameObject;

        /// <summary>
        /// The GameObject, that points to the Miscellaneous root of the Scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesOtherObjectGameObject;

        /// <summary>
        /// The Start Function to the Component, that initializes every Variable for the Scenes.
        /// </summary>
        protected void Start()
        {
            // Find relevant GameObject and remember them
            cinemachinesCamerasGameObject = transform.Find("Cameras")?.gameObject;
            cinemachinesSplinesGameObject = transform.Find("Splines")?.gameObject;
            cinemachinesFocusObjectGameObject = transform.Find("FocusObjects")?.gameObject;
            cinemachinesOtherObjectGameObject = transform.Find("OtherObjects")?.gameObject;

            // create gameobjects, if they are not found
            if (!cinemachinesCamerasGameObject || !cinemachinesSplinesGameObject || !cinemachinesFocusObjectGameObject || !cinemachinesOtherObjectGameObject)
            {
                if (!cinemachinesCamerasGameObject)
                {
                    cinemachinesCamerasGameObject = new GameObject("Cameras");
                    cinemachinesCamerasGameObject.transform.SetParent(transform);
                }

                if (!cinemachinesSplinesGameObject)
                {
                    cinemachinesSplinesGameObject = new GameObject("Splines");
                    cinemachinesSplinesGameObject.transform.SetParent(transform);
                }

                if (!cinemachinesFocusObjectGameObject)
                {
                    cinemachinesFocusObjectGameObject = new GameObject("FocusObjects");
                    cinemachinesFocusObjectGameObject.transform.SetParent(transform);
                }

                if (!cinemachinesOtherObjectGameObject)
                {
                    cinemachinesOtherObjectGameObject = new GameObject("OtherObjects");
                    cinemachinesOtherObjectGameObject.transform.SetParent(transform);
                }
            }
        }

        /// <summary>
        /// The Updater Function, that is unused in this Component.
        /// </summary>
        protected void Update()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// AssetGUID of the Folder associated with this Scene. (Field).
        /// </summary>
        [SerializeField, DisableInPlayMode, DisableInEditorMode]
        [Title("Scene Maintenance", horizontalLine: true)]
        [PropertyOrder(CinemachineSceneConfigOrderDeletion), RuntimeGroupOrder(CinemachineSceneConfigOrderDeletion)]
        [LabelText("GUID of the Scene")]
        private string sceneGUID = "";

        /// <summary>
        /// AssetGUID of the Folder associated with this Scene. (Property).
        /// </summary>
        public string SceneGUID
        {
            get
            {
                return sceneGUID;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(sceneGUID))
                {
                    sceneGUID = value;
                }
            }
        }

        /// <summary>
        /// Deletes this CinemachineScene from the Scenes.
        /// </summary>
        [Button("Delete selected Scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Delete Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderDeletion), RuntimeGroupOrder(CinemachineSceneConfigOrderDeletion)]
        [InfoBox("@CinemachinesUtility.GetSceneDeletionWarningMessage(SceneGUID)", InfoMessageType.Warning)]
        [Tooltip("Removes the currently selected Cinemachine Scene and its associated Timeline and other Assets.")]
        internal void DestroyObject()
        {
            // Confirm, if the user wants to delete the Scene, permanently
            if (!EditorUtility.DisplayDialog("Deletion Confirmation", "Are you sure, you want to remove this Scene?\n This will also permanently remove any associated files?", "Yes, delete", "No, keep Scene"))
            {
                return;
            }

            // Delete Children before destroying the Object
            foreach (Transform Child in transform)
            {
                #if UNITY_EDITOR
                Debug.Log("Immediate Destroying Scene-Children within Editor", Child.gameObject);
                DestroyImmediate(Child.gameObject);
                #else
                Debug.Log("Destroying Scene-Children during Runtime", Child.gameObject);
                Destroy(child.gameObject);
                #endif
            }

            // Remove SceneFolder inside Assets/Cinemachines/Scenes, if one is assigned to this scene
            if (!String.IsNullOrWhiteSpace(SceneGUID))
            {
                string PathToSceneFolder = AssetDatabase.GUIDToAssetPath(SceneGUID);
                Debug.Log($"Attempting to remove associated Scenes Folder from Project. Path: {PathToSceneFolder}", this);
                if (!String.IsNullOrWhiteSpace(PathToSceneFolder))
                {
                    AssetDatabase.DeleteAsset(PathToSceneFolder);
                }
                else
                {
                    Debug.LogWarning("Failed to find Scene-Folder. Assuming it never existed.", this);
                }
            }
            else
            {
                Debug.LogWarning("GUID of Scene-Folder not set. Assuming it never existed.", this);
            }

            // Remove self
            #if UNITY_EDITOR
            Debug.Log("Immediate Destroying Scene-Root from Editor", transform.gameObject);
            DestroyImmediate(transform.gameObject);
            #else
            Debug.Log("Destroying Scene-Root during Runtime", transform.gameObject);
            Destroy(transform.gameObject);
            #endif
        }

        /// <summary>
        /// Stores this CinemachineScene as a Prefab.
        /// </summary>
        [Button("Backup Scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Backup Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderStore), RuntimeGroupOrder(CinemachineSceneConfigOrderStore)]
        // [InfoBox("@CinemachinesUtility.GetSceneDeletionWarningMessage(SceneGUID)", InfoMessageType.Warning)]
        [Tooltip("Stores the current Scene as a Prefab for loading in a different Unity-Scene or in the same in a different spot. This does not carry over References specific to a Scene.")]
        internal void SaveScene()
        {
            // Generate the Prefabs Structure, if it doesn't exist yet
            CinemachinesUtility.GenerateCinemachinesPrefabFolder();

            // Full Path to the to be created Prefab Asset
            string assetPathOfScene = $"{CinemachinesUtility.CinemachinesPrefabsRoot}/Scenes/{SceneManager.GetActiveScene().name} - {gameObject.name}.prefab";

            // Ensuring, that the path is unique
            assetPathOfScene = AssetDatabase.GenerateUniqueAssetPath(assetPathOfScene);

            // Attempt to create the Prefab
            bool prefabCreationSuccess;
            PrefabUtility.SaveAsPrefabAsset(gameObject, assetPathOfScene, out prefabCreationSuccess);

            // Log result
            if (prefabCreationSuccess)
                Debug.Log($"Scene has been successfully stored under {assetPathOfScene}");
            else
                Debug.LogError($"Failed to store Scene under {assetPathOfScene}");
        }

        /// <summary>
        /// Text-Field for a Name Suffix, to be appended to the Objects Name after creation.
        /// </summary>
        [Title("Scene Object Creation", horizontalLine: true)]
        [LabelText("Suffix for Object")]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSpline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSpline)]
        [Tooltip("Name to be added as a Suffix to the Spline or Cinemachines Camera.")]
        public string ObjectNameSuffix = "";

        /// <summary>
        /// Creates a new Spline, that can be assigned inside Cinemachine-Cameras with SplineDolly-Component.
        /// </summary>
        [Button("Create Spline", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Spline")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSpline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSpline)]
        [Tooltip("Creates a GameObject, including an empty SplineContainer-Component. Note that the Positions inside the SplineContainer are relative to the root of the GameObject and its always placed at Scene Origin.")]
        internal void CreateEmptySpline()
        {
            CinemachinesUtility.CreateGameObject("CinemachinesSpline", ref splineCount, ref ObjectNameSuffix, cinemachinesSplinesGameObject, typeof(SplineContainer), true);

            // Set current Active ToolContext to Spline
            EditorApplication.delayCall += () =>
            {
                EditorSplineUtility.SetKnotPlacementTool();
            };
        }

        /// <summary>
        /// Creates a Signal inside the Scene-Folder, which can be used on the Scenes Timeline to trigger or invoke Functions of certain Objects or Scripts.
        /// </summary>
        [Button("Create Signal", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Signal")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSignal), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSignal)]
        [Tooltip("Creates a Signal inside the current Scenes Folder. This Signal then can be used on the current Scenes Timeline for triggering or accessing specific Functions")]
        internal void CreateNewSignal()
        {
            string signalName = $"{transform.name} - {CinemachinesUtility.GetNewObjectName("Signal", ref signalCount, ref ObjectNameSuffix)}";

            // Create a new Signal, and store it under this Scenes Signals Folder
            SignalAsset newSignal = ScriptableObject.CreateInstance<SignalAsset>();

            string scenePath = AssetDatabase.GUIDToAssetPath(SceneGUID);

            Debug.Log($"Creating SignalsAsset in: \"{scenePath}/Signals\"");
            AssetDatabase.CreateAsset(newSignal, $"{scenePath}/Signals/{signalName}.signal");
        }

        /// <summary>
        /// Creates a new Cinemachines Camera, that can be assigned to a Timeline.
        /// </summary>
        [Button("Add Camera", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Cinemachine-Camera")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateCamera), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateCamera)]
        [Tooltip("Creates a GameObject, including the Cinemachines Camera Component.")]
        internal void CreateNewCamera()
        {
            CinemachinesUtility.CreateGameObject("CinemachinesCamera", ref cinemachinesCameraCount, ref ObjectNameSuffix, cinemachinesCamerasGameObject, typeof(CinemachineCamera), true);
        }

        /// <summary>
        /// Creates a new GameObject, that can used to focus a Cinemachine-Camera on.
        /// </summary>
        [Button("Create Focus-Object", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Focus-Object")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateFocus), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateFocus)]
        [Tooltip("Creates a GameObject, which can be used to focus a Cinemachine-Camera onto.")]
        internal void CreateNewFocusObject()
        {
            CinemachinesUtility.CreateGameObject("FocusObject", ref focusObjectCount, ref ObjectNameSuffix, cinemachinesFocusObjectGameObject, null, false);
        }

        /// <summary>
        /// Creates a new Spline, that can be assigned inside Cinemachine-Cameras with SplineDolly-Component.
        /// </summary>
        [Button("Open Timeline", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootActions, "Open Timeline")]
        [ButtonGroup(CinemachinesSceneRootActions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderOpenTimeline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderOpenTimeline)]
        [Tooltip("Open Timeline of the current Scene.")]
        internal void OpenTimelineWindow()
        {
            // Get or Create the Timeline Window and lock it to current Scene
            TimelineEditorWindow timelineEditorWindow = TimelineEditor.GetOrCreateWindow();

            // select and lock the current Scene, selecting the current scenes PlayableDirector
            timelineEditorWindow.locked = true;
            timelineEditorWindow.SetTimeline(transform.GetComponent<PlayableDirector>());
        }

        #region Odin Inspector Attributes

        #region Scene Configuration

        protected const string CinemachineSceneConfig = "CinemachineSceneConfig";

        protected const float CinemachineSceneConfigOrderStore = 0;

        protected const float CinemachineSceneConfigOrderDeletion = CinemachineSceneConfigOrderStore + 1;

        #endregion Scene Configuration

        #region Options for CinemachinesSceneRoot

        protected const string CinemachinesSceneRootOptions = "CinemachinesSceneRootOptions";

        protected const float CinemachinesSceneRootOptionsOrderCreateSpline = 10;

        protected const float CinemachinesSceneRootOptionsOrderCreateCamera = CinemachinesSceneRootOptionsOrderCreateSpline + 1;

        protected const float CinemachinesSceneRootOptionsOrderCreateSignal = CinemachinesSceneRootOptionsOrderCreateCamera + 1;

        protected const float CinemachinesSceneRootOptionsOrderCreateFocus = CinemachinesSceneRootOptionsOrderCreateSignal + 1;

        protected const string CinemachinesSceneRootActions = "CinemachinesSceneRootActions";

        protected const float CinemachinesSceneRootOptionsOrderOpenTimeline = 15;

        #endregion Options for CinemachinesSceneRoot

        #region Scene Maintenance

        protected const string CinemachinesSceneRootMaintenance = "CinemachinesSceneRootMaintenance";

        protected const float CinemachinesSceneRootMaintenanceOrderRepair = 20;

        #endregion Scene Maintenance

        #endregion Odin Inspector Attributes
    }
}