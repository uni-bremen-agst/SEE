using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE;

namespace SEE.Layout
{
    public class CoseNodeSublayoutValues
    {
        /// <summary>
        /// the scale of the calculated sublayout. If the node layout is not enclosing all its childnodes, the position and scale of the root node will be adaped.
        /// </summary>
        private Vector3 relativeScale;

        /// <summary>
        /// the centerposition of the calculated sublayout. If the node layout is not enclosing all its childnodes, the position and scale of the root node will be adaped.
        /// </summary>
        private Vector3 relativeCenterPosition;

        /// <summary>
        /// Indicates whether the node is a sublayout root
        /// </summary>
        private bool isSubLayoutRoot = false;

        /// <summary>
        /// Indicates whether the node is a sublayout node
        /// </summary>
        private bool isSubLayoutNode = false;

        /// <summary>
        /// The root node of the sublayout the node is part of 
        /// </summary>
        private CoseNode subLayoutRoot = null;

        /// <summary>
        /// is the node is sublayoutRoot, the sublayout if this node 
        /// </summary>
        private Sublayout sublayout;

        public bool IsSubLayoutRoot { get => isSubLayoutRoot; set => isSubLayoutRoot = value; }
        public bool IsSubLayoutNode { get => isSubLayoutNode; set => isSubLayoutNode = value; }
        public CoseNode SubLayoutRoot { get => subLayoutRoot; set => subLayoutRoot = value; }
        public Sublayout Sublayout { get => sublayout; set => sublayout = value; }
        public Vector3 RelativeScale { get => relativeScale; set => relativeScale = value; }
        public Vector3 RelativeCenterPosition { get => relativeCenterPosition; set => relativeCenterPosition = value; }

        /// <summary>
        /// Set the given x/ z position the the relative centerPosition
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="z">z value</param>
        public void SetLocationRelative(float x, float z)
        {
            relativeCenterPosition.x = x;
            relativeCenterPosition.z = z;
        }

        /// <summary>
        /// updates the relative bounding rect
        /// </summary>
        /// <param name="scale">the scale</param>
        /// <param name="position">the centerPosition</param>
        public void UpdateRelativeBounding(Vector3 scale, Vector3 position)
        {
            relativeScale = scale;
            relativeCenterPosition = position;
        }
    }
}

