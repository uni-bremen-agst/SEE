using SEE.UI;
using Sirenix.OdinInspector;
using System;
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
        public string Webcam { get; set; } =
            WebCamTexture.devices.Length > 0 ?
            WebCamTexture.devices[0].name
                : string.Empty;
    }
}