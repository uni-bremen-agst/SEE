using DG.Tweening;
using OpenAI.Realtime;
using SEE.GO;
using SEE.Net;
using SEE.Tools.OpenTelemetry;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace SEE.User
{
    /// <summary>
    /// Represents the user settings for the SEE application. These are attributes
    /// that are generally set by the user at the start of the application.
    /// </summary>
    /// <remarks>This component is assumed to be attached to a game object in the
    /// start scene. The user can select the environment in the Unity editor.
    /// The selection can also be made during run-time.</remarks>
    internal class UserSettings : MonoBehaviour
    {
        /// <summary>
        /// Settings of the player.
        /// </summary>
        [Tooltip("Settings of the player.")]
        public readonly Player Player = new();

        /// <summary>
        /// Settings of the network.
        /// </summary>
        [Tooltip("Settings of the network.")]
        public readonly Network Network = new();

        /// <summary>
        /// The voice chat system as selected by the user. Note: This attribute
        /// can be changed in the editor via <see cref="NetworkEditor"/> as well
        /// as at the start up in the <see cref="OpeningDialog"/>.
        /// </summary>
        [Tooltip("The voice chat system to be used. 'None' for no voice chat.")]
        public VoiceChatSystems VoiceChat = VoiceChatSystems.None;

        /// <summary>
        /// The kind of environment the game is running (Desktop, VR, etc).
        /// </summary>
        [Tooltip("The kind of environment the game is running (Desktop, VR, etc).")]
        public PlayerInputType InputType = PlayerInputType.DesktopPlayer;

        /// <summary>
        /// Settings for telemetry.
        /// </summary>
        [Tooltip("Telemetry settings.")]
        public readonly Telemetry Telemetry = new();

        /// <summary>
        /// Settings for video.
        /// </summary>
        [Tooltip("Video settings.")]
        public readonly Video Video = new();

        /// <summary>
        /// Settings for audio.
        /// </summary>
        [Tooltip("Audio settings.")]
        public readonly Audio Audio = new();

        /// <summary>
        /// Default path of the configuration file (path and filename).
        /// </summary>
        [PropertyTooltip("Path of the file containing the settings.")]
        [OdinSerialize, HideReferenceObjectPicker]
        public DataPath ConfigPath = new();

        /// <summary>
        /// Backing field of <see cref="Instance"/>.
        /// </summary>
        private static UserSettings instance;

        /// <summary>
        /// The single unique instance of the user settings.
        /// There can be only one.
        /// </summary>
        public static UserSettings Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                instance = FindAnyObjectByType<UserSettings>();
                if (instance == null)
                {
                    Debug.LogError($"There is no {typeof(UserSettings)} component in the current scene!\n");
                }
                return instance;
            }
        }

        /// <summary>
        /// Sets <see cref="MainThread"/> to the current thread and loads the user settings
        /// from the configuration file.
        /// </summary>
        private void Awake()
        {
            /// The field <see cref="MainThread"/> is supposed to denote Unity's main thread.
            /// The <see cref="Awake"/> function is guaranteed to be executed by Unity's main
            /// thread, that is, <see cref="Thread.CurrentThread"/> represents Unity's
            /// main thread here.
            MainThread = Thread.CurrentThread;

            // Sets the Unity-dependent default values.
            Video.InitializeDefaults();

            Load();
        }

        /// <summary>
        /// Initializes the application by setting up scene loading callbacks, configuring default animation easing, and
        /// initializing the network.
        /// </summary>
        private void Start()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            DOTween.defaultEaseType = Ease.OutExpo;

            Network.SetUp();
        }

        /// <summary>
        /// Starts the voice-chat system selected. Unregisters itself from
        /// <see cref="SceneManager.sceneLoaded"/>.
        /// Note: This method is assumed to be called when the new scene is fully loaded.
        /// </summary>
        /// <param name="scene">Scene that was loaded.</param>
        /// <param name="mode">The mode in which the scene was loaded.</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Now we have loaded the scene that is supposed to contain settings for the voice chat
            // system. We can now turn on the voice chat system.
            Debug.Log($"Loaded scene {scene.name} in mode {mode}.\n");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            User.VoiceChat.StartVoiceChat(VoiceChat);
        }

        /// <summary>
        /// Shuts down the voice-chat system and OpenTelemetry,
        /// and saves all user settings.
        /// This ensures that any changes made during the session are persisted
        /// when the application quits.
        /// </summary>
        private void OnApplicationQuit()
        {
            TracingHelperService.Shutdown(true);
            User.VoiceChat.EndVoiceChat(VoiceChat);
        }

        /// <summary>
        /// Registers the quit callback when the object becomes enabled.
        /// This ensures that application shutdown can be handled gracefully.
        /// </summary>
        private void OnEnable()
        {
            Application.wantsToQuit += SaveOnQuit;
        }

        /// <summary>
        /// Unregisters the quit callback when the object is disabled.
        /// This prevents callbacks from being invoked on inactive objects.
        /// </summary>
        private void OnDisable()
        {
            Application.wantsToQuit -= SaveOnQuit;
        }

        /// <summary>
        /// Called when the application is about to quit.
        /// Attempts to save the current instance state before shutdown.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> to allow the application to quit.
        /// </returns>
        private bool SaveOnQuit()
        {
            try
            {
                Instance.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during quit: {e}\n");
            }
            return true;
        }

        /// <summary>
        /// The Unity main thread. Note that we cannot initialize its value here
        /// because the elaboration code initializing static attributes may be
        /// executed by a thread different from Unity's main thread. This attribute
        /// will be initialized in <see cref="Awake"/> for this reason.
        /// </summary>
        private static Thread mainThread = null;
        /// <summary>
        /// Contains the Unity main thread of the application.
        /// </summary>
        public static Thread MainThread
        {
            get
            {
                Assert.IsNotNull(mainThread, "The main Unity thread must not have been determined as of now!");
                return mainThread;
            }
            private set
            {
                Assert.IsNotNull(value, "The main Unity thread must not be null!");
                if (mainThread != value)
                {
                    Assert.IsNull(mainThread, "The main Unity thread has already been determined!");
                    mainThread = value;
                }
            }
        }

        /// <summary>
        /// True if the user is using a VR headset.
        /// </summary>
        public static bool IsVR => Instance.InputType == PlayerInputType.VRPlayer;

        /// <summary>
        /// True if the user is using a desktop computer.
        /// </summary>
        public static bool IsDesktop => Instance.InputType == PlayerInputType.DesktopPlayer;

        /// <summary>
        /// The backend domain to be used for network connections to the SEE backend.
        /// </summary>
        public static string BackendDomain => Instance?.Network.BackendDomain;

        /// <summary>
        /// The complete backend server API endpoint to be used for network connections to
        /// the SEE backend.
        /// </summary>
        public static string BackendServerAPI => Instance?.Network.BackendServerAPI;

        /// <summary>
        /// The name of the group for the Inspector buttons loading and saving the configuration file.
        /// </summary>
        private const string configurationButtonsGroup = "ConfigurationButtonsGroup";

        /// <summary>
        /// Saves the settings of this network configuration to <see cref="ConfigPath()"/>.
        /// If the configuration file exists already, it will be overridden.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [PropertyTooltip("Saves the user settings in a configuration file.")]
        [ButtonGroup(configurationButtonsGroup)]
        public void Save()
        {
            Save(ConfigPath.Path);
        }

        /// <summary>
        /// Loads the settings of this network configuration from <see cref="ConfigPath()"/>
        /// if it exists. If it does not exist, nothing happens.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [PropertyTooltip("Loads the user configuration file.")]
        [ButtonGroup(configurationButtonsGroup)]
        public void Load()
        {
            Load(ConfigPath.Path);
        }

        /// <summary>
        /// Saves the settings of this network configuration to <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Name of the file in which the settings are stored.</param>
        public void Save(string filename)
        {
            using ConfigWriter writer = new(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this network configuration from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Name of the file from which the settings are restored.</param>
        private void Load(string filename)
        {
            if (File.Exists(filename))
            {
                Debug.Log($"Loading user settings from {filename}.\n");
                using ConfigReader stream = new(filename);
                Restore(stream.Read());
            }
            else
            {
                Debug.LogError($"User settings file {filename} does not exist.\n");
            }
        }

        #region Configuration I/O
        /// <summary>
        /// Label of attribute <see cref="Player"/> in the configuration file.
        /// </summary>
        private const string playerLabel = "Player";

        /// <summary>
        /// Label of attribute <see cref="Network"/> in the configuration file.
        /// </summary>
        private const string networkLabel = "Network";

        /// <summary>
        /// Label of attribute <see cref="User.VoiceChat"/> in the configuration file.
        /// </summary>
        private const string voiceChatLabel = "VoiceChat";

        /// <summary>
        /// Label of attribute <see cref="Telemetry"/> in the configuration file.
        /// </summary>
        private const string telemetryLabel = "Telemetry";

        /// <summary>
        /// Label of attribute <see cref="InputType"/> in the configuration file.
        /// </summary>
        private const string inputTypeLabel = "InputType";

        /// <summary>
        /// Label of attribute <see cref="Video"/> in the configuration file.
        /// </summary>
        private const string videoLabel = "Video";

        /// <summary>
        /// Label of attribute <see cref="Audio"/> in the confiugration file.
        /// </summary>
        private const string audioLabel = "Audio";

        /// <summary>
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to be used to save the settings.</param>
        protected virtual void Save(ConfigWriter writer)
        {
            Player.Save(writer, playerLabel);
            writer.Save(VoiceChat.ToString(), voiceChatLabel);
            Telemetry.Save(writer, telemetryLabel);
            writer.Save(InputType.ToString(), inputTypeLabel);
            Video.Save(writer, videoLabel);
            Audio.Save(writer, audioLabel);
            try
            {
                Network.Save(writer, networkLabel);
            }
            catch (System.Exception)
            {
                Debug.LogError("Network settings could not be saved.\n");
                throw;
            }
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes from which to restore the settings.</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            Player.Restore(attributes, playerLabel);
            ConfigIO.RestoreEnum(attributes, voiceChatLabel, ref VoiceChat);
            Telemetry.Restore(attributes, telemetryLabel);
            ConfigIO.RestoreEnum(attributes, inputTypeLabel, ref InputType);
            Video.Restore(attributes, videoLabel);
            Audio.Restore(attributes, audioLabel);
            Network.Restore(attributes, networkLabel);
        }

        #endregion Configuration I/O
    }
}
