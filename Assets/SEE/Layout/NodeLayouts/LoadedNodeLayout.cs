using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// A layout that is read from a GVL file.
    /// </summary>
    public class LoadedNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="filename">the name of the GVL file from which to read the layout information</param>
        public LoadedNodeLayout(float groundLevel, string filename)
          : base(groundLevel)
        {
            name = "Loaded Layout";
            this.filename = filename;
        }

        /// <summary>
        /// The name of the GVL file from which to read the layout information.
        /// </summary>
        private string filename;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new Dictionary<ILayoutNode, NodeTransform>();
            if (File.Exists(filename))
            {
                SEE.Layout.IO.Reader reader 
                    = new SEE.Layout.IO.Reader(filename, layoutNodes.Cast<IGameNode>().ToList(), groundLevel, new SEELogger());
                foreach (ILayoutNode node in layoutNodes)
                {
                    Vector3 position = node.CenterPosition;
                    Vector3 absoluteScale = node.AbsoluteScale;
                    // Note: The node transform's y co-ordinate of the position is interpreted as the ground of the object.
                    // We need to adjust it accordingly.
                    position.y -= absoluteScale.y / 2.0f;
                    result[node] = new NodeTransform(position, absoluteScale);
                }
            }
            else
            {
                Debug.LogErrorFormat("GVL file {0} does not exist. No layout could be loaded.\n", filename);
            }
            return result;
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new System.NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}
