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
        [ListDrawerSettings(CustomAddFunction = nameof(AddSegment))]
        public List<AntennaSection> AntennaSections = new List<AntennaSection>();

        /// <summary>
        /// Returns a new <see cref="AntennaSection"/> with default values.
        ///
        /// Note: This method is used as a custom function for additions in <see cref="AntennaSections"/>.
        /// </summary>
        /// <returns>new <see cref="AntennaSection"/> with default values</returns>
        private AntennaSection AddSegment()
        {
            AntennaSection result = new AntennaSection();
            result.Color = Color.white;
            result.Metric = "Metric.LOC";
            return result;
        }

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
                if (values.TryGetValue(AntennaSectionsLabel, out object antennaSections))
                {
                    if (!(antennaSections is IList<object> objects))
                    {
                        throw new InvalidCastException($"Value to be cast {antennaSections} is expected to be a list. Actual type is {antennaSections.GetType().Name}");
                    }
                    foreach (object anObject in objects)
                    {
                        if (!(anObject is Dictionary<string, object> antennaSection))
                        {
                            throw new InvalidCastException($"Value to be cast {anObject} is expected to be a dictionary. Actual type is {anObject.GetType().Name}");
                        }
                        AntennaSection section = new AntennaSection();
                        result = section.Restore(antennaSection) || result;
                        AddAntennaSection(section);
                    }
                }
                return result;
            }
            else
            {
                return false;
            }

            // If AntennaSections already has an antenna section for the metric in newSection,
            // it will be removed. Then the newSection is added to it.
            void AddAntennaSection(AntennaSection newSection)
            {
                AntennaSections.RemoveAll(section => section.Metric == newSection.Metric);
                AntennaSections.Add(newSection);
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
