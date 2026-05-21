using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Layout.IO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// A layout that is read from a layout file.
    /// </summary>
    public class LoadedNodeLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">The y co-ordinate setting the ground level; all nodes will be
        /// placed on this level.</param>
        public LoadedNodeLayout(string filename)
        {
            this.filename = filename;
        }

        static LoadedNodeLayout()
        {
            Name = "Loaded Layout";
        }

        /// <summary>
        /// The name of the layout file from which to read the layout information.
        /// </summary>
        private readonly string filename;

        /// <summary>
        /// See <see cref="NodeLayout.Layout"/>.
        /// </summary>
        /// <exception cref="Exception">Thrown if the file extension of <see cref="filename"/>
        /// is not known or if the file could not be loaded.</exception>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
             Vector3 centerPosition,
             Vector2 rectangle)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new();
            // Where to add the loaded node layout.
            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            ICollection<IGameNode> gameNodes = layoutNodeList.Cast<IGameNode>().ToList();

            LayoutReader.Read(filename, gameNodes);

            // Apply the layout for the result.
            foreach (ILayoutNode node in layoutNodeList)
            {
                result[node] = new NodeTransform(node.CenterPosition, node.AbsoluteScale);
            }
            return result;
        }
    }
}
