using SEE.GO;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Allows a user to select the kind of environment in which the game
    /// runs: (1) desktop with keyboard and mouse input, (2) touch devices
    /// or gamepads using InControl, (3) virtual reality, or (4) augmented
    /// reality.
    /// </summary>
    [Obsolete]
    public class PlayerSettings : MonoBehaviour
    {
        /// <summary>
        /// The name of the game object holding the unique <see cref="PlayerSettings"/>
        /// component.
        /// </summary>
        private const string NameOfPlayerSettingsGameObject = "Player Settings";

        //-----------------------------------------------
        // Attributes that can be configured by the user.
        //-----------------------------------------------

        [Tooltip("What kind of player type should be enabled.")]
        [OdinSerialize]
        public PlayerInputType playerInputType = PlayerInputType.DesktopPlayer;

        /// <summary>
        /// The cached player settings within this local instance of Unity.
        /// Will be updated by <see cref="GetPlayerSettings"/> on its first call.
        /// </summary>
        private static PlayerSettings instance;

        /// <summary>
        /// The cached player input type within this local instance of Unity.
        /// Will be updated by <see cref="GetInputType"/> upon its first call.
        /// </summary>
        private static PlayerInputType localPlayerInputType = PlayerInputType.None;

        /// <summary>
        /// Sets <see cref="instance"/> to the <see cref="PlayerSettings"/> component
        /// retrieved from the current scene. Its game object is declared to be kept
        /// alive across scenes.
        ///
        /// Precondition: The scene must contain a game object named
        /// <see cref="NameOfPlayerSettingsGameObject"/> holding a
        /// <see cref="PlayerSettings"/> component.
        /// </summary>
        private void Start()
        {
            // Note: instance = FindObjectOfType<PlayerSettings>() would not work
            // because FindObjectOfType does not work when changing scenes.

            GameObject playerSettings = GameObject.Find(NameOfPlayerSettingsGameObject);
            if (playerSettings == null)
            {
                Debug.LogError($"There is no game object with name {NameOfPlayerSettingsGameObject}. This is a fatal error.\n");
            }
            else if (playerSettings.TryGetComponent(out instance))
            {
                // We will keep the game object of instance alive across scenes.
                DontDestroyOnLoad(instance.gameObject);
            }
            else
            {
                Debug.LogError($"There is no game object with a {typeof(PlayerSettings)} component. This is a fatal error.\n");
            }
        }

        /// <summary>
        /// The player input type within this local instance of Unity.
        /// </summary>
        /// <returns>player input type</returns>
        //public static PlayerInputType GetInputType()
        //{
        //    if (localPlayerInputType == PlayerInputType.None)
        //    {
        //        localPlayerInputType = GetPlayerSettings().playerInputType;
        //    }
        //    return localPlayerInputType;
        //}

        /// <summary>
        /// The player settings within this local instance of Unity.
        /// </summary>
        /// <returns>player settings</returns>
        public static PlayerSettings GetPlayerSettings()
        {
            return instance;
        }
    }
}
