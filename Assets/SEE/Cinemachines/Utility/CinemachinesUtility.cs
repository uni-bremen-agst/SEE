using System;
// using System.Text;
using System.Collections.Generic;
// using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
// using UnityEngine.Timeline;
// using UnityEngine.Playables;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace SEE.Cinemachines.Utility
{
    /// <summary>
    /// Static Utility-Class for general functions shared between the custom Cinemachines Components.
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
        /// Returns the active CinemachineRoots-Transform, if one exists in the current Scene.
        /// </summary>
        /// <returns>Returns the active Transform, that includes the Cinemachines-Root Component, or null, of none is found.</returns>
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
            // Create a new CinemachinesRoot at the root of the scene
            new GameObject(CinemachinesRootName, typeof(CinemachinesRoot));
        }

        /// <summary>
        /// Helper Function to generate the "Scene Deletion Warning"-Message, including the Path to the respective Folder.
        /// </summary>
        /// <param name="guid">The GUID of the Folder for the corresponding Cinemachines-Scene.</param>
        /// <returns>The Message generated for the Cinemachines-Scene.</returns>
        internal static string GetSceneDeletionWarningMessage(string guid)
        {
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
            string SceneGUID = AssetDatabase.CreateFolder($"{CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}", $"{sceneName}");
            // create Signals folder to store Timeline Signals
            if (!AssetDatabase.IsValidFolder($"{AssetDatabase.GUIDToAssetPath(SceneGUID)}/Signals"))
            {
                AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(SceneGUID), "Signals");
            }

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
            // Add the CinemachinesScenes Component to the newly created Scene GameObject
            scene.GetComponent<CinemachinesScene>().SceneGUID = GenerateSceneFolder(sceneName);
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
            if (string.IsNullOrEmpty(objectType))
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
        internal static void CreateGameObject(string objectType, ref int objectCount, ref string suffixText,
                                             GameObject rootGameObject, System.Type componentToAdd = null, bool shouldBeFocused = true)
        {
            // Throw exception, if objectType is empty or null
            if (string.IsNullOrEmpty(objectType))
            {
                throw new ArgumentException("objectType string cannot be empty or null.");
            }

            // Throw exception, if rootGameObject is null
            if (rootGameObject == null)
            {
                throw new ArgumentNullException("rootGameObject cannot be null.");
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
        }

        #endif
    }
}
