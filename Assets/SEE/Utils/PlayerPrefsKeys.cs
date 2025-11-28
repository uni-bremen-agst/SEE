using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Provides constant string keys for accessing values stored via <see cref="PlayerPrefs"/>
    /// </summary>
    /// <remarks>
    /// These keys are used when reading or writing persistent user settings,
    /// for example via <see cref="PlayerPrefs.GetString(string, string)"/> or
    /// <see cref="PlayerPrefs.SetString(string, string)"/>.
    ///
    /// Centralizing the keys in this class avoids typos and ensures consistent
    /// usage throughout the project.
    /// </remarks>
    public class PlayerPrefsKeys
    {
        /// <summary>
        /// Key for storing and retrieving the LiveKit server URL.
        /// </summary>
        public const string LiveKitURL = "LiveKitURL";

        /// <summary>
        /// Key for storing and retrieving the token server URL.
        /// </summary>
        public const string TokenURL = "TokenURL";

        /// <summary>
        /// Key for storing and retrieving the LiveKit room name.
        /// </summary>
        public const string RoomName = "RoomName";

        /// <summary>
        /// Key for storing and retrieving the selected camera.
        /// </summary>
        public const string WebcamDevice = "WebcamDevice";
    }
}