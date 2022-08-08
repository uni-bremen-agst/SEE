using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings of one antenna section specifying the metric that
    /// determines the length and color of the section.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    [Obsolete]
    public class AntennaSection : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// The metric which should determine the length of the section.
        /// </summary>
        public string Metric;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metric">the metric that should determine the length and color of the section</param>
        public AntennaSection(string metric)
        {
            Metric = metric;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AntennaSection() : this(string.Empty) { }

        /// <summary>
        /// Label in the configuration file for the <see cref="Metric"/>.
        /// </summary>
        private const string MetricLabel = "Metric";

        /// <summary>
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Metric, MetricLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Restore(Dictionary{string, object}, string)"/>.
        /// </summary>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            Dictionary<string, object> values;
            if (string.IsNullOrEmpty(label))
            {
                // no label given => attributes contains already the data to be restored
                values = attributes;
            }
            else if (attributes.TryGetValue(label, out object dictionary))
            {
                // label was given => attributes is a dictionary where we need to look up the data
                // using the label
                values = dictionary as Dictionary<string, object>;
            }
            else
            {
                // label was given, but attributes does not know it
                // => no data; we cannot restore the object
                return false;
            }

            return ConfigIO.Restore(values, MetricLabel, ref Metric);
        }
    }
}
