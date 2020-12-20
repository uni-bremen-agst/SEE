using SEE.Utils;
using System;
using System.Collections.Generic;

namespace SEE.Game
{
    /// <summary>
    /// Setting for labels to be shown above game nodes.
    /// </summary>
    public class LabelSettings
    {
        /// <summary>
        /// If true, a label with the node's SourceName will be displayed above each node.
        /// </summary>
        public bool Show = true;
        /// <summary>
        /// The distance between the top of the node and its label.
        /// </summary>
        public float Distance = 0.2f;
        /// <summary>
        /// The font size of the node's label.
        /// </summary>
        public float FontSize = 0.4f;

        private const string ShowLabel = "Show";
        private const string DistanceLabel = "Distance";
        private const string FontSizeLabel = "FontSize";

        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ShowLabel, Show);
            writer.Save(DistanceLabel, Distance);
            writer.Save(FontSizeLabel, FontSize);
            writer.EndGroup();
        }

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, ShowLabel, ref Show);
                    ConfigIO.Restore(values, DistanceLabel, ref Distance);
                    ConfigIO.Restore(values, FontSizeLabel, ref FontSize);
                }
            }
        }
    }
}
