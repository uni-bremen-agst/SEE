using Sirenix.Serialization;
using SEE.Game;
using SEE.Game.Charts.VR;
using SEE.GO;
using SEE.Utils;
using System;
using DG.Tweening;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Sirenix.OdinInspector;
using System.IO;

namespace SEE.Controls
{
    public class SceneSettings : MonoBehaviour
    {
        //----------------------------------------------------------------------------------
        // Names of game objects present in the scene (possibly depending on the environment
        // we are running in).
        //----------------------------------------------------------------------------------

        /// <summary>
        /// The name of the game object where the ChartManager component and his friends are
        /// attached to. It is used for handling the metric charts.
        /// </summary>
        private const string ChartManagerName = "ChartManager";

        //-----------------------------------------------
        // Attributes that can be configured by the user.
        //-----------------------------------------------

        [Tooltip("The kind of environment the game is running (Desktop, VR, etc).")]
        [ShowInInspector]
        public static PlayerInputType InputType = PlayerInputType.DesktopPlayer;

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
                    SceneSettings.InputType = playerInputType;

                    // FIXME: We use desktop during the opening dialog whatever the user set earlier
                    // because the GUI for the opening dialog is implemented only for desktop.
                    Debug.LogWarning($"Because of partial GUI implementation, we will use {PlayerInputType.DesktopPlayer}.\n");
                    SceneSettings.InputType = PlayerInputType.DesktopPlayer;
                }
                else
                {
                    Debug.LogError($"Configuration file {path} for the selected kind of environment (desktop, VR, etc.) does not contain the expected content. Using default {PlayerInputType.DesktopPlayer}.\n");
                    playerInputType = PlayerInputType.DesktopPlayer;
                }
            }
            else
            {
                Debug.Log($"Configuration file {path} for the selected kind of environment (desktop, VR, etc.) does not exist. Using default {PlayerInputType.DesktopPlayer}.\n");
                InputType = PlayerInputType.DesktopPlayer;
            }
        }

        [Tooltip("The plane the player is to focus initially in the desktop environment.")]
        [ShowInInspector]
        private GO.Plane FocusPlane;

        [Tooltip("The factor by which code cities should be scaled on startup."), OdinSerialize, Min(0.01f)]
        public float CityScalingFactor = 1f;

        /// <summary>
        /// The name of the game object holding the unique <see cref="SceneSettings"/>
        /// component.
        /// </summary>
        private const string NameOfSceneSettingsGameObject = "Scene Settings";

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

            GameObject seeSettings = GameObject.Find(NameOfSceneSettingsGameObject);
            if (seeSettings == null)
            {
                Debug.LogError($"There is no game object with name {NameOfSceneSettingsGameObject}. This is a fatal error.\n");
            }
            else if (!seeSettings.TryGetComponent(out instance))
            {
                Debug.LogError($"There is no game object with a {typeof(SceneSettings)} component. This is a fatal error.\n");
            }
        }

        private static GameObject localPlayer;
        /// <summary>
        /// The game object representing the active local player, that is, the player
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                {
                    localPlayer = instance.CreatePlayer(InputType);
                }
                return localPlayer;
            }
            private set
            {
                if (LocalPlayer == null)
                {
                    localPlayer = value;
                }
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
#if UNITY_EDITOR
            KeyBindings.PrintBindings();
#endif
        }

        /// <summary>
        /// Sets the DOTween settings for newly created tweens, such as the ease type.
        /// </summary>
        private static void SetDoTweenSettings()
        {
            DOTween.defaultEaseType = Ease.OutExpo;
        }

        /// <summary>
        /// Finds or creates the kind of player required for the given <paramref name="inputType"/>.
        /// For some players, additional game objects and/or components will be added to the
        /// scene required for the particular player to work correctly.
        /// </summary>
        /// <param name="inputType">the kind of environment the player is to run</param>
        /// <returns>new player for given <paramref name="inputType"/>, <c>null</c>
        /// if <paramref name="inputType"/> equals <see cref="PlayerInputType.DesktopPlayer"/></returns>
        [Obsolete("Do not use. This method will be superseded by SEE.Game.Avatars.AvatarAdapter.")]
        private GameObject CreatePlayer(PlayerInputType inputType)
        {
            GameObject player;

            switch (inputType)
            {
                case PlayerInputType.DesktopPlayer:
                    // position and rotation of the local desktop player are set
                    // elsewhere by the player spawner. That is why we can return
                    // this local player immediately.
                    return LocalPlayer();
                case PlayerInputType.VRPlayer:
                    {
                        /// FIXME: Move this code to <see cref="SEE.Game.Avatars.AvatarAdapter"/>.
                        player = LocalPlayer();
                    }
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    /// FIXME: Move this code to <see cref="SEE.Game.Avatars.AvatarAdapter"/>.
                    player = LocalPlayer();
                    break;
                case PlayerInputType.None: return null; // No player needs to be created
                default:
                    throw new NotImplementedException($"Unhandled case {inputType}.");
            }
            return player;

            static GameObject LocalPlayer()
            {
                // The local player is holding the main camera. Remote players do not have
                // a camera attached. Hence, we only need to retrieve that camera.

                /// FIXME: This should be the case for all environments as soon as we
                /// migrated <see cref="CreatePlayer(PlayerInputType)"/> to
                /// <see cref="Game.Avatars.AvatarAdapter"/>
                return InputType == PlayerInputType.DesktopPlayer ?
                    MainCamera.Camera.gameObject : null;
            }
        }
    }
}
