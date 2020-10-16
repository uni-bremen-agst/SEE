// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    public class CoseNodeSublayoutValues
    {
        /// <summary>
        /// the scale of the calculated sublayout. If the node layout is not enclosing all its childnodes, 
        /// the position and scale of the root node will be adaped.
        /// </summary>
        private Vector3 relativeScale;

        /// <summary>
        /// the centerposition of the calculated sublayout. If the node layout is not enclosing all 
        /// its childnodes, the position and scale of the root node will be adaped.
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

