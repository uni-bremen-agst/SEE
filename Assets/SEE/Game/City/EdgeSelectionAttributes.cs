using System;
using System.Collections.Generic;
using SEE.Utils;

namespace SEE.Game.City
{
    /// <summary>
    /// Attributes regarding the selection of edges.
    /// </summary>
    [Serializable]
    public class EdgeSelectionAttributes : VisualAttributes
    {
        /// <summary>
        /// Number of segments along the tubular for edge selection.
        /// </summary>
        public int TubularSegments = 50;
        /// <summary>
        /// Radius of the tubular for edge selection.
        /// </summary>
        public float Radius = 0.005f;
        /// <summary>
        /// Number of segments around the tubular for edge selection.
        /// </summary>
        public int RadialSegments = 8;
        /// <summary>
        /// Whether the edges are selectable or not.
        /// </summary>
        public bool AreSelectable = true;

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(TubularSegments, TubularSegmentsLabel);
            writer.Save(Radius, RadiusLabel);
            writer.Save(RadialSegments, RadialSegmentsLabel);
            writer.Save(AreSelectable, AreSelectableLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, TubularSegmentsLabel, ref TubularSegments);
                ConfigIO.Restore(values, RadialSegmentsLabel, ref RadialSegments);
                ConfigIO.Restore(values, RadiusLabel, ref Radius);
                ConfigIO.Restore(values, AreSelectableLabel, ref AreSelectable);
            }
        }

        private const string TubularSegmentsLabel = "TubularSegments";
        private const string RadialSegmentsLabel = "RadialSegments";
        private const string RadiusLabel = "Radius";
        private const string AreSelectableLabel = "AreSelectable";
    }
}
