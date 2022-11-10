using System;
using System.Collections.Generic;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings for the layout of the edges.
    /// </summary>
    [Serializable]
    public class EdgeLayoutAttributes : LayoutSettings
    {
        /// <summary>
        /// Layout for drawing edges.
        /// </summary>
        public EdgeLayoutKind Kind = EdgeLayoutKind.Bundling;

        /// <summary>
        /// The width of an edge (drawn as line).
        /// </summary>
        [Range(0.0f, float.MaxValue)]
        public float EdgeWidth = 0.01f;

        /// <summary>
        /// Orientation of the edges;
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses.
        /// </summary>
        public bool EdgesAboveBlocks = true;

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// 0.85 is the value recommended by Holten
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Tension = 0.85f;

        /// <summary>
        /// Determines to which extent the polylines of the generated splines are
        /// simplified. Range: [0.0, inf] (0.0 means no simplification). More precisely,
        /// stores the epsilon parameter of the RamerDouglasPeucker algorithm which
        /// is used to identify and remove points based on their distances to the line
        /// drawn between their neighbors.
        /// </summary>
        public float RDP = 0.0001f;

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), EdgeLayoutLabel);
            writer.Save(EdgeWidth, EdgeWidthLabel);
            writer.Save(EdgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(Tension, TensionLabel);
            writer.Save(RDP, RDPLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, EdgeLayoutLabel, ref Kind);
                ConfigIO.Restore(values, EdgeWidthLabel, ref EdgeWidth);
                ConfigIO.Restore(values, EdgesAboveBlocksLabel, ref EdgesAboveBlocks);
                ConfigIO.Restore(values, TensionLabel, ref Tension);
                ConfigIO.Restore(values, RDPLabel, ref RDP);
            }
        }

        private const string EdgeLayoutLabel = "EdgeLayout";
        private const string EdgeWidthLabel = "EdgeWidth";
        private const string EdgesAboveBlocksLabel = "EdgesAboveBlocks";
        private const string TensionLabel = "Tension";
        private const string RDPLabel = "RDP";
    }
}