using SEE.Game;
using SEE.UI;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.User
{
    /// <summary>
    /// Represents the video settings for the SEE application. These are attributes
    /// that are generally set by the user at the <see cref="SettingsMenu"/> of the application.
    /// </summary>
    [Serializable]
    internal class Video
    {
        /// <summary>
        /// Name of the last used webcam.
        /// </summary>
        [Tooltip("The name of the last used webcam."), ShowInInspector]
        public string WebcamName { get; set; } = string.Empty;

        /// <summary>
        /// The WebSocket URL of the LiveKit server to connect to.
        /// </summary>
        [Tooltip("The WebSocket URL of the LiveKit server to connect to."), ShowInInspector]
        public string LiveKitUrl { get; set; } = "ws://localhost:7880";

        /// <summary>
        /// The URL used to fetch the access token required for authentication.
        /// </summary>
        [Tooltip("The URL used to fetch the access token required for authentication."), ShowInInspector]
        public string TokenUrl { get; set; } = "http://localhost:3000";

        /// <summary>
        /// The room name to join in LiveKit.
        /// </summary>
        [Tooltip("The room name to join in LiveKit."), ShowInInspector]
        public string RoomName { get; set; } = "development";

        /// <summary>
        /// Initializes Unity-dependent default values of the video settings.
        /// If <see cref="WebcamName"/> is empty and at least one webcam is available,
        /// it will automatically be set to the first detected webcam.
        /// </summary>
        public void InitializeDefaults()
        {
            if (string.IsNullOrEmpty(WebcamName))
            {
                WebCamDevice[] devices = WebCamTexture.devices;
                if (devices.Length > 0)
                {
                    WebcamName = devices[0].name;
                }
            }
        }

        /// <summary>
        /// Updates the LiveKit-related settings stored in this <see cref="Video"/> instance.
        /// This method sets the server URL, token URL, and room name, which will be used
        /// when connecting via the <see cref="LiveKitVideoManager"/> or when displaying
        /// these values in the settings UI.
        ///
        /// After updating the values, this method automatically calls <see cref="UpdateSettingsMenu"/>
        /// to refresh the LiveKit input fields in the settings menu.
        /// </summary>
        /// <param name="liveKitUrl">The WebSocket URL of the LiveKit server.</param>
        /// <param name="tokenUrl">The URL of the token server used to fetch access tokens.</param>
        /// <param name="roomName">The name of the LiveKit room to join.</param>
        public void UpdateLiveKitSettings(string liveKitUrl, string tokenUrl, string roomName)
        {
            LiveKitUrl = liveKitUrl;
            TokenUrl = tokenUrl;
            RoomName = roomName;

            UpdateSettingsMenu();
        }

        /// <summary>
        /// Notifies the settings menu to refresh its LiveKit-related input fields
        /// so that they reflect the current values stored in this <see cref="Video"/> instance.
        /// </summary>
        private void UpdateSettingsMenu()
        {
            if (LocalPlayer.TryGetSettingsMenu(out SettingsMenu menu))
            {
                menu.UpdateLiveKitSettings();
            }
        }

        #region Configuration I/O

        /// <summary>
        /// Label of attribute <see cref="WebcamName"/> in the configuration file.
        /// </summary>
        private const string webcamNameLabel = "WebcamName";

        /// <summary>
        /// Label of attribute <see cref="LiveKitUrl"/> in the configuration file.
        /// </summary>
        private const string liveKitUrlLabel = "LiveKitURL";

        /// <summary>
        /// Label of attribute <see cref="TokenUrl"/> in the configuration file.
        /// </summary>
        private const string tokenUrlLabel = "TokenURL";

        /// <summary>
        /// Label of attribute <see cref="RoomName"/> in the configuration file.
        /// </summary>
        private const string roomNameLabel = "RoomName";

        /// <summary>
        /// Label of the LiveKit URL Environment Variable name.
        /// </summary>
        private const string liveKitUrlVariableName = "SEE_LIVEKIT_URL";

        /// <summary>
        /// Label of the LiveKit room Environment Variable name.
        /// </summary>
        private const string liveKitRoomNameVariableName = "SEE_LIVEKIT_ROOM_NAME";

        /// <summary>
        /// Saves the settings of this <see cref="Video"/> using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to be used to save the settings.</param>
        /// <param name="label">The label under which to group the settings.</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(WebcamName, webcamNameLabel);
            writer.Save(LiveKitUrl, liveKitUrlLabel);
            writer.Save(TokenUrl, tokenUrlLabel);
            writer.Save(RoomName, roomNameLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes from which to restore the settings.</param>
        /// <param name="label">The label under which to look up the settings in <paramref name="attributes"/>.</param>
        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    string value = WebcamName;
                    ConfigIO.Restore(values, webcamNameLabel, ref value);
                    WebcamName = value;
                }
                {
                    string value = LiveKitUrl;
                    ConfigIO.Restore(values, liveKitUrlLabel, ref value);
                    LiveKitUrl = value;
                }
                {
                    string value = TokenUrl;
                    ConfigIO.Restore(values, tokenUrlLabel, ref value);
                    TokenUrl = value;
                }
                {
                    string value = RoomName;
                    ConfigIO.Restore(values, roomNameLabel, ref value);
                    RoomName = value;
                }
            }
        }

        /// <summary>
        /// Sets up the video configuration, by pulling the values from env variables.
        /// </summary>
        internal void SetUp()
        {
            string livekitUrl = Environment.GetEnvironmentVariable(liveKitUrlVariableName);
            if (!string.IsNullOrEmpty(livekitUrl))
            {
                LiveKitUrl = livekitUrl;
            }

            string roomName = Environment.GetEnvironmentVariable(liveKitRoomNameVariableName);
            if (!string.IsNullOrEmpty(roomName))
            {
                RoomName = roomName;
            }

        }
        #endregion
    }
}
