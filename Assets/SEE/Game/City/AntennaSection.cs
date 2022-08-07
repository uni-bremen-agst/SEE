using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings of one antenna section specifying the metric that
    /// determines the length and color of the section.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public class AntennaSection : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// The metric which should determine the length of the section.
        /// </summary>
        public string Metric;
        /// <summary>
        /// The color in which the section should be drawn.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metric">the metric that should determine the length of the section</param>
        /// <param name="color">color in the section should be drawn</param>
        public AntennaSection(string metric, Color color)
        {
            Metric = metric;
            Color = color;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AntennaSection() : this(string.Empty, Color.white) { }

        /// <summary>
        /// Label in the configuration file for the <see cref="Metric"/>.
        /// </summary>
        private const string MetricLabel = "Metric";
        /// <summary>
        /// Label in the configuration file for the <see cref="Color"/>.
        /// </summary>
        private const string ColorLabel = "Color";

        /// <summary>
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Metric, MetricLabel);
            writer.Save(Color, ColorLabel);
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

            bool metricRestored = ConfigIO.Restore(values, MetricLabel, ref Metric);
            return ConfigIO.Restore(values, ColorLabel, ref Color) || metricRestored;
        }
    }
}
