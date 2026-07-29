using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEditor;
using Unity.Cinemachine;
using SEE.Cinemachines;

namespace SEE.Cinemachines.Utility {
    /// <summary>
    /// Static Utility-Class for general functions shared between the custom Cinemachines Components.
    /// </summary>
    internal static class CinemachinesUtility
    {
        #region Constant String names
        internal const string CinemachinesBrainsName              = "CinemachinesBrains";
        internal const string CinemachinesScenesName              = "Scenes";
        internal const string CinemachinesControlCameraName       = "ControlCamera";
        internal const string CinemachinesMainOutputName          = "CinemachinesMainOutput.renderTexture";
        internal const string CinemachinesPIPOutputName           = "CinemachinesPIPOutput.renderTexture";

        internal const string CinemachinesPrefabsRoot             = "Assets/Resources/Prefabs/Cinemachines";
        internal const string CinemachinesRootPrefabsRoot         = "Prefabs/Cinemachines/CinemachinesRoot";
        internal const string CinemachinesAssetsRoot              = "Assets/Cinemachines";

        internal const string CinemachinesPersistanceKeyName                = "CinemachinesPersistanceKey";
        internal const string CinemachinesPersistanceKeyRestorableName      = "CinemachinesPersistanceKeyRestorable";
        #endregion Constant String names

        /// <summary>
        /// Class for Restoring GameObjects related to the Cinemachines.
        /// </summary>
        [InitializeOnLoad]
        internal static class CinemachinesStateRestorer
        {
            /// <summary>
            /// Structure-Class for storing References of Components in a serializable form.
            /// </summary>
            [Serializable]
            internal class StoredReference
            {
                public int InstanceID;
                public bool IsNull;
            }

            /// <summary>
            /// Structure-Class for storing Components inside GameObjects in a serializable form.
            /// </summary>
            [Serializable]
            internal class StoredComponent
            {
                // Data for reconstructing the Type
                public string AssemblyName;
                public string TypeName;

                // Component Data
                public int InstanceID;
                public string JSONContent;
                public List<StoredReference> ListReferences = new List<StoredReference>();
            }

            /// <summary>
            /// Structure-Class for storing GameObjects in a serializable form.
            /// </summary>
            [Serializable]
            internal class StoredGameObject
            {
                // Data of this GameObject
                public int InstanceID;
                public string JSONGameObject;
                public List<StoredComponent> ListComponents = new List<StoredComponent>();

                // List of Children inside this GameObject
                [SerializeReference]
                public List<StoredGameObject> ChildGameObjects = new List<StoredGameObject>(0);
            }

            /// <summary>
            /// Implementation of a Serializer to store changes made in PlayMode persistently.
            /// </summary>
            internal class Serializer
            {
                /// <summary>
                /// Helper-Function to Serialize References inside Components.
                /// </summary>
                /// <param name="component">The Component to be serialized.</param>
                /// <returns>List of StoredReferences, that will be included to the respective Component.</returns>
                private List<StoredReference> SerializeReference(Component component)
                {
                    // List for storing required References, that can not be applied normally
                    List<StoredReference> referenceList = new List<StoredReference>();

                    // Create SerializedObject from the Object/Component and get its SerializedProperty as an Iterator
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty propertyIterator = serializedObject.GetIterator();

                    // while there still are properties
                    while (propertyIterator.NextVisible(true))
                    {
                        // check, if the type of property is relevant to us
                        if (propertyIterator.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            StoredReference storedReference = new StoredReference();

                            // get Object referenced in Property
                            UnityEngine.Object objectReference = propertyIterator.objectReferenceValue;

                            // setup StoredReference appropriatly, based on if its null or not
                            switch (objectReference)
                            {
                                case null:
                                    storedReference.IsNull = true;
                                    break;
                                default:
                                    storedReference.InstanceID = objectReference.GetInstanceID();
                                    storedReference.IsNull = false;
                                    break;
                            }

                            referenceList.Add(storedReference);
                        }
                    }

                    return referenceList;
                }

                /// <summary>
                /// Serializing Function for Transforms and their associated Data.
                /// </summary>
                /// <param name="rootObject">The GameObject to be serialized.</param>
                /// <returns>Returns a StoredGameObject, that can be serialized into JSON.</returns>
                internal StoredGameObject Serialize(Transform rootObject)
                {
                    StoredGameObject storedGameObject = new StoredGameObject();

                    // Store base Data of this Object into the StoredGameObject
                    storedGameObject.InstanceID      = rootObject.GetInstanceID();
                    storedGameObject.JSONGameObject  = EditorJsonUtility.ToJson(rootObject.gameObject);

                    // iterate through all components
                    foreach (Component component in rootObject.GetComponents(typeof(Component)))
                    {
                        StoredComponent storedComponent = new StoredComponent();

                        // deconstruct the type of the Component
                        storedComponent.AssemblyName = component.GetType().Assembly.GetName().Name;
                        storedComponent.TypeName     = component.GetType().FullName;

                        // store base data of this Component
                        storedComponent.InstanceID   = component.GetInstanceID();
                        storedComponent.JSONContent  = EditorJsonUtility.ToJson(component);

                        // serialize references made inside component
                        storedComponent.ListReferences = SerializeReference(component);

                        storedGameObject.ListComponents.Add(storedComponent);
                    }

                    // re-iterate through all GameObjects, that are a child of this Object
                    // needs to be done in a depth-first way
                    foreach (Transform child in rootObject)
                    {
                        storedGameObject.ChildGameObjects.Add(Serialize(child));
                    }

                    return storedGameObject;
                }
            }

            /// <summary>
            /// Implementation of a Deserializer to store changes made in PlayMode persistently.
            /// </summary>
            internal class Deserializer
            {
                /// <summary>
                /// Temporary Dictionary for storing InstanceIDs of Objects with their respective StoredProperties.
                /// </summary>
                private Dictionary<int, List<StoredReference>> storedReferences = new Dictionary<int, List<StoredReference>>();

                /// <summary>
                /// Reference List between original InstanceIDs and restored IDs.
                /// </summary>
                private Dictionary<int, int> referenceList = new Dictionary<int, int>();

                /// <summary>
                /// Helper-Function to Deserialize References inside Components, like Transform, UnityEvents, etc.
                /// </summary>
                private void DeserializeReferences()
                {
                    // Loop through every KeyValuePair in the storedReferences
                    foreach (var kvp in storedReferences)
                    {
                        // Find the Object/Component, you want to apply the References to
                        UnityEngine.Object restoredObject = Resources.InstanceIDToObject(kvp.Key);

                        // create SerializedObject and Property
                        SerializedObject serializedObject = new SerializedObject(restoredObject);
                        SerializedProperty propertyIterator = serializedObject.GetIterator();

                        // get the stored References list from the KeyValuePair
                        List<StoredReference> storedReferences = kvp.Value;

                        // local Counter for iterating through the storedReferences for this Object
                        int Index = 0;

                        // check, if there is still a Property accessable
                        while (propertyIterator.NextVisible(true))
                        {
                            // check, if the type of the Property is relevant to us
                            if (propertyIterator.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                // Select the indexed StoredReference, which potentially needs to be applied
                                StoredReference storedReference = storedReferences[Index];

                                // If the StoredReference was null, ignore it, ...
                                if (!storedReference.IsNull)
                                {
                                    // ... else get the Object by InstanceID and apply it
                                    int objectInstanceID;
                                    if (referenceList.ContainsKey(storedReference.InstanceID))
                                    {
                                        // This part is not working correctly, since Unity differenciates InstanceIDs from actual Objects and References
                                        objectInstanceID = referenceList[storedReference.InstanceID];
                                    }
                                    else
                                    {
                                        objectInstanceID = storedReference.InstanceID;
                                    }

                                    UnityEngine.Object objectReference = Resources.InstanceIDToObject(objectInstanceID);

                                    if (objectReference == null)
                                    {
                                        Debug.LogWarning($"Object with InstanceID '{objectInstanceID}' does not exist.\n Maybe the Object was created during Runtime, which must then be manually recreated and reapplied.");
                                    }

                                    propertyIterator.objectReferenceValue = objectReference;
                                }

                                // Increment Index for next Reference
                                Index++;
                            }
                        }

                        // Apply all modified Properties to the SerializedObject, which applies the changes to the regular Objects/Components
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                /// <summary>
                /// Actual Deserializing Method, which returns the root transform.
                /// </summary>
                /// <param name="storedGameObject">The StoredGameObject, that will be restored to actual GameObjects.</param>
                /// <returns>The Transform reconstructed from the StoredGameObject, including their Children.</returns>
                private Transform Deserialize(StoredGameObject storedGameObject)
                {
                    // Find Object as Child of rootTransform
                    GameObject restoredGameObject = new GameObject();
                    EditorJsonUtility.FromJsonOverwrite(storedGameObject.JSONGameObject, restoredGameObject);

                    // Iterate through all Components for this GameObject
                    foreach (StoredComponent storedComponent in storedGameObject.ListComponents)
                    {
                        // Reconstruct Type of this Component
                        Component readComponent = null;
                        Type ComponentType = Assembly.Load(storedComponent.AssemblyName).GetType(storedComponent.TypeName);

                        // check, if the component is a Transform
                        if (ComponentType == typeof(Transform))
                        {
                            readComponent = restoredGameObject.transform;
                        }
                        else if (ComponentType == typeof(PlayableDirector))
                        {
                            readComponent = restoredGameObject.GetComponent<PlayableDirector>();

                            if (readComponent == null)
                            {
                                readComponent = restoredGameObject.AddComponent(typeof(PlayableDirector));
                            }
                        }
                        else if (ComponentType == typeof(SignalReceiver))
                        {
                            readComponent = restoredGameObject.GetComponent<SignalReceiver>();

                            if (readComponent == null)
                            {
                                readComponent = restoredGameObject.AddComponent(typeof(SignalReceiver));
                            }
                        }
                        else
                        {
                            readComponent = restoredGameObject.AddComponent(ComponentType);
                        }

                        // apply Data onto the component
                        EditorJsonUtility.FromJsonOverwrite(storedComponent.JSONContent, readComponent);

                        // store References, that might need to be applied after everything has been applied
                        storedReferences.Add(readComponent.GetInstanceID(), storedComponent.ListReferences);

                        // store mapping of old InstanceID to new InstanceID for Components
                        try
                        {
                            referenceList.Add(storedComponent.InstanceID, readComponent.GetInstanceID());
                        }
                        catch (Exception _)
                        {
                            Debug.LogWarning($"Attempted to Map '{storedComponent.InstanceID}' to '{readComponent.GetInstanceID()}'");
                        }
                    }

                    // Iterate depth-first through all child GameObjects
                    foreach (StoredGameObject childStoredGameObject in storedGameObject.ChildGameObjects)
                    {
                        Transform child = Deserialize(childStoredGameObject);

                        // reposition accordingly
                        child.SetParent(restoredGameObject.transform);
                    }

                    return restoredGameObject.transform;
                }

                /// <summary>
                /// Primer Deserializer Function for GameObjects, initialized by inputing serialized Data.
                /// </summary>
                /// <param name="jsonData">The serialized data, that will be restored from.</param>
                /// <returns>The restored Transform, including restored components and children.</returns>
                internal Transform Deserialize(string jsonData)
                {
                    // Deserialize first layer
                    StoredGameObject storedGameObject = JsonUtility.FromJson<StoredGameObject>(jsonData);

                    // Reconstruct root GameObject from deserialized Data
                    Transform restoredRootTransform = Deserialize(storedGameObject);

                    // apply references to gameObject
                    DeserializeReferences();

                    return restoredRootTransform;
                }
            }

            /// <summary>
            /// Constructor for the Cinemachines State Restorer, which enables an Event to restore changes made inside CinemachinesRoot to be re-applied after exiting PlayMode.
            /// </summary>
            static CinemachinesStateRestorer()
            {
                // Register new Event for restoring changes made in PlayMode
                EditorApplication.playModeStateChanged += StoreCinemachinesChanges;
            }

            /// <summary>
            /// Save function, utilizing the custom Serializer Class for Cinemachines.
            /// </summary>
            /// <param name="UnitySceneName">The Name of the Unity-Scene, that the Object will be stored from.</param>
            private static void Save(string unitySceneName)
            {
                // Locating CinemachinesRoot
                Transform cinemachinesRootTransform = GetCinemachinesRootInScene();

                if (cinemachinesRootTransform == null)
                {
                    return;
                }

                // JSON storage
                string serializedData = "";

                // Serialize ScenesRoot
                Serializer serializer = new Serializer();
                StoredGameObject storedGameObject = serializer.Serialize(cinemachinesRootTransform);

                serializedData = JsonUtility.ToJson(storedGameObject);

                // Store generated JSON inside EditorPrefs for persistance
                EditorPrefs.SetString($"{unitySceneName}.{CinemachinesPersistanceKeyName}", serializedData);
            }

            /// <summary>
            /// Load function, utilizing the custom Deserializer Class for Cinemachines.
            /// </summary>
            /// <param name="UnityScene">The Unity-Scene to reconstruct the serialized GameObjects.</param>
            private static void Load(Scene unityScene)
            {
                string serializedDataKeyName = $"{unityScene.name}.{CinemachinesPersistanceKeyName}";

                if (!EditorPrefs.HasKey(serializedDataKeyName))
                {
                    return;
                }
                // JSON storage
                string serializedData = "";

                // prepare deserializer
                Deserializer deserializer = new Deserializer();
                serializedData = EditorPrefs.GetString(serializedDataKeyName);

                // get stored scenes-data with deserializer
                Transform restoredObject = deserializer.Deserialize(serializedData);

                // Display warning about referencing original Objects
                EditorUtility.DisplayDialog(
                    "Restoration of Cinemachines",
                    "Warning! The restored Backup is using parts of the original Cinemachine Structure.\nPlease check the Timelines and other Objects and update these components to their restored equivalents before continuing.",
                    "Okay"
                );

                // Reset PersistanceKey
                if (EditorPrefs.HasKey(serializedDataKeyName))
                {
                    EditorPrefs.DeleteKey(serializedDataKeyName);
                }
            }

            /// <summary>
            /// Helper-Function to get Unity-Scene names with CinemachineRoot-Backups.
            /// </summary>
            /// <returns>Name of Unity-Scenes, that have GameObject backups.</returns>
            private static string[] GetRestorableScenes()
            {
                // find all Unity-Scenes in Project
                string[] scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
                List<string> scenesPaths = new List<string>();

                foreach (string guid in scenesGUIDs)
                    scenesPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

                StringBuilder result = new StringBuilder();

                foreach (string scenePath in scenesPaths)
                {
                    // check, if scene-path starts at the correct location
                    // doing so will exclude every example scene from Extensions
                    if (!scenePath.StartsWith("Assets/Scenes"))
                        continue;

                    // Logic for trimming the path down to the File-name of the Scene
                    int ToDeleteSuffixLength = ".unity".Length;
                    string[] UnitySceneNameSplit = scenePath.Remove(scenePath.Length - ToDeleteSuffixLength, ToDeleteSuffixLength).Split('/');
                    string UnitySceneName = UnitySceneNameSplit[UnitySceneNameSplit.Length - 1];

                    // checking, if a persistance key for the scene exists
                    string prefKeyName = $"{UnitySceneName}.{CinemachinesPersistanceKeyName}";
                    if (EditorPrefs.HasKey(prefKeyName))
                        result.AppendFormat("{0}\n", UnitySceneName);
                }

                return result.ToString().Split('\n');
            }

            /// <summary>
            /// Display Function to get list of restorable scenes.
            /// </summary>
            [MenuItem("SEE/Cinemachines/Get restorable Cinemachine Roots", false, 12)]
            internal static void GetRestorableRoots()
            {
                // String Builder for Dialog Body, which includes the names of the Scenes, that can restore a CinemachinesRoot
                StringBuilder stringBuilder = new StringBuilder("The CinemachineRoots of the following Unity-Scenes have been backed up.\n");
                string[] restorableScenes = GetRestorableScenes();

                // Format Scene Names
                foreach (string scene in restorableScenes)
                {
                    if (scene != "")
                    {
                        stringBuilder.AppendFormat("* {0}\n", scene);
                    }
                }

                stringBuilder.Append("\nTo restore a CinemachineRoot, enter Unity-Scene and select SEE > Cinemachines > Restore Cinemachine Root");

                EditorUtility.DisplayDialog(
                    "Restoration of Cinemachines",
                    stringBuilder.ToString(),
                    "Okay"
                );
            }

            /// <summary>
            /// Checker function, to see if current Unity-Scene has a restoreable CinemachinesRoot.
            /// </summary>
            /// <returns>True, if the current Scene has a backup, else false.</returns>
            [MenuItem("SEE/Cinemachines/Restore Cinemachines Root", true, 10)]
            private static bool HasUnitySceneBackup() {
                return EditorPrefs.HasKey($"{SceneManager.GetActiveScene().name}.{CinemachinesPersistanceKeyName}");
            }

            /// <summary>
            /// Menu Entry for restoring the CinemachinesRoot at current Scene.
            /// </summary>
            [MenuItem("SEE/Cinemachines/Restore Cinemachines Root", false, 10)]
            internal static void RestoreCinemachinesRoot()
            {
                // Get Current Scenes Name
                string UnitySceneName = SceneManager.GetActiveScene().name;
                string prefKeyName = $"{UnitySceneName}.{CinemachinesPersistanceKeyName}";

                if (!EditorPrefs.HasKey(prefKeyName))
                {
                    EditorUtility.DisplayDialog(
                        "Restoration of Cinemachines",
                        "This Scene contains no valid Backups for CinemachinesRoot.",
                        "Okay"
                    );
                    return;
                }

                // Confirm, if the user wants to restore to the last PlayTime State
                if (!EditorUtility.DisplayDialog("Restoration of Cinemachines", "The Editor is in the process of restoring the Cinemachines, like they where at the time of exiting PlayTime.\n Do you want to restore to this Point?\n A Backup of the old Cinemachines will be created.", "Yes, restore", "No, don't restore"))
                {
                    // // Reset PersistanceKey and abort
                    // if (EditorPrefs.HasKey(prefKeyName))
                    //     EditorPrefs.DeleteKey(prefKeyName);
                    return;
                }

                // rename and deactivate old Scenes as backup
                Transform CinemachinesRootTransform = GetCinemachinesRootInScene();

                if (CinemachinesRootTransform != null)
                    // backup old root
                    HandleOldGameObject(CinemachinesRootTransform, true);

                // Load serialized Data
                Load(SceneManager.GetActiveScene());
            }

            /// <summary>
            /// Event Function to be called when the Play-State changes from PlayMode to EditMode.
            /// Specific for restoring the Cinemachine-Scenes.
            /// </summary>
            /// <param name="state">The current state of the Editor.</param>
            private static void StoreCinemachinesChanges(PlayModeStateChange state)
            {
                switch (state)
                {
                    case PlayModeStateChange.ExitingPlayMode:   // Editor is exiting Play-Mode, proceed to save changes made
                        // Iterate through all Scenes inside the Project
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            // currently selected Scene
                            Scene currScene = SceneManager.GetSceneAt(i);

                            // Save serialized Data
                            Save(currScene.name);
                        }

                        GetRestorableRoots();
                        break;
                    default:
                        break;
                }
            }

            /// <summary>
            /// Helper Function to handle old GameObject on load.
            /// </summary>
            /// <param name="oldRoot">The GameObject, that will be deactivated or removed.</param>
            /// <param name="backupOldRoot">Parameter that determines, if the <paramref name="oldRoot"> gets removed or deactivated.</param>
            private static void HandleOldGameObject(Transform oldRoot, bool backupOldRoot)
            {
                // if the Scenes root doesn't exist, don't try to create a backup
                if (oldRoot != null)
                {
                    if (backupOldRoot)
                    {
                        // backup old transform
                        oldRoot.gameObject.name = $"{oldRoot.gameObject.name} - Backup";
                        oldRoot.gameObject.SetActive(false);
                        oldRoot.GetComponent<CinemachinesRoot>().enabled = false;
                    }
                    else
                    {
                        // Remove old transform
#if UNITY_EDITOR
                        Debug.Log("Immediate Destroying from Editor", oldRoot);
                        UnityEngine.Object.DestroyImmediate(oldRoot);
#else
                        Debug.Log("Destroying during Runtime", oldRoot);
                        UnityEngine.Object.Destroy(oldRoot);
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Returns the active CinemachineRoots-Transform, if one exists in the current Scene.
        /// </summary>
        /// <returns>Returns the active Transform, that includes the Cinemachines-Root Component, or null, of none is found.</returns>
        internal static Transform GetCinemachinesRootInScene()
        {
            CinemachinesRoot[] CinemachinesRootComponents = FindAllCinemachinesRootsInScene();

            if (CinemachinesRootComponents.Length < 1)
                return null;

            return CinemachinesRootComponents[0].transform;
        }

        /// <summary>
        /// Function that finds all active <see cref="CinemachinesRoot">-Components in the current Unity-Scene.
        /// </summary>
        /// <remarks>There should only be one active <see cref="CinemachinesRoot">-Component in a Unity-Scene.</remarks>
        /// <returns>List of <see cref="CinemachinesRoot">-Components in the current Unity-Scene.</returns>
        internal static CinemachinesRoot[] FindAllCinemachinesRootsInScene()
        {
            return UnityEngine.Object.FindObjectsByType<CinemachinesRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        /// <summary>
        /// Menu Entry for creating the CinemachinesRoot easily.
        /// </summary>
        [MenuItem("SEE/Cinemachines/Create Cinemachines Root", false, 10)]
        [MenuItem("GameObject/SEE/Cinemachines/Create Cinemachines Root", false, 10)]
        internal static void CreateCinemachinesRoot()
        {
            // Create a new CinemachinesRoot at the Root of the Scene
            GameObject newRoot = new GameObject("CinemachinesRoot", typeof(CinemachinesRoot));
        }

        /// <summary>
        /// Helper Function to generate the "Scene Deletion Warning"-Message, including the Path to the respective Folder.
        /// </summary>
        /// <param name="guid">The GUID of the Folder for the corresponding Cinemachines-Scene.</param>
        /// <returns>The Message generated for the Cinemachines-Scene.</returns>
        internal static string GetSceneDeletionWarningMessage(string guid) {
            return $"Deleting the Scene will also delete its associated Scene Folder, which is \"{AssetDatabase.GUIDToAssetPath(guid)}\"";
        }

        /// <summary>
        /// Helper Function to generate the Scene Folder, based on the current active UnityScene.
        /// </summary>
        /// <param name="sceneName">The Name of the Cinemachines-Scene.</param>
        /// <returns>The GUID of the Cinemachines-Scene folder.</returns>
        internal static string GenerateSceneFolder(string sceneName)
        {
            // create new Folder for Scene in Assets/Cinemachines/Scenes
            string SceneGUID = AssetDatabase.CreateFolder($"{CinemachinesAssetsRoot}/Scenes", $"{SceneManager.GetActiveScene().name} - {sceneName}");
            // create Signals folder to store Timeline Signals
            if (!AssetDatabase.IsValidFolder($"{AssetDatabase.GUIDToAssetPath(SceneGUID)}/Signals"))
                AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(SceneGUID), "Signals");

            return SceneGUID;
        }

        /// <summary>
        /// Generator-Function to create the Cinemachines-Prefab Structure.
        /// </summary>
        internal static void GenerateCinemachinesPrefabFolder()
        {
            // If the Directory doesn't exists, create it
            if(!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/Scenes"))
            {
                // Check and create Sub-Directories, if they don't exist
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                    AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

                if (!AssetDatabase.IsValidFolder(CinemachinesPrefabsRoot))
                    AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "Cinemachines");

                if (!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/Scenes"))
                    AssetDatabase.CreateFolder(CinemachinesPrefabsRoot, "Scenes");

                Debug.Log($"Created Folder Structure: {CinemachinesPrefabsRoot}/Scenes");
                return;
            }
        }

        /// <summary>
        /// Generator-Function to create the Scene Structure.
        /// </summary>
        /// <param name="scene">The Cinemachine-Scene GameObject.</param>
        /// <param name="sceneName">The Name of the Cinemachines-Scene.</param>
        internal static void GenerateSceneStructure(GameObject scene, string sceneName)
        {
            // Create Scene Folder
            string sceneRootGUID = GenerateSceneFolder(sceneName);
            // assign Scene-Folder to Scene-GameObject
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneRootGUID);

            // Add the CinemachinesScenes Component to the newly created Scene GameObject
            scene.GetComponent<CinemachinesScene>().SceneGUID = sceneRootGUID;
        }

        /// <summary>
        /// Helper-Function to construct the Name of the Object.
        /// </summary>
        /// <param name="objectType">Type of Object the Name should be constructed.</param>
        /// <param name="objectCount">Amount of Objects already created.</param>
        /// <exception cref="ArgumentException">Gets thrown, if the objectType is not defined or invalid.</exception>
        /// <returns>Fully constructed Name for the Object.</returns>
        internal static string GetNewObjectName(string objectType, ref int objectCount, ref string suffixText)
        {
            if (objectType == string.Empty || objectType == null)
            {
                throw new ArgumentException("objectType string cannot be empty or null");
            }

            // Form Name based on Type and Count
            string newName = $"{objectType}{objectCount}";

            // add optional suffix to name, if one is defined
            if (!String.IsNullOrWhiteSpace(suffixText))
            {
                newName += $" - {suffixText}";
            }

            // Reset the Text-Field and increment the counter
            suffixText = "";
            objectCount += 1;

            return newName;
        }

        /// <summary>
        /// General Helper-Function to construct any GameObject with only one component added.
        /// </summary>
        /// <param name="objectType">Type of Object the Name should be constructed.</param>
        /// <param name="objectCount">Amount of Objects already created.</param>
        /// <param name="rootGameObject">The Root GameObject, that the new GameObject should be attached to.</param>
        /// <param name="componentToAdd">The Component to add to the newly created GameObject. By default, it will not add any components.</param>
        /// <param name="shouldBeFocused">Whether the newly created GameObject should be selected or not. By default, it will get selected.</param>
        /// <exception cref="ArgumentException">Gets thrown, if either the objectType and/or rootGameObject are not defined or invalid.</exception>
        internal static void CreateGameObject(string objectType, ref int objectCount, ref string suffixText, GameObject rootGameObject, System.Type componentToAdd = null, bool shouldBeFocused = true)
        {
            // Throw exception, if objectType is empty or null
            if (objectType == string.Empty || objectType == null)
            {
                throw new ArgumentException("objectType string cannot be empty or null.");
            }

            // Throw exception, if rootGameObject is null
            if (rootGameObject == null)
            {
                throw new ArgumentNullException("rootGameObject cannot be null.");
            }

            string objectName = GetNewObjectName(objectType, ref objectCount, ref suffixText);

            GameObject newObject;
            // Create a new GameObject, and place it in under the correct GameObject
            if (componentToAdd == null)
            {
                newObject = new GameObject(objectName);
            }
            else
            {
                newObject = new GameObject(objectName, componentToAdd);
            }
            newObject.transform.SetParent(rootGameObject.transform);

            // Select newly created GameObject
            if (shouldBeFocused)
            {
                Selection.activeGameObject = newObject;
            }
        }
    }
}
