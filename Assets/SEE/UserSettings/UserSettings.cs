using SEE.Net;
using SEE.Tools.OpenTelemetry;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
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
    /// main scene. The user can select the environment in the Unity editor.
    /// The selection can also be made during run-time.</remarks>
    internal class UserSettings : MonoBehaviour
    {
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
            private set { instance = value; }
        }

        private void Awake()
        {
            /// The field <see cref="MainThread"/> is supposed to denote Unity's main thread.
            /// The <see cref="Awake"/> function is guaranteed to be executed by Unity's main
            /// thread, that is, <see cref="Thread.CurrentThread"/> represents Unity's
            /// main thread here.
            MainThread = Thread.CurrentThread;

            Load();
        }

        private void Start()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Network.SetUp();
        }

        /// <summary>
        /// The voice chat system as selected by the user. Note: This attribute
        /// can be changed in the editor via <see cref="NetworkEditor"/> as well
        /// as at the start up in the <see cref="OpeningDialog"/>.
        /// </summary>
        [Tooltip("The voice chat system to be used. 'None' for no voice chat."), FoldoutGroup(voiceChatFoldoutGroup)]
        public VoiceChatSystems VoiceChat = VoiceChatSystems.None;

        /// <summary>
        /// Name of the Inspector foldout group for the logging setttings.
        /// </summary>
        private const string voiceChatFoldoutGroup = "Voice Chat";

        /// <summary>
        /// Starts the voice-chat system selected. Unregisters itself from
        /// <see cref="SceneManager.sceneLoaded"/>.
        /// Note: This method is assumed to be called when the new scene is fully loaded.
        /// </summary>
        /// <param name="scene">scene that was loaded</param>
        /// <param name="mode">the mode in which the scene was loaded</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Now we have loaded the scene that is supposed to contain settings for the voice chat
            // system. We can now turn on the voice chat system.
            Debug.Log($"Loaded scene {scene.name} in mode {mode}.\n");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            User.VoiceChat.StartVoiceChat(VoiceChat);
        }

        /// <summary>
        /// Shuts down the voice-chat system and OpenTelemetry.
        /// </summary>
        private void OnApplicationQuit()
        {
            TracingHelperService.Shutdown(true);
            User.VoiceChat.EndVoiceChat(VoiceChat);
        }

        /// <summary>
        /// Settings of the network.
        /// </summary>
        [Tooltip("Settings of the network."), FoldoutGroup("Network")]
        public Network Network = new();

        /// <summary>
        /// Settings of the player.
        /// </summary>
        [Tooltip("Settings of the player."), FoldoutGroup("Player")]
        public Player Player = new();

        /// <summary>
        /// </summary>
        [Tooltip("Telemetry settings."), FoldoutGroup("Telemetry")]
        public Telemetry Telemetry = new();

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
        /// The name of the group for the fold-out group of the configuration file.
        /// </summary>
        private const string configurationFoldoutGroup = "Configuration File";

        /// <summary>
        /// The name of the group for the Inspector buttons loading and saving the configuration file.
        /// </summary>
        private const string configurationButtonsGroup = "ConfigurationButtonsGroup";

        /// <summary>
        /// Default path of the configuration file (path and filename).
        /// </summary>
        [PropertyTooltip("Path of the file containing the network configuration.")]
        [OdinSerialize, HideReferenceObjectPicker, FoldoutGroup(configurationFoldoutGroup)]
        public DataPath ConfigPath = new();

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
        /// <param name="filename">name of the file in which the settings are stored</param>
        public void Save(string filename)
        {
            using ConfigWriter writer = new(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this network configuration from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">name of the file from which the settings are restored</param>
        public void Load(string filename)
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
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        protected virtual void Save(ConfigWriter writer)
        {
            Network.Save(writer, networkLabel);
            writer.Save(VoiceChat.ToString(), voiceChatLabel);
            Telemetry.Save(writer, telemetryLabel);
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            Network.Restore(attributes, networkLabel);
            ConfigIO.RestoreEnum(attributes, voiceChatLabel, ref VoiceChat);
            Telemetry.Restore(attributes, telemetryLabel);
        }
    }
}
