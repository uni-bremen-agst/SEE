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

using System.Collections.Generic;
using SEE.Game;

namespace SEE.Layout.NodeLayouts.Cose
{
    public abstract class AbstractSublayoutNode<T>
    {
        /// <summary>
        /// the root node
        /// </summary>
        public T Node { get; }

        /// <summary>
        /// A List with removed children
        /// </summary>
        public List<T> RemovedChildren { get; set; }

        /// <summary>
        /// nodes of the sublayout
        /// </summary>
        public List<T> Nodes { get; set; }

        /// <summary>
        /// the kind of the inner nodes
        /// </summary>
        public InnerNodeKinds InnerNodeKind { get; }

        /// <summary>
        /// the node Layout
        /// </summary>
        public NodeLayoutKind NodeLayout { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="node">the root node</param>
        /// <param name="innerNodeKinds">the kind of the inner nodes</param>
        /// <param name="nodeLayouts">the node Layout</param>
        public AbstractSublayoutNode(T node, InnerNodeKinds innerNodeKinds, NodeLayoutKind nodeLayouts)
        {
            Node = node;
            InnerNodeKind = innerNodeKinds;
            NodeLayout = nodeLayouts;
            Nodes = new List<T>();
            RemovedChildren = new List<T>();
        }
    }

}

