using SEE.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEE.Game
{
    /// <summary>
    /// Setting for labels to be shown above game nodes.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public class LabelAttributes
    {
        /// <summary>
        /// If true, a label with the node's SourceName will be displayed above each node.
        /// </summary>
        [Tooltip("Whether the label should be shown during hovering.")]
        public bool Show = true;

        /// <summary>
        /// The distance between the top of the node and its label.
        /// </summary>
        [Tooltip("The distance between the top of the node and its label.")]
        public float Distance = 0.2f;

        /// <summary>
        /// The font size of the node's label.
        /// </summary>
        [Tooltip("The font size of the label.")]
        public float FontSize = 0.4f;

        /// <summary>
        /// How fast the label should (dis)appear.
        /// </summary>
        [FormerlySerializedAs("AnimationDuration")]
        [Tooltip("How fast the label should (dis)appear, expressed as a factor multiplied to the base duration.")]
        public float AnimationFactor = 0.5f;

        /// <summary>
        /// The alpha value of the label.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("The alpha value (degree of transparency) of the label.")]
        public float LabelAlpha = 1f;

        private const string ShowLabel = "Show";
        private const string DistanceLabel = "Distance";
        private const string FontSizeLabel = "FontSize";
        private const string AnimationDurationLabel = "AnimationDuration";
        private const string LabelAlphaLabel = "LabelAlpha";

        /// <summary>
        /// Saves these LabelSettings using <paramref name="writer"/> under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">used to emit the settings</param>
        /// <param name="label">the label under which to emit the settings</param>
        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Show, ShowLabel);
            writer.Save(Distance, DistanceLabel);
            writer.Save(FontSize, FontSizeLabel);
            writer.Save(AnimationFactor, AnimationDurationLabel);
            writer.Save(LabelAlpha, LabelAlphaLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the label settings based on the values of the entry in <paramref name="attributes"/>
        /// via key <paramref name="label"/>. If there is no such label, nothing happens. If any of the
        /// values is missing, the original value will be kept.
        /// </summary>
        /// <param name="attributes">where to look up the values</param>
        /// <param name="label">the key for the lookup</param>
        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                {
                    ConfigIO.Restore(values, ShowLabel, ref Show);
                    ConfigIO.Restore(values, DistanceLabel, ref Distance);
                    ConfigIO.Restore(values, FontSizeLabel, ref FontSize);
                    ConfigIO.Restore(values, AnimationDurationLabel, ref AnimationFactor);
                    ConfigIO.Restore(values, LabelAlphaLabel, ref LabelAlpha);
                }
            }
        }
    }
}