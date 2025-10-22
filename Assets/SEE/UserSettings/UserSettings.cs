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

namespace SEE.UserSettings
{

    internal class UserSettings : MonoBehaviour
    {
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
            Network.Start();
        }

        /// <summary>
        /// Shuts down the voice-chat system and OpenTelemetry.
        /// </summary>
        private void OnApplicationQuit()
        {
            TracingHelperService.Shutdown(true);
            Network.EndVoiceChat();
        }

        public Network Network = new();

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
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        protected virtual void Save(ConfigWriter writer)
        {
            Network.Save(writer);
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        protected virtual void Restore(Dictionary<string, object> attributes)
        {
            Network.Restore(attributes);
        }
    }
}
