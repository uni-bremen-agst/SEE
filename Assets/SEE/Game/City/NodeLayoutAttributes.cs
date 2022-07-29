using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.Utils;

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
        public NodeLayoutKind Kind = NodeLayoutKind.Balloon;

        /// <summary>
        /// The path for the layout file containing the node layout information.
        /// If the file extension is <see cref="Filenames.GVLExtension"/>, the layout is expected
        /// to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates.
        /// Otherwise our own layout format SDL is expected, which saves the complete Transform
        /// data of a game object.
        /// </summary>
        [OdinSerialize]
        public FilePath LayoutPath = new FilePath();

        private const string LayoutPathLabel = "LayoutPath";

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), NodeLayoutLabel);
            LayoutPath.Save(writer, LayoutPathLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, NodeLayoutLabel, ref Kind);
                LayoutPath.Restore(values, LayoutPathLabel);
            }
        }

        private const string NodeLayoutLabel = "NodeLayout";
    }
}
