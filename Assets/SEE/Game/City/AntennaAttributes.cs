using System;
using System.Collections.Generic;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Specifies how metrics are to be rendered as an antenna above the blocks.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public sealed class AntennaAttributes : ConfigIO.IPersistentConfigItem
    {
        /// <summary>
        /// This parameter determines the sections of the antenna.
        /// </summary>
        [SerializeField]
        [HideReferenceObjectPicker]
        public IList<string> AntennaSections = new List<string>();

        /// <summary>
        /// Saves the settings in the configuration file.
        ///
        /// Implements <see cref="ConfigIO.IPersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="writer">To be used for writing the settings.</param>
        /// <param name="label">The outer label grouping the settings.</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(AntennaSections, antennaSectionsLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        ///
        /// Implements <see cref="ConfigIO.IPersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">Dictionary of attributes from which to retrieve the settings.</param>
        /// <param name="label">The label for the settings (a key in <paramref name="attributes"/>).</param>
        /// <returns>True if at least one attribute was successfully restored.</returns>
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                bool result = false;
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.RestoreStringList(values, antennaSectionsLabel, ref AntennaSections);
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Label in the configuration file for the antenna sections.
        /// </summary>
        private const string antennaSectionsLabel = "AntennaSections";
    }
}
