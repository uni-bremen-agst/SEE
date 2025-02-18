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
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
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
        /// <exception cref="Exception">thrown if the file extension of <see cref="filename"/>
        /// is not known or if the file could not be loaded</exception>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
             Vector3 centerPosition,
             Vector2 rectangle)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new();
            if (File.Exists(filename))
            {
                // Where to add the loaded node layout.
                IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
                // Load the layout from the file.
                if (Filenames.HasExtension(filename, Filenames.GVLExtension))
                {
                    new GVLReader(filename, layoutNodeList.Cast<IGameNode>().ToList(), groundLevel, new SEELogger());
                    // The elements in layoutNodeList will be stacked onto each other starting at groundLevel.
                }
                else if (Filenames.HasExtension(filename, Filenames.SLDExtension))
                {
                    SLDReader.Read(filename, layoutNodeList.Cast<IGameNode>().ToList());
                    // The elements in layoutNodeList will have the y position as it was saved in the file.
                }
                else
                {
                    throw new Exception($"Unknown layout file format for file extension of {filename}.");
                }

                // Apply the layout for the result.
                foreach (ILayoutNode node in layoutNodeList)
                {
                    result[node] = new NodeTransform(node.CenterPosition, node.AbsoluteScale);
                }
            }
            else
            {
                throw new Exception($"Layout file {filename} does not exist. No layout could be loaded.");
            }
            return result;
        }
    }
}
