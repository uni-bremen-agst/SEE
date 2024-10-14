using System;
using System.Collections.Generic;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
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
        /// Kind of animation used to draw edges.
        /// </summary>
        public EdgeAnimationKind AnimationKind = EdgeAnimationKind.None;

        /// <summary>
        /// Whether to animate edges of inner nodes as well when hovering over nodes.
        /// </summary>
        [Tooltip("When hovering over nodes, animate edges of inner nodes too.")]
        [InfoBox("Be aware that animating inner edges may cause heavy performance issues when "
                 + "combined with the 'Buildup' animation.", InfoMessageType.Warning,
                 nameof(WarnAboutInnerEdgeAnimation))]
        public bool AnimateInnerEdges = true;

        [Tooltip("When hovering over nodes, repeatedly animate the edges of source nodes, one after another.")]
        public bool AnimateTransitiveSourceEdges = false;

        [Tooltip("When hovering over nodes, repeatedly animate the edges of target nodes, one after another.")]
        public bool AnimateTransitiveTargetEdges = false;

        /// <summary>
        /// True if the user should be warned about animating inner edges due to performance issues.
        /// </summary>
        private bool WarnAboutInnerEdgeAnimation => AnimateInnerEdges && AnimationKind == EdgeAnimationKind.Buildup;

        /// <summary>
        /// The maximal width of an edge.
        /// </summary>
        public const float MaxEdgeWidth = 0.1f;

        /// <summary>
        /// The width of an edge (drawn as line).
        /// </summary>
        [Range(0.0f, MaxEdgeWidth)]
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

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), edgeLayoutLabel);
            writer.Save(AnimationKind.ToString(), animationKindLabel);
            writer.Save(AnimateInnerEdges, animateInnerEdgesLabel);
            writer.Save(AnimateTransitiveSourceEdges, animateTransitiveSourceEdgesLabel);
            writer.Save(AnimateTransitiveTargetEdges, animateTransitiveTargetEdgesLabel);
            writer.Save(EdgeWidth, edgeWidthLabel);
            writer.Save(EdgesAboveBlocks, edgesAboveBlocksLabel);
            writer.Save(Tension, tensionLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, edgeLayoutLabel, ref Kind);
                ConfigIO.RestoreEnum(values, animationKindLabel, ref AnimationKind);
                ConfigIO.Restore(values, animateInnerEdgesLabel, ref AnimateInnerEdges);
                ConfigIO.Restore(values, animateTransitiveSourceEdgesLabel, ref AnimateTransitiveSourceEdges);
                ConfigIO.Restore(values, animateTransitiveTargetEdgesLabel, ref AnimateTransitiveTargetEdges);
                ConfigIO.Restore(values, edgeWidthLabel, ref EdgeWidth);
                ConfigIO.Restore(values, edgesAboveBlocksLabel, ref EdgesAboveBlocks);
                ConfigIO.Restore(values, tensionLabel, ref Tension);
            }
        }

        private const string edgeLayoutLabel = "EdgeLayout";
        private const string edgeWidthLabel = "EdgeWidth";
        private const string edgesAboveBlocksLabel = "EdgesAboveBlocks";
        private const string tensionLabel = "Tension";
        private const string animationKindLabel = "AnimationKind";
        private const string animateInnerEdgesLabel = "AnimateInnerEdges";
        private const string animateTransitiveSourceEdgesLabel = "AnimateTransitiveSourceEdges";
        private const string animateTransitiveTargetEdgesLabel = "AnimateTransitiveTargetEdges";
    }
}
