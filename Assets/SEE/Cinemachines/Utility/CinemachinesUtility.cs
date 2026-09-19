using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SEE.Game;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE.Cinemachines.Utility
{
    /// <summary>
    /// Utility class for general functions shared between the custom Cinemachines components.
    /// </summary>
    internal static class CinemachinesUtility
    {
        #if UNITY_EDITOR

        #region Constant String names
        internal const string CinemachinesBrainsName = "CinemachinesBrains";
        internal const string CinemachinesScenesName = "Scenes";
        internal const string CinemachinesControlCameraName = "ControlCamera";
        internal const string CinemachinesMainOutputName = "CinemachinesMainOutput.renderTexture";
        internal const string CinemachinesPIPOutputName = "CinemachinesPIPOutput.renderTexture";

        internal const string CinemachinesPrefabsRoot = "Assets/Resources/Prefabs/Cinemachines";
        internal const string CinemachinesRootPrefabsRoot = "Prefabs/Cinemachines/CinemachinesRoot";
        internal const string CinemachinesAssetsRoot = "Assets/Cinemachines";

        internal const string CinemachinesPersistanceKeyName = "CinemachinesPersistanceKey";
        internal const string CinemachinesPersistanceKeyRestorableName = "CinemachinesPersistanceKeyRestorable";
        /// <summary>
        /// The name of the root GameObject, that contains all Cinemachines related GameObjects in the scene.
        /// </summary>
        private const string CinemachinesRootName = "CinemachinesRoot";
        #endregion Constant String names

        /// <summary>
        /// Returns the active CinemachineRoots-Transform if one exists in the current scene.
        /// </summary>
        /// <returns>Returns the active Transform that includes the Cinemachines root component, or null if none is found.</returns>
        internal static Transform GetCinemachinesRootInScene()
        {
            CinemachinesRoot[] CinemachinesRootComponents = FindAllCinemachinesRootsInScene();

            if (CinemachinesRootComponents.Length < 1)
            {
                return null;
            }

            return CinemachinesRootComponents[0].transform;
        }

        /// <summary>
        /// Returns all active <see cref="CinemachinesRoot"> components in the current Unity scene.
        /// </summary>
        /// <remarks>There should only be one active <see cref="CinemachinesRoot"> component in a Unity scene.</remarks>
        /// <returns>List of <see cref="CinemachinesRoot"> components in the current Unity scene.</returns>
        internal static CinemachinesRoot[] FindAllCinemachinesRootsInScene()
        {
            return UnityEngine.Object.FindObjectsByType<CinemachinesRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        /// <summary>
        /// Menu entry for creating the CinemachinesRoot easily.
        /// </summary>
        [MenuItem("SEE/Cinemachines/Create Cinemachines Root", false, 10)]
        [MenuItem("GameObject/SEE/Cinemachines/Create Cinemachines Root", false, 10)]
        internal static void CreateCinemachinesRoot()
        {
            // Create a new CinemachinesRoot at the root of the scene
            new GameObject(CinemachinesRootName, typeof(CinemachinesRoot));
        }

        /// <summary>
        /// Helper Function to generate the "Scene Deletion Warning" message,
        /// including the path to the respective folder.
        /// </summary>
        /// <param name="guid">The GUID of the folder for the corresponding Cinemachines scene.</param>
        /// <returns>The message generated for the Cinemachines scene.</returns>
        internal static string GetSceneDeletionWarningMessage(string guid)
        {
            return $"Deleting the scene will also delete its associated scene folder, which is \"{AssetDatabase.GUIDToAssetPath(guid)}\"";
        }

        /// <summary>
        /// Helper Function to generate the scene folder based on the currently active Unity scene.
        /// </summary>
        /// <param name="sceneName">The name of the Cinemachines scene.</param>
        /// <returns>The GUID of the Cinemachines scene folder.</returns>
        internal static string GenerateSceneFolder(string sceneName)
        {
            // create new folder for Scene in Assets/Cinemachines/Scenes
            string SceneGUID = AssetDatabase.CreateFolder($"{CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}", $"{sceneName}");
            // create signals folder to store timeline signals
            if (!AssetDatabase.IsValidFolder($"{AssetDatabase.GUIDToAssetPath(SceneGUID)}/Signals"))
            {
                AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(SceneGUID), "Signals");
            }

            return SceneGUID;
        }

        /// <summary>
        /// Creates the Cinemachines prefab structure.
        /// </summary>
        internal static void GenerateCinemachinesPrefabFolder()
        {
            // If the Directory doesn't exist, create it.
            if (!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/Scenes"))
            {
                // Check and create Sub-Directories, if they don't exist
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
                }

                if (!AssetDatabase.IsValidFolder(CinemachinesPrefabsRoot))
                {
                    AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "Cinemachines");
                }

                if (!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/Scenes"))
                {
                    AssetDatabase.CreateFolder(CinemachinesPrefabsRoot, "Scenes");
                }

                Debug.Log($"Created Folder Structure: {CinemachinesPrefabsRoot}/Scenes.\n");
            }
        }

        /// <summary>
        /// Creates the scene structure.
        /// </summary>
        /// <param name="scene">The Cinemachine scene GameObject.</param>
        /// <param name="sceneName">The name of the Cinemachines scene.</param>
        internal static void GenerateSceneStructure(GameObject scene, string sceneName)
        {
            // Add the CinemachinesScenes Component to the newly created scene GameObject
            scene.GetComponent<CinemachinesScene>().SceneGUID = GenerateSceneFolder(sceneName);
        }

        /// <summary>
        /// Constructs the name of the object.
        /// </summary>
        /// <param name="objectType">Type of Object the name should be constructed.</param>
        /// <param name="objectCount">Amount of Objects already created.</param>
        /// <exception cref="ArgumentException">Gets thrown, if the objectType is not defined or invalid.</exception>
        /// <returns>Fully constructed name for the object.</returns>
        internal static string GetNewObjectName(string objectType, ref int objectCount, ref string suffixText)
        {
            if (string.IsNullOrEmpty(objectType))
            {
                throw new ArgumentException($"{nameof(objectType)} string must neither be empty nor null.");
            }

            // Form name based on type and count
            string newName = $"{objectType}{objectCount}";

            // add optional suffix to name, if one is defined
            if (!String.IsNullOrWhiteSpace(suffixText))
            {
                newName += $" - {suffixText}";
            }

            // Reset the text field and increment the counter
            suffixText = "";
            objectCount += 1;

            return newName;
        }

        /// <summary>
        /// Constructs any GameObject with only one component added.
        /// </summary>
        /// <param name="objectType">Type of object the name should be constructed.</param>
        /// <param name="objectCount">Amount of objects already created.</param>
        /// <param name="rootGameObject">The root GameObject, that the new
        /// GameObject should be attached to.</param>
        /// <param name="componentToAdd">The component to add to the newly created
        /// GameObject. By default, it will not add any components.</param>
        /// <param name="shouldBeFocused">Whether the newly created GameObject
        /// should be selected or not. By default, it will get selected.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="objectType"/> or
        /// <paramref name="rootGameObject"/> are not defined or invalid.</exception>
        internal static void CreateGameObject
            (string objectType,
             ref int objectCount,
             ref string suffixText,
             GameObject rootGameObject,
             System.Type componentToAdd = null,
             bool shouldBeFocused = true)
        {
            // Throw exception, if objectType is empty or null
            if (string.IsNullOrEmpty(objectType))
            {
                throw new ArgumentException($"{nameof(objectType)} string must neither be empty nor null");
            }

            // Throw exception, if rootGameObject is null
            if (rootGameObject == null)
            {
                throw new ArgumentNullException(nameof(rootGameObject), $"{nameof(rootGameObject)} cannot be null.");
            }

            string objectName = GetNewObjectName(objectType, ref objectCount, ref suffixText);

            GameObject newObject = componentToAdd == null ? new GameObject(objectName) : new GameObject(objectName, componentToAdd);
            // Create a new GameObject, and place it in under the correct GameObject
            newObject.transform.SetParent(rootGameObject.transform);

            // Select newly created GameObject
            if (shouldBeFocused)
            {
                Selection.activeGameObject = newObject;
            }

            // Sets the new Objects hideFlags to not Save into a build
            newObject.tag = Tags.EditorOnly;
        }

        #endif
    }
}
