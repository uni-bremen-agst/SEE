﻿using System;
using System.Collections.Generic;
using SEE.Utils;
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
            writer.Save(Kind.ToString(), EdgeLayoutLabel);
            writer.Save(AnimationKind.ToString(), AnimationKindLabel);
            writer.Save(AnimateInnerEdges, AnimateInnerEdgesLabel);
            writer.Save(EdgeWidth, EdgeWidthLabel);
            writer.Save(EdgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(Tension, TensionLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, EdgeLayoutLabel, ref Kind);
                ConfigIO.RestoreEnum(values, AnimationKindLabel, ref AnimationKind);
                ConfigIO.Restore(values, AnimateInnerEdgesLabel, ref AnimateInnerEdges);
                ConfigIO.Restore(values, EdgeWidthLabel, ref EdgeWidth);
                ConfigIO.Restore(values, EdgesAboveBlocksLabel, ref EdgesAboveBlocks);
                ConfigIO.Restore(values, TensionLabel, ref Tension);
            }
        }

        private const string EdgeLayoutLabel = "EdgeLayout";
        private const string EdgeWidthLabel = "EdgeWidth";
        private const string EdgesAboveBlocksLabel = "EdgesAboveBlocks";
        private const string TensionLabel = "Tension";
        private const string AnimationKindLabel = "AnimationKind";
        private const string AnimateInnerEdgesLabel = "AnimateInnerEdges";
    }
}
