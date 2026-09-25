using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using SEE.Cinemachines;
using SEE.Cinemachines.Utility;
using SEE.Utils;
using System.Linq;

#if UNITY_EDITOR

/// Required for <see cref="UnityEditor.InitializeOnLoadAttribute"/>.
using UnityEditor;

#endif

namespace SEEEditor.Cinemachines.Utility
{
    /// <summary>
    /// Class for restoring GameObjects related to the Cinemachines.
    /// Based on work done by inkle Studios: https://github.com/inkle/Unity-Save-Play-Mode-Changes
    /// under MIT License.
    /// </summary>
    [InitializeOnLoad]
    internal static class CinemachinesStateRestorer
    {
        /// <summary>
        /// For storing references of components in a serializable form.
        /// </summary>
        [Serializable]
        internal class StoredReference
        {
            public int InstanceID;
            public bool IsNull;
        }

        /// <summary>
        /// For storing components inside GameObjects in a serializable form.
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
            public List<StoredReference> ListReferences = new();
        }

        /// <summary>
        /// For storing GameObjects in a serializable form.
        /// </summary>
        [Serializable]
        internal class StoredGameObject
        {
            // Data of this GameObject
            public int InstanceID;
            public string JSONGameObject;
            public readonly List<StoredComponent> ListComponents = new();

            // List of children inside this GameObject
            [SerializeReference]
            public List<StoredGameObject> ChildGameObjects = new();
        }

        /// <summary>
        /// Implementation of a Serializer to store changes made in PlayMode persistently.
        /// </summary>
        internal class Serializer
        {
            /// <summary>
            /// Helper function to serialize references inside components.
            /// </summary>
            /// <param name="component">The component to be serialized.</param>
            /// <returns>List of StoredReferences that will be included to the respective component.</returns>
            private List<StoredReference> SerializeReference(Component component)
            {
                // List for storing required References, that can not be applied normally
                List<StoredReference> referenceList = new();

                // Create SerializedObject from the Object/Component and get its SerializedProperty as an Iterator
                SerializedObject serializedObject = new(component);
                SerializedProperty propertyIterator = serializedObject.GetIterator();

                // while there are still properties
                while (propertyIterator.NextVisible(true))
                {
                    // check whether the type of property is relevant to us
                    if (propertyIterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        StoredReference storedReference = new();

                        // get object referenced in property
                        UnityEngine.Object objectReference = propertyIterator.objectReferenceValue;

                        // setup StoredReference appropriatly, based on wether it is null or not
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
            /// Serializing function for Transforms and their associated data.
            /// </summary>
            /// <param name="rootObject">The GameObject to be serialized.</param>
            /// <returns>Returns a StoredGameObject that can be serialized into JSON.</returns>
            internal StoredGameObject Serialize(Transform rootObject)
            {
                StoredGameObject storedGameObject = new();

                // Store base Data of this Object into the StoredGameObject
                storedGameObject.InstanceID      = rootObject.GetInstanceID();
                storedGameObject.JSONGameObject  = EditorJsonUtility.ToJson(rootObject.gameObject);

                // iterate through all components
                foreach (Component component in rootObject.GetComponents(typeof(Component)))
                {
                    StoredComponent storedComponent = new();

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
            /// Temporary Dictionary for storing InstanceIDs of objects with their respective StoredProperties.
            /// </summary>
            private Dictionary<int, List<StoredReference>> storedReferences = new();

            /// <summary>
            /// Reference List between original InstanceIDs and restored IDs.
            /// </summary>
            private Dictionary<int, int> referenceList = new();

            /// <summary>
            /// Helper-Function to Deserialize References inside components, like Transform, UnityEvents, etc.
            /// </summary>
            private void DeserializeReferences()
            {
                foreach (var kvp in storedReferences)
                {
                    // Find the Object/Component you want to apply the references to
                    UnityEngine.Object restoredObject = Resources.InstanceIDToObject(kvp.Key);

                    // create SerializedObject and property
                    SerializedObject serializedObject = new(restoredObject);
                    SerializedProperty propertyIterator = serializedObject.GetIterator();

                    // get the stored References list from the KeyValuePair
                    List<StoredReference> storedReferencesList = kvp.Value;

                    // local counter for iterating through the storedReferences for this object
                    int index = 0;

                    // check if there is still a Property accessable
                    while (propertyIterator.NextVisible(true))
                    {
                        // check, if the type of the Property is relevant to us
                        if (propertyIterator.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            // The stored references are matched to the properties by
                            // their order, so a restored object with more of them than
                            // were stored would run off the end. That can happen where
                            // a component came back with different defaults, in which
                            // case the ones beyond what was stored are left alone.
                            if (index >= storedReferencesList.Count)
                            {
                                Debug.LogWarning($"'{restoredObject.name}' has more references than were "
                                                 + "stored for it. The remaining ones are left as they are.\n");
                                break;
                            }

                            // Select the indexed StoredReference, which potentially needs to be applied
                            StoredReference storedReference = storedReferencesList[index];

                            // If the StoredReference was null, ignore it, ...
                            if (!storedReference.IsNull)
                            {
                                // ... else get the Object by InstanceID and apply it.
                                // The stored ID is the one the object had before the
                                // domain reload. Where the object was itself restored,
                                // referenceList says what it is called now; where it
                                // was not, the old ID is all there is, and it may well
                                // name nothing.
                                if (!referenceList.TryGetValue(storedReference.InstanceID,
                                                               out int objectInstanceID))
                                {
                                    objectInstanceID = storedReference.InstanceID;
                                }

                                UnityEngine.Object objectReference = Resources.InstanceIDToObject(objectInstanceID);

                                if (objectReference == null)
                                {
                                    // Leave whatever the property holds. Assigning the
                                    // null would wipe a reference that the component
                                    // may have brought with it from its own defaults,
                                    // and would do so without the user being any wiser.
                                    Debug.LogWarning($"Object with InstanceID '{objectInstanceID}' does not exist.\n Maybe the object was created during runtime, which must then be manually recreated and re-applied.\n");
                                }
                                else
                                {
                                    propertyIterator.objectReferenceValue = objectReference;
                                }
                            }

                            // Increment index for next reference.
                            index++;
                        }
                    }

                    // Apply all modified properties to the SerializedObject, which applies the
                    // changes to the regular objects/components
                    serializedObject.ApplyModifiedProperties();
                }
            }

            /// <summary>
            /// The type <paramref name="storedComponent"/> names, or null where
            /// the assembly or the type is no longer to be found.
            /// </summary>
            /// <remarks>A script renamed, or a package changed, between the storing
            /// and the restoring leaves a name that resolves to nothing. Reported
            /// rather than thrown, so that the rest of the tree still comes back.</remarks>
            /// <param name="storedComponent">Names the assembly and the type.</param>
            /// <returns>The type, or null.</returns>
            private static Type FindComponentType(StoredComponent storedComponent)
            {
                try
                {
                    Type result = Assembly.Load(storedComponent.AssemblyName)
                                          .GetType(storedComponent.TypeName);
                    if (result == null)
                    {
                        Debug.LogWarning($"Type '{storedComponent.TypeName}' no longer exists in "
                                         + $"assembly '{storedComponent.AssemblyName}'.\n");
                    }
                    return result;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Assembly '{storedComponent.AssemblyName}' could not be loaded: "
                                     + $"{exception.Message}\n");
                    return null;
                }
            }

            /// <summary>
            /// Actual Deserializing Method, which returns the root transform.
            /// </summary>
            /// <param name="storedGameObject">The StoredGameObject that will be restored to actual GameObjects.</param>
            /// <returns>The Transform reconstructed from the StoredGameObject, including their children.</returns>
            private Transform Deserialize(StoredGameObject storedGameObject)
            {
                // Find object as child of rootTransform
                GameObject restoredGameObject = new();
                EditorJsonUtility.FromJsonOverwrite(storedGameObject.JSONGameObject, restoredGameObject);

                // A reference may name the GameObject rather than one of its
                // components, so what this one used to be called is recorded too.
                referenceList[storedGameObject.InstanceID] = restoredGameObject.GetInstanceID();

                // Iterate through all components for this GameObject
                foreach (StoredComponent storedComponent in storedGameObject.ListComponents)
                {
                    Type ComponentType = FindComponentType(storedComponent);

                    // The type is gone, so nothing can be made of what was stored for
                    // it. This happens when a script is renamed or a package changes
                    // between storing and restoring.
                    if (ComponentType == null)
                    {
                        continue;
                    }

                    // Reconstruct type of this component.
                    Component readComponent;
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

                    // AddComponent yields null where the component cannot be added,
                    // a second one of a type allowing only one, say. Everything below
                    // would then fail on the null rather than on the cause.
                    if (readComponent == null)
                    {
                        Debug.LogWarning($"Could not add a component of type '{ComponentType}' to "
                                         + $"'{restoredGameObject.name}'. Its stored state is lost.\n");
                        continue;
                    }

                    // apply Data onto the component
                    EditorJsonUtility.FromJsonOverwrite(storedComponent.JSONContent, readComponent);

                    // store references that might need to be applied after everything has been applied
                    try
                    {
                        storedReferences.Add(readComponent.GetInstanceID(), storedComponent.ListReferences);
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning($"Attempted to map '{readComponent.GetInstanceID()}' to a references list, while already having one associated to it.\n");
                    }

                    // store mapping of old InstanceID to new InstanceID for components
                    try
                    {
                        referenceList.Add(storedComponent.InstanceID, readComponent.GetInstanceID());
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning($"Attempted to map '{storedComponent.InstanceID}' to '{readComponent.GetInstanceID()}'\n");
                    }
                }

                // Iterate depth first through all child GameObjects
                foreach (StoredGameObject childStoredGameObject in storedGameObject.ChildGameObjects)
                {
                    Transform child = Deserialize(childStoredGameObject);

                    // reposition accordingly
                    child.SetParent(restoredGameObject.transform);
                }

                return restoredGameObject.transform;
            }

            /// <summary>
            /// Main Deserializer function for GameObjects, initialized by inputing serialized data.
            /// </summary>
            /// <param name="jsonData">The serialized data that will be restored from.</param>
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
        /// Constructor for the Cinemachines State Restorer, which enables an event to restore changes
        /// made inside CinemachinesRoot to be re-applied after exiting PlayMode.
        /// </summary>
        static CinemachinesStateRestorer()
        {
            // Register new event for restoring changes made in PlayMode
            EditorApplication.playModeStateChanged += StoreCinemachinesChanges;
        }

        /// <summary>
        /// Save function, utilizing the custom Serializer class for Cinemachines.
        /// </summary>
        /// <param name="unityScene">The Unity scene that the object will be stored from.</param>
        /// <returns>True if that scene held a CinemachinesRoot and it was stored.</returns>
        private static bool Save(Scene unityScene)
        {
            // Locating the CinemachinesRoot of this scene, and of no other. Several
            // scenes can be open together, and a search across all of them would store
            // the root of the first under the name of every one of them.
            Transform cinemachinesRootTransform = CinemachinesUtility.GetCinemachinesRootInScene(unityScene);

            if (cinemachinesRootTransform == null)
            {
                return false;
            }

            // Serialize ScenesRoot
            Serializer serializer = new();
            StoredGameObject storedGameObject = serializer.Serialize(cinemachinesRootTransform);

            // Store generated JSON inside EditorPrefs for persistance
            EditorPrefs.SetString($"{unityScene.name}.{CinemachinesUtility.CinemachinesPersistanceKeyName}", JsonUtility.ToJson(storedGameObject));
            return true;
        }

        /// <summary>
        /// Load function, utilizing the custom Deserializer class for Cinemachines.
        /// </summary>
        /// <param name="unityScene">The Unity scene to reconstruct the serialized GameObjects.</param>
        private static void Load(Scene unityScene)
        {
            string serializedDataKeyName = $"{unityScene.name}.{CinemachinesUtility.CinemachinesPersistanceKeyName}";

            if (!EditorPrefs.HasKey(serializedDataKeyName))
            {
                return;
            }

            // prepare deserializer
            Deserializer deserializer = new();

            // get stored scene data with deserializer
            deserializer.Deserialize(EditorPrefs.GetString(serializedDataKeyName));

            // Display warning about referencing original objects.
            EditorUtility.DisplayDialog(
                "Restoration of Cinemachines",
                "Warning! The restored backup is using parts of the original Cinemachine Structure.\nPlease check the Timelines and other objects and update these components to their restored equivalents before continuing.",
                "Okay"
            );

            // Reset persistance key
            if (EditorPrefs.HasKey(serializedDataKeyName))
            {
                EditorPrefs.DeleteKey(serializedDataKeyName);
            }
        }

        /// <summary>
        /// Helper function to get unity scene names with CinemachineRoot backups.
        /// </summary>
        /// <returns>Name of Unity scenes that have GameObject backups.</returns>
        private static string[] GetRestorableScenes()
        {
            // find all Unity scenes in Project
            string[] scenesGUIDs = AssetDatabase.FindAssets("t:Scene");
            List<string> scenesPaths = new();

            foreach (string guid in scenesGUIDs)
            {
                scenesPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            StringBuilder result = new();

            foreach (string scenePath in scenesPaths)
            {
                // check wether scene path starts at the correct location;
                // doing so will exclude every example scene from extensions
                if (!scenePath.StartsWith("Assets/Scenes"))
                {
                    continue;
                }

                // Logic for trimming the path down to the file name of the scene
                int ToDeleteSuffixLength = ".unity".Length;
                string[] UnitySceneNameSplit = scenePath.Remove(scenePath.Length - ToDeleteSuffixLength, ToDeleteSuffixLength).Split('/');
                string UnitySceneName = UnitySceneNameSplit[UnitySceneNameSplit.Length - 1];

                // checking, if a persistance key for the scene exists
                string prefKeyName = $"{UnitySceneName}.{CinemachinesUtility.CinemachinesPersistanceKeyName}";
                if (EditorPrefs.HasKey(prefKeyName))
                {
                    result.AppendFormat("{0}\n", UnitySceneName);
                }
            }

            return result.ToString().Split('\n');
        }

        /// <summary>
        /// Display function to get list of restorable scenes.
        /// </summary>
        [MenuItem("SEE/Cinemachines/Get restorable Cinemachine Roots", false, 12)]
        internal static void GetRestorableRoots()
        {
            // String Builder for dialog body, which includes the names of the scenes, that can restore a CinemachinesRoot
            StringBuilder stringBuilder = new("The CinemachineRoots of the following Unity scenes have been backed up.\n");
            string[] restorableScenes = GetRestorableScenes();

            // Format scene names
            foreach (string scene in restorableScenes.Where(s => s != ""))
            {
                stringBuilder.AppendFormat("* {0}\n", scene);
            }

            stringBuilder.Append("\nTo restore a CinemachineRoot, enter Unity scene and select SEE > Cinemachines > Restore Cinemachine Root");

            EditorUtility.DisplayDialog(
                "Restoration of Cinemachines",
                stringBuilder.ToString(),
                                        "Okay"
            );
        }

        /// <summary>
        /// Checker function, to see if current Unity scene has a restorable CinemachinesRoot.
        /// </summary>
        /// <returns>True, if the current scene has a backup, else false.</returns>
        [MenuItem("SEE/Cinemachines/Restore Cinemachines Root", true, 10)]
        private static bool HasUnitySceneBackup()
        {
            return EditorPrefs.HasKey($"{SceneManager.GetActiveScene().name}.{CinemachinesUtility.CinemachinesPersistanceKeyName}");
        }

        /// <summary>
        /// Menu Entry for restoring the CinemachinesRoot at current scene.
        /// </summary>
        [MenuItem("SEE/Cinemachines/Restore Cinemachines Root", false, 10)]
        internal static void RestoreCinemachinesRoot()
        {
            // Get current scene's name
            string UnitySceneName = SceneManager.GetActiveScene().name;
            string prefKeyName = $"{UnitySceneName}.{CinemachinesUtility.CinemachinesPersistanceKeyName}";

            if (!EditorPrefs.HasKey(prefKeyName))
            {
                EditorUtility.DisplayDialog(
                    "Restoration of Cinemachines",
                    "This Scene contains no valid Backups for CinemachinesRoot.",
                    "Okay"
                );
                return;
            }

            // Confirm whether the user wants to restore to the last PlayTime state.
            if (!EditorUtility.DisplayDialog("Restoration of Cinemachines", "The Editor is in the process of restoring the Cinemachines like they were at the time of exiting PlayTime.\n Do you want to restore to this point?\n A backup of the old Cinemachines will be created.", "Yes, restore", "No, don't restore"))
            {
                // // Reset PersistanceKey and abort
                // if (EditorPrefs.HasKey(prefKeyName))
                //     EditorPrefs.DeleteKey(prefKeyName);
                return;
            }

            // rename and deactivate old Scenes as backup
            Transform CinemachinesRootTransform = CinemachinesUtility.GetCinemachinesRootInScene();

            if (CinemachinesRootTransform != null)
            {
                // backup old root
                HandleOldGameObject(CinemachinesRootTransform.gameObject, true);
            }

            // Load serialized Data
            Load(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Event function to be called when the play-state changes from PlayMode to EditMode.
        /// Specific for restoring the Cinemachine scenes.
        /// </summary>
        /// <param name="state">The current state of the Editor.</param>
        private static void StoreCinemachinesChanges(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:   // Editor is exiting Play-Mode, proceed to save changes made
                    // Iterate through all scenes inside the project
                    bool anythingStored = false;
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        // currently selected scene
                        Scene currScene = SceneManager.GetSceneAt(i);

                        // Save serialized data
                        anythingStored |= Save(currScene);
                    }

                    // Only where there is something to tell. This class is loaded for
                    // everyone working in the project, and a dialog raised on every
                    // exit from play mode, in every scene holding no Cinemachines at
                    // all, is a dialog raised for nothing.
                    if (anythingStored)
                    {
                        GetRestorableRoots();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Helper function to handle old GameObject on load.
        /// </summary>
        /// <param name="oldRoot">The GameObject that will be deactivated or removed.</param>
        /// <param name="backupOldRoot">Parameter that determines whether the <paramref name="oldRoot"/> gets removed or deactivated.</param>
        private static void HandleOldGameObject(GameObject oldRoot, bool backupOldRoot)
        {
            // if the Scenes root doesn't exist, don't try to create a backup
            if (oldRoot != null)
            {
                if (backupOldRoot)
                {
                    // backup old transform
                    oldRoot.name = $"{oldRoot.name} - Backup";
                    oldRoot.SetActive(false);
                    oldRoot.GetComponent<CinemachinesRoot>().enabled = false;
                }
                else
                {
                    // Remove old transform
                    Destroyer.Destroy(oldRoot);
                }
            }
        }
    }
}
