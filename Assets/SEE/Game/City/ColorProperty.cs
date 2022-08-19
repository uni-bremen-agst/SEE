using System;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Specifies for a node what the color is used for: either the node type
    /// or a node metric.
    /// </summary>
    [Serializable]
    public class ColorProperty : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// Whether color is used to represent the node type or a metric.
        /// </summary>
        [EnumToggleButtons]
        public PropertyKind Property = PropertyKind.Type;

        /// <summary>
        /// The color used to represent the type of a node.
        /// Used only if <see cref="Property"/> is <see cref="PropertyKind.Type"/>.
        /// </summary>
        [ShowIf("Property", PropertyKind.Type)]
        public Color TypeColor = Color.white;

        /// <summary>
        /// Whether the <see cref="TypeColor"/> should be varied by the level of the node
        /// in the node hierarchy.
        /// Used only if <see cref="Property"/> is <see cref="PropertyKind.Type"/>.
        /// </summary>
        [ShowIf("Property", PropertyKind.Type)]
        public bool ByLevel = true;

        /// <summary>
        /// The name of the metric determining the style (color) of a node. The
        /// actual color is found in a <see cref="ColorMap"/> for the metrics.
        /// Used only if <see cref="Property"/> is <see cref="PropertyKind.Metric"/>.
        /// </summary>
        [ShowIf("Property", PropertyKind.Metric)]
        public string ColorMetric = string.Empty;

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.RestoreEnum(values, PropertyLabel, ref Property);
                ConfigIO.Restore(values, TypeColorLabel, ref TypeColor);
                ConfigIO.Restore(values, ByLevelLabel, ref ByLevel);
                ConfigIO.Restore(values, ColorMetricLabel, ref ColorMetric);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the settings in the configuration file using <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">writer to be used to save the settings</param>
        /// <param name="label">label under which to save the settings</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(Property.ToString(), PropertyLabel);
            writer.Save(TypeColor, TypeColorLabel);
            writer.Save(ByLevel, ByLevelLabel);
            writer.Save(ColorMetric, ColorMetricLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Label in the configuration file for a <see cref="Property"/>
        /// </summary>
        private const string PropertyLabel = "Property";
        /// <summary>
        /// Label in the configuration file for <see cref="TypeColor"/>.
        /// </summary>
        private const string TypeColorLabel = "TypeColor";
        /// <summary>
        /// Label in the configuration file for <see cref="TypeColor"/>.
        /// </summary>
        private const string ByLevelLabel = "ByLevel";
        /// <summary>
        /// Label in the configuration file for a <see cref="ColorMetric"/>
        /// </summary>
        private const string ColorMetricLabel = "ColorMetric";
    }
}
