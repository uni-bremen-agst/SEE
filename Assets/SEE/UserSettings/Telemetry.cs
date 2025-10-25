using SEE.Tools.OpenTelemetry;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.User
{
    /// <summary>
    /// Represents the telemetry configuration for the application, including the mode of operation  and the server
    /// endpoint for telemetry data. Provides functionality to save and restore  telemetry settings.
    /// </summary>
    /// <remarks>The <see cref="Telemetry"/> class allows configuring telemetry behavior, such as whether
    /// telemetry is disabled, stored locally, or sent to a remote server. It also supports saving and restoring these
    /// settings using a configuration writer or attribute dictionary.</remarks>
    [System.Serializable]
    internal class Telemetry
    {
        [Tooltip("The current telemetry mode (Disabled: no telemetry, Local: data are stored locally, Remote: data are sent to server).")]
        [ShowInInspector]
        public TelemetryMode Mode = TelemetryMode.Local;

        [Tooltip("Custom server endpoint for sending telemetry data (used in remote mode).")]
        [ShowInInspector]
        public string ServerURL = "http://stvr2.informatik.uni-bremen:4318/v1/traces";

        #region Configuration I/O

        /// <summary>
        /// Represents the label for the <see cref="Mode"/> attribute in the configuration file.
        /// </summary>
        private const string modeLabel = "Mode";

        /// <summary>
        /// Represents the label for the <see cref="ServerURL"/> attribute in the configuration file.
        /// </summary>
        private const string serverURL = "ServerURL";

        /// <summary>
        /// Saves the settings of this network configuration using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        /// <param name="label">the label under which to group the settings</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Mode.ToString(), modeLabel);
            writer.Save(ServerURL, serverURL);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        /// <param name="label">the label under which to look up the settings in <paramref name="attributes"/></param>
        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, modeLabel, ref Mode);
                ConfigIO.Restore(values, serverURL, ref ServerURL);
            }
        }

        #endregion Configuration I/O
    }
}
