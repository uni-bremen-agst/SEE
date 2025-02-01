using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SEE.Game.City
{
    /// <summary>
    /// The settings for the layout of the nodes.
    /// </summary>
    [Serializable]
    public class NodeLayoutAttributes : LayoutSettings
    {
        /// <summary>
        /// How to layout the nodes.
        /// </summary>
        [Tooltip("How to layout the nodes.")]
        public NodeLayoutKind Kind = NodeLayoutKind.Treemap;

        /// <summary>
        /// Settings for the <see cref="Layout.NodeLayouts.IncrementalTreeMapLayout"/>.
        /// </summary>
        [Tooltip("Settings for the IncrementalTreeMap layout. Used only for this kind of layout.")]
        [ShowIf("@this.Kind == NodeLayoutKind.IncrementalTreeMap")]
        public IncrementalTreeMapAttributes IncrementalTreeMap = new();

        [Tooltip("Layout for the architecture. Used only for the reflexion layout.")]
        [ShowIf("@this.Kind == NodeLayoutKind.Reflexion")]
        public NodeLayoutKind Architecture = NodeLayoutKind.Treemap; // TODO: Save in configuration.

        [Tooltip("Layout for the implementation. Used only for the reflexion layout.")]
        [ShowIf("@this.Kind == NodeLayoutKind.Reflexion")]
        public NodeLayoutKind Implementation = NodeLayoutKind.Treemap; // TODO: Save in configuration.

        /// <summary>
        /// The path for the layout file containing the node layout information.
        /// If the file extension is <see cref="Filenames.GVLExtension"/>, the layout is expected
        /// to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates.
        /// Otherwise our own layout format SDL is expected, which saves the complete Transform
        /// data of a game object.
        /// </summary>
        [OdinSerialize]
        [Tooltip("The path to the layout file containing the node layout information. " +
                 "If the file extension is GVL, the layout is expected to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates. " +
                 "Otherwise the layout format SDL is expected, which saves all three dimensions of a node. " +
                 "This information is used only if 'From File' is selected as node layout.")]
        public DataPath LayoutPath = new();

        #region Config I/O
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), nodeLayoutLabel);
            LayoutPath.Save(writer, layoutPathLabel);
            IncrementalTreeMap.Save(writer, incrementalTreeMapLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, nodeLayoutLabel, ref Kind);
                LayoutPath.Restore(values, layoutPathLabel);
                IncrementalTreeMap.Restore(values, incrementalTreeMapLabel);
            }
        }

        /// <summary>
        /// Configuration label for <see cref="LayoutPath"/>.
        /// </summary>
        private const string layoutPathLabel = "LayoutPath";
        /// <summary>
        /// Configuration label for <see cref="IncrementalTreeMap"/>.
        /// </summary>
        private const string incrementalTreeMapLabel = "IncrementalTreeMap";
        /// <summary>
        /// Configuration label for <see cref="Kind"/>.
        /// </summary>
        private const string nodeLayoutLabel = "NodeLayout";
        #endregion Config I/O
    }
}
