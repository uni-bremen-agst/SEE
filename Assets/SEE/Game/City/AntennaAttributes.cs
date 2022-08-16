using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Specifies how metrics are to be rendered as an antenna above the blocks.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public sealed class AntennaAttributes : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// This parameter determines the sections of the antenna.
        /// </summary>
        [SerializeField]
        [HideReferenceObjectPicker]
        public IList<string> AntennaSections = new List<string>();

        /// <summary>
        /// The width of an antenna.
        /// </summary>
        public float AntennaWidth = 0.1f;

        /// <summary>
        /// Saves the settings in the configuration file.
        ///
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(AntennaWidth, AntennaWidthLabel);
            writer.Save(AntennaSections, AntennaSectionsLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        ///
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        /// <returns>true if at least one attribute was successfully restored</returns>
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                bool result = false;
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.Restore(values, AntennaWidthLabel, ref AntennaWidth);
                ConfigIO.RestoreStringList(values, AntennaSectionsLabel, ref AntennaSections);
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Label in the configuration file for the antenna width.
        /// </summary>
        private const string AntennaWidthLabel = "Width";

        /// <summary>
        /// Label in the configuration file for the antenna sections.
        /// </summary>
        private const string AntennaSectionsLabel = "AntennaSections";
    }
}
