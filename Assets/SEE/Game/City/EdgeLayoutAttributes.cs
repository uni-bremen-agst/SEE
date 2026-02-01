using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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
        [Tooltip("Whether an edge layout should be generated. None means that no edges will be created."),
         PropertyOrder(-1)]
        public EdgeLayoutKind Kind = EdgeLayoutKind.Bundling;

        /// <summary>
        /// Callback when <see cref="ShowEdges"/> has changed.
        /// </summary>
        /// <param name="showEdges">The new state of <see cref="ShowEdges"/>.</param>
        public delegate void ShowEdgesChanged(ShowEdgeStrategy showEdges);

        /// <summary>
        /// Clients can register here to listen to changes of <see cref="ShowEdges"/>.
        /// </summary>
        public event ShowEdgesChanged OnShowEdgesChanged;

        /// <summary>
        /// Backing field for <see cref="ShowEdges"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private ShowEdgeStrategy showEdges = ShowEdgeStrategy.Always;

        /// <summary>
        /// The strategy when to show edges.
        /// </summary>
        [ShowInInspector, Tooltip("The strategy when to show edges."),
         PropertyOrder(1)]
        public ShowEdgeStrategy ShowEdges
        {
            get => showEdges;
            set
            {
                if (value != showEdges)
                {
                    showEdges = value;
                    OnShowEdgesChanged?.Invoke(showEdges);
                }
            }
        }

        /// <summary>
        /// Callback when <see cref="AnimateEdgeFlow"/> has changed.
        /// </summary>
        /// <param name="animateFlow">The new state of <see cref="AnimateEdgeFlow"/>.</param>
        public delegate void EdgeFlowChanged(bool animateFlow);

        /// <summary>
        /// Clients can register here to listen to changes of <see cref="AnimateEdgeFlow"/>.
        /// </summary>
        public event EdgeFlowChanged OnEdgeFlowChanged;

        /// <summary>
        /// Backing field for <see cref="AnimateEdgeFlow"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool animateEdgeFlow = false;

        /// <summary>
        /// The strategy when to show edges.
        /// </summary>
        [ShowInInspector, Tooltip("Whether the direction of an edge should be animated by a flow effect."),
         PropertyOrder(2)]
        public bool AnimateEdgeFlow
        {
            get => animateEdgeFlow;
            set
            {
                if (value != animateEdgeFlow)
                {
                    animateEdgeFlow = value;
                    OnEdgeFlowChanged?.Invoke(animateEdgeFlow);
                }
            }
        }

        /// <summary>
        /// Kind of animation used to draw edges.
        /// </summary>
        [Tooltip("The kind of animation used to draw edges when they appear."),
         PropertyOrder(2)]
        public EdgeAnimationKind AnimationKind = EdgeAnimationKind.None;

        /// <summary>
        /// Whether to animate edges of inner nodes as well when hovering over nodes.
        /// </summary>
        [Tooltip("When hovering over nodes, animate edges of inner nodes too."),
         PropertyOrder(3)]
        [InfoBox("Be aware that animating inner edges may cause heavy performance issues when "
                 + "combined with the 'Buildup' animation.", InfoMessageType.Warning,
                 nameof(WarnAboutInnerEdgeAnimation))]
        public bool AnimateInnerEdges = true;

        [Tooltip("When hovering over nodes, repeatedly animate the edges of source nodes, one after another."),
         PropertyOrder(4)]
        public bool AnimateTransitiveSourceEdges = false;

        [Tooltip("When hovering over nodes, repeatedly animate the edges of target nodes, one after another."),
         PropertyOrder(5)]
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
        [Tooltip("The maximal width of an edge"),
         Range(0.0f, MaxEdgeWidth),
         PropertyOrder(6)]
        public float EdgeWidth = 0.01f;

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// 0.85 is the value recommended by Holten
        /// </summary>
        [Tooltip("The strength of the bundling. Relevant only for Bundling layout."),
         Range(0.0f, 1.0f),
         PropertyOrder(7)]
        public float Tension = 0.85f;

        #region Config I/O
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), edgeLayoutLabel);
            writer.Save(ShowEdges.ToString(), showEdgesLabel);
            writer.Save(AnimateEdgeFlow, animateEdgeFlowLabel);
            writer.Save(AnimationKind.ToString(), animationKindLabel);
            writer.Save(AnimateInnerEdges, animateInnerEdgesLabel);
            writer.Save(AnimateTransitiveSourceEdges, animateTransitiveSourceEdgesLabel);
            writer.Save(AnimateTransitiveTargetEdges, animateTransitiveTargetEdgesLabel);
            writer.Save(EdgeWidth, edgeWidthLabel);
            writer.Save(Tension, tensionLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, edgeLayoutLabel, ref Kind);
                {
                    ShowEdgeStrategy strategy = ShowEdgeStrategy.Never;
                    if (ConfigIO.RestoreEnum(values, showEdgesLabel, ref strategy))
                    {
                        ShowEdges = strategy;
                    }
                }
                {
                    bool animateFlow = false;
                    if (ConfigIO.Restore(values, animateEdgeFlowLabel, ref animateFlow))
                    {
                        AnimateEdgeFlow = animateFlow;
                    }
                }
                ConfigIO.RestoreEnum(values, animationKindLabel, ref AnimationKind);
                ConfigIO.Restore(values, animateInnerEdgesLabel, ref AnimateInnerEdges);
                ConfigIO.Restore(values, animateTransitiveSourceEdgesLabel, ref AnimateTransitiveSourceEdges);
                ConfigIO.Restore(values, animateTransitiveTargetEdgesLabel, ref AnimateTransitiveTargetEdges);
                ConfigIO.Restore(values, edgeWidthLabel, ref EdgeWidth);
                ConfigIO.Restore(values, tensionLabel, ref Tension);
            }
        }

        private const string edgeLayoutLabel = "EdgeLayout";
        private const string showEdgesLabel = "ShowEdges";
        private const string edgeWidthLabel = "EdgeWidth";
        private const string tensionLabel = "Tension";
        private const string animateEdgeFlowLabel = "AnimateEdgeFlow";
        private const string animationKindLabel = "AnimationKind";
        private const string animateInnerEdgesLabel = "AnimateInnerEdges";
        private const string animateTransitiveSourceEdgesLabel = "AnimateTransitiveSourceEdges";
        private const string animateTransitiveTargetEdgesLabel = "AnimateTransitiveTargetEdges";

        #endregion Config I/O
    }
}
