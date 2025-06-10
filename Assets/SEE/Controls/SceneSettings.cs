using System;
using System.IO;
using DG.Tweening;
using SEE.Game;
using SEE.GO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Enumeration for the different telemetry modes.
    /// </summary>
    public enum TelemetryMode
    {
        Disabled,
        Local,
        Remote
    }

    /// <summary>
    /// Manages the kind of environment (desktop or VR).
    /// </summary>
    /// <remarks>This component is assumed to be attached to a game object in the
    /// main scene. The user can select the environment in the Unity editor.
    /// The selection can also be made during run-time.</remarks>
    public class SceneSettings : MonoBehaviour
    {
        //-----------------------------------------------
        // Attributes that can be configured by the user.
        //-----------------------------------------------

        [Tooltip("The kind of environment the game is running (Desktop, VR, etc).")]
        [ShowInInspector]
        public static PlayerInputType InputType = PlayerInputType.DesktopPlayer;
        
        [Tooltip("The current telemetry mode.")]
        [ShowInInspector]
        public static TelemetryMode telemetryMode = TelemetryMode.Local;

        [Tooltip("Custom endpoint for telemetry export (used in remote mode).")]
        [ShowInInspector]
        public static string CustomTelemetryServerURL = "http://localhost:4317";


        /// <summary>
        /// Saves the current telemetry configuration (mode, remote URL, and local path)
        /// </summary>
        public static void SaveTelemetrySettings()
        {
            File.WriteAllText(Application.persistentDataPath + "/TelemetryMode.cfg", telemetryMode.ToString());
            File.WriteAllText(Application.persistentDataPath + "/TelemetryServerURL.cfg", CustomTelemetryServerURL);
        }

        /// <summary>
        /// Loads the telemetry configuration (mode, remote URL, and local path)
        /// </summary>
        public static void LoadTelemetrySettings()
        {
            string modePath = Application.persistentDataPath + "/TelemetryMode.cfg";
            if (File.Exists(modePath) &&
                Enum.TryParse(File.ReadAllText(modePath).Trim(), out TelemetryMode mode))
            {
                telemetryMode = mode;
            }

            string urlPath = Application.persistentDataPath + "/TelemetryServerURL.cfg";
            if (File.Exists(urlPath))
            {
                CustomTelemetryServerURL = File.ReadAllText(urlPath).Trim();
            }
        }

        /// <summary>
        /// The path of the configuration file in which to store the selected <see cref="InputType"/>.
        /// </summary>
        /// <returns>path of the configuration file</returns>
        private static string ConfigurationFilename()
        {
            return Application.persistentDataPath + "/SceneSettings.cfg";
        }

        /// <summary>
        /// Saves the <see cref="InputType"/> in a configuration file.
        /// </summary>
        internal static void Save()
        {
            File.WriteAllText(ConfigurationFilename(), InputType.ToString());
        }

        /// <summary>
        /// Restores the <see cref="InputType"/> from the configuration file.
        /// </summary>
        internal static void Load()
        {
            string path = ConfigurationFilename();
            if (File.Exists(path))
            {
                string input = File.ReadAllText(path).Trim();
                if (Enum.TryParse(input, out PlayerInputType playerInputType))
                {
                    Debug.Log($"Loaded environment {playerInputType} from configuration file {path}.\n");
                    InputType = playerInputType;
                }
                else
                {
                    Debug.LogError($"Configuration file {path} for the selected kind of environment (desktop, VR, etc.) does not contain the expected content. Using default {PlayerInputType.DesktopPlayer}.\n");
                    InputType = PlayerInputType.DesktopPlayer;
                }
            }
            else
            {
                Debug.Log($"Configuration file {path} for the selected kind of environment (desktop, VR, etc.) does not exist. Using default {PlayerInputType.DesktopPlayer}.\n");
                InputType = PlayerInputType.DesktopPlayer;
            }
        }

        /// <summary>
        /// The name of the game object holding the unique <see cref="SceneSettings"/>
        /// component.
        /// </summary>
        private const string nameOfSceneSettingsGameObject = "Scene Settings";

        /// <summary>
        /// The single instance of <see cref="SceneSettings"/> derived from the
        /// current scene.
        /// </summary>
        private static SceneSettings instance;

        /// <summary>
        /// The scene settings within this local instance of Unity.
        /// </summary>
        /// <returns>scene settings</returns>
        public static SceneSettings Settings()
        {
            return instance;
        }

        /// <summary>
        /// Sets <see cref="instance"/> to the <see cref="SceneSettings"/> component
        /// retrieved from the current scene. Its game object is declared to be kept
        /// alive across scenes.
        ///
        /// Precondition: The scene must contain a game object named
        /// <see cref="NameOfPlayerSettingsGameObject"/> holding a
        /// <see cref="SceneSettings"/> component.
        /// </summary>
        private static void SetInstance()
        {
            // Note: instance = FindObjectOfType<SceneSettings>() would not work
            // because FindObjectOfType does not work when changing scenes.

            GameObject seeSettings = GameObject.Find(nameOfSceneSettingsGameObject);
            if (seeSettings == null)
            {
                Debug.LogError($"There is no game object with name {nameOfSceneSettingsGameObject}. This is a fatal error.\n");
            }
            else if (!seeSettings.TryGetComponent(out instance))
            {
                Debug.LogError($"There is no game object with a {typeof(SceneSettings)} component. This is a fatal error.\n");
            }
        }

        /// <summary>
        /// Depending on the user's selection, turns VR mode on or off and activates/deactivates
        /// the game objects representing the player in the scene.
        /// </summary>
        private void Awake()
        {
            //InputType = PlayerSettings.GetInputType();
            Debug.Log($"Player input type: {InputType}.\n");
            VRStatus.Enable(InputType == PlayerInputType.VRPlayer);
        }

        /// <summary>
        /// If we are running in a VR environment and the user wants us to hide the controllers,
        /// we do so. Caches <see cref="SceneSettings.InputType"/> in <see cref="InputType"/>.
        /// </summary>
        private void Start()
        {
            SetDoTweenSettings();
            SetInstance();
        }

        /// <summary>
        /// Sets the DOTween settings for newly created tweens, such as the ease type.
        /// </summary>
        private static void SetDoTweenSettings()
        {
            DOTween.defaultEaseType = Ease.OutExpo;
        }
    }
}
