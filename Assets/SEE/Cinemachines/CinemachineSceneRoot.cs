using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Cinemachines.Utility;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Splines;
using UnityEditor.Timeline;

#endif

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Unity.Properties;
using Unity.Cinemachine;
using Debug = UnityEngine.Debug;
using SEE.Game;

namespace SEE.Cinemachines
{
    /// <summary>
    /// Scene component that controls and handles GameObjects and Assets specific to a Cinemachines scene.
    /// </summary>
    [Serializable]
    [ExecuteInEditMode]
    [RequireComponent(typeof(PlayableDirector))]
    [RequireComponent(typeof(SignalReceiver))]
    internal class CinemachinesScene : SerializedMonoBehaviour
    {
        #if UNITY_EDITOR

        /// <summary>
        /// Incremental counter for Cinemachines camera. Counter not decremented on camera deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int cinemachinesCameraCount;

        /// <summary>
        /// Incremental counter for splines. Counter not decremented on spline deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int splineCount;

        /// <summary>
        /// Incremental counter for signals. Counter not decremented on signal deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int signalCount;

        /// <summary>
        /// Incremental counter for focus objects. Counter not decremented on object deletion to avoid duplication.
        /// </summary>
        [HideInInspector, SerializeField]
        private int focusObjectCount;

        /// <summary>
        /// The GameObject that points to the Cinemachines cameras' root of the scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesCamerasGameObject;

        /// <summary>
        /// The GameObject that points to the splines root of the scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesSplinesGameObject;

        /// <summary>
        /// The GameObject that points to the focus objects' root of the scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesFocusObjectGameObject;

        /// <summary>
        /// The GameObject that points to the miscellaneous root of the scene.
        /// </summary>
        [HideInInspector]
        private GameObject? cinemachinesOtherObjectGameObject;

        /// <summary>
        /// Initializes all variables for the scenes.
        /// </summary>
        protected void Start()
        {
            // Find relevant GameObject and remember them.
            cinemachinesCamerasGameObject = transform.Find("Cameras")?.gameObject;
            cinemachinesSplinesGameObject = transform.Find("Splines")?.gameObject;
            cinemachinesFocusObjectGameObject = transform.Find("FocusObjects")?.gameObject;
            cinemachinesOtherObjectGameObject = transform.Find("OtherObjects")?.gameObject;

            // create gameobjects, if they are not found
            if (!cinemachinesCamerasGameObject || !cinemachinesSplinesGameObject
                || !cinemachinesFocusObjectGameObject || !cinemachinesOtherObjectGameObject)
            {
                if (!cinemachinesCamerasGameObject)
                {
                    cinemachinesCamerasGameObject = new GameObject("Cameras");
                    cinemachinesCamerasGameObject.transform.SetParent(transform);

                    // Dont Save in Build
                    cinemachinesCamerasGameObject.tag = Tags.EditorOnly;
                }

                if (!cinemachinesSplinesGameObject)
                {
                    cinemachinesSplinesGameObject = new GameObject("Splines");
                    cinemachinesSplinesGameObject.transform.SetParent(transform);

                    // Dont Save in Build
                    cinemachinesSplinesGameObject.tag = Tags.EditorOnly;
                }

                if (!cinemachinesFocusObjectGameObject)
                {
                    cinemachinesFocusObjectGameObject = new GameObject("FocusObjects");
                    cinemachinesFocusObjectGameObject.transform.SetParent(transform);

                    // Dont Save in Build
                    cinemachinesFocusObjectGameObject.tag = Tags.EditorOnly;
                }

                if (!cinemachinesOtherObjectGameObject)
                {
                    cinemachinesOtherObjectGameObject = new GameObject("OtherObjects");
                    cinemachinesOtherObjectGameObject.transform.SetParent(transform);

                    // Dont save in build
                    cinemachinesOtherObjectGameObject.tag = Tags.EditorOnly;
                }
            }

            // Dont save in build
            tag = Tags.EditorOnly;
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
        /// Asset GUID of the folder associated with this scene.
        /// </summary>
        /// <remarks>Can be assigned only once, namely when the scene folder is created.
        /// Re-assignment would orphan the previous folder and make <see cref="DestroyObject"/>
        /// delete the folder of a different scene.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the GUID is already set.</exception>
        public string SceneGUID
        {
            get => sceneGUID;
            set
            {
                if (!String.IsNullOrWhiteSpace(sceneGUID))
                {
                    throw new InvalidOperationException($"The scene folder GUID is already set to {sceneGUID}.");
                }
                sceneGUID = value;
            }
        }

        /// <summary>
        /// Deletes this CinemachineScene from the scenes.
        /// </summary>
        [Button("Delete selected scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Delete Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderDeletion), RuntimeGroupOrder(CinemachineSceneConfigOrderDeletion)]
        [InfoBox("@CinemachinesUtility.GetSceneDeletionWarningMessage(SceneGUID)", InfoMessageType.Warning)]
        [Tooltip("Removes the currently selected Cinemachine scene and its associated Timeline and other assets.")]
        internal void DestroyObject()
        {
            // Confirm, if the user wants to delete the Scene, permanently
            if (!EditorUtility.DisplayDialog("Deletion Confirmation", "Are you sure, you want to remove this scene?\n This will also permanently remove any associated files?", "Yes, delete", "No, keep scene"))
            {
                return;
            }

            // Delete Children before destroying the Object
            foreach (Transform Child in transform)
            {
                #if UNITY_EDITOR
                Debug.Log("Immediate destroying scene children within editor\n", Child.gameObject);
                DestroyImmediate(Child.gameObject);
                #else
                Debug.Log("Destroying scene children during runtime\n", Child.gameObject);
                Destroyer.Destroy(Child.gameObject);
                #endif
            }

            // Remove SceneFolder inside Assets/Cinemachines/Scenes, if one is assigned to this scene
            if (!String.IsNullOrWhiteSpace(SceneGUID))
            {
                string PathToSceneFolder = AssetDatabase.GUIDToAssetPath(SceneGUID);
                Debug.Log($"Attempting to remove associated scenes folder from project. Path: {PathToSceneFolder}\n", this);
                if (!String.IsNullOrWhiteSpace(PathToSceneFolder))
                {
                    AssetDatabase.DeleteAsset(PathToSceneFolder);
                }
                else
                {
                    Debug.LogWarning("Failed to find scene folder. Assuming it never existed.\n", this);
                }
            }
            else
            {
                Debug.LogWarning("GUID of scene folder not set. Assuming it never existed.\n", this);
            }

            // Remove self
            #if UNITY_EDITOR
            Debug.Log("Immediate destroying scene root from editor\n", transform.gameObject);
            DestroyImmediate(transform.gameObject);
            #else
            Debug.Log("Destroying scene root during runtime\n", transform.gameObject);
            Destroyer.Destroy(transform.gameObject);
            #endif
        }

        /// <summary>
        /// Stores this CinemachineScene as a prefab.
        /// </summary>
        [Button("Backup Scene", ButtonSizes.Small), RuntimeButton(CinemachineSceneConfig, "Backup Scene")]
        [ButtonGroup(CinemachineSceneConfig)]
        [PropertyOrder(CinemachineSceneConfigOrderStore), RuntimeGroupOrder(CinemachineSceneConfigOrderStore)]
        [Tooltip("Stores the current scene as a prefab for loading in a different Unity scene or in the same in a different spot. This does not carry over references specific to a scene.")]
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
                Debug.Log($"Scene has been successfully stored under {assetPathOfScene}.\n");
            else
                Debug.LogError($"Failed to store scene under {assetPathOfScene}.\n");
        }

        /// <summary>
        /// Text-Field for a Name Suffix, to be appended to the Objects Name after creation.
        /// </summary>
        [Title("Scene Object Creation", horizontalLine: true)]
        [LabelText("Suffix for Object")]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSpline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSpline)]
        [Tooltip("Name to be added as a suffix to the Spline or Cinemachines Camera.")]
        public string ObjectNameSuffix = "";

        /// <summary>
        /// Creates a new Spline, that can be assigned inside Cinemachine-Cameras with SplineDolly-Component.
        /// </summary>
        [Button("Create Spline", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Spline")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSpline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSpline)]
        [Tooltip("Creates a GameObject, including an empty SplineContainer component. Note that the positions inside the SplineContainer are relative to the root of the GameObject and it is always placed at scene origin.")]
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
        /// Creates a Signal inside the scene folder, which can be used on the scenes timeline to trigger or invoke functions of certain objects or scripts.
        /// </summary>
        [Button("Create Signal", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Signal")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateSignal), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateSignal)]
        [Tooltip("Creates a Signal inside the current scenes folder. This Signal then can be used on the current scenes timeline for triggering or accessing specific functions")]
        internal void CreateNewSignal()
        {
            string signalName = $"{transform.name} - {CinemachinesUtility.GetNewObjectName("Signal", ref signalCount, ref ObjectNameSuffix)}";

            // Create a new Signal, and store it under this Scenes Signals Folder
            SignalAsset newSignal = ScriptableObject.CreateInstance<SignalAsset>();

            string scenePath = AssetDatabase.GUIDToAssetPath(SceneGUID);

            Debug.Log($"Creating SignalsAsset in: \"{scenePath}/Signals\"\n");
            AssetDatabase.CreateAsset(newSignal, $"{scenePath}/Signals/{signalName}.signal");
        }

        /// <summary>
        /// Creates a new Cinemachines Camera, that can be assigned to a Timeline.
        /// </summary>
        [Button("Add Camera", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Cinemachine Camera")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateCamera), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateCamera)]
        [Tooltip("Creates a GameObject, including the Cinemachines Camera component.")]
        internal void CreateNewCamera()
        {
            CinemachinesUtility.CreateGameObject("CinemachinesCamera", ref cinemachinesCameraCount, ref ObjectNameSuffix, cinemachinesCamerasGameObject, typeof(CinemachineCamera), true);
        }

        /// <summary>
        /// Creates a new GameObject that can be used to focus a Cinemachine Camera on.
        /// </summary>
        [Button("Create Focus Object", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootOptions, "Create Focus Object")]
        [ButtonGroup(CinemachinesSceneRootOptions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderCreateFocus), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderCreateFocus)]
        [Tooltip("Creates a GameObject that can be used to focus a Cinemachine Camera onto.")]
        internal void CreateNewFocusObject()
        {
            CinemachinesUtility.CreateGameObject("FocusObject", ref focusObjectCount, ref ObjectNameSuffix, cinemachinesFocusObjectGameObject, null, false);
        }

        /// <summary>
        /// Creates a new Spline, that can be assigned inside Cinemachine-Cameras with SplineDolly component.
        /// </summary>
        [Button("Open Timeline", ButtonSizes.Small), RuntimeButton(CinemachinesSceneRootActions, "Open Timeline")]
        [ButtonGroup(CinemachinesSceneRootActions)]
        [PropertyOrder(CinemachinesSceneRootOptionsOrderOpenTimeline), RuntimeGroupOrder(CinemachinesSceneRootOptionsOrderOpenTimeline)]
        [Tooltip("Open Timeline of the current scene.")]
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

        #endif
    }
}
