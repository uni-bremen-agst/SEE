//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// An ObjectManager creates and manages GameObjects with a given BlockFactory.
    /// Non-existing GameObjects are created and stored for reuse during query,
    /// depending on the implementation. Each GameObject is assigned to the
    /// LinkName of a node and can be retrieved via any node with the same LinkName.
    /// </summary>
    public abstract class AbstractObjectManager
    {
        /// <summary>
        /// The graph renderer used to create the game objects. It is used for creating missing
        /// game objects.
        /// </summary>
        private readonly GraphRenderer _graphRenderer;

        /// <summary>
        /// Returns the graph renderer used to create the game objects.
        /// </summary>
        protected GraphRenderer GraphRenderer => _graphRenderer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="renderer">the graph renderer used to create the game objects</param>
        public AbstractObjectManager(GraphRenderer renderer)
        {
            renderer.AssertNotNull("city");
            _graphRenderer = renderer;
        }

        /// <summary>
        /// Returns all created GameObjects till now
        /// </summary>
        public abstract List<GameObject> GameObjects
        {
            get;
        }

        /// <summary>
        /// Returns a saved plane or generates a new one if it does not already exist. The resulting
        /// plane encloses all game objects of the city.
        /// </summary>
        /// <param name="plane">the plane (new or existing)</param>
        /// <returns>true if the plane already existed (thus, can be re-used) and false if it was newly 
        /// created.</returns>
        public abstract bool GetPlane(out GameObject plane);

        /// <summary>
        /// Returns a saved GameObject for an inner node or creates a new one if it does not already exist.
        /// The resulting game object will have a random scale and position. Those will be determined later
        /// when the layout was calculated.
        /// </summary>
        /// <param name="node">The inner node under which a GameObject may be stored.</param>
        /// <param name="innerNode">The resulting GameObject associated to node or null if no GameObject 
        /// could be found or created.</param>
        /// <returns>true if the GameObject already existed and false if it was newly created.</returns>
        public abstract bool GetInnerNode(Node node, out GameObject innerNode);

        /// <summary>
        /// Returns a saved GameObject for a leaf node or creates a new one if it does not already exist.
        /// The resulting game object will have the dimensions according to attributes of the given 
        /// <paramref name="node"/> even if the game node existed already. The position of the resulting
        /// game object is random. The reason for that is the fact that layouts do not change the scale
        /// (well, some of them, for instance the TreeMap, may shrink or extend the scale by a factor). 
        /// Instead the node layouters need to know the scale of the nodes they are to layout upfront. 
        /// On the other hand, the layouts determine the positions.
        /// </summary>
        /// <param name="node">The leaf node under which a GameObject may be stored.</param>
        /// <param name="leaf">The resulting GameObject associated to node or null if no GameObject 
        /// could be found or created.</param>
        /// <returns>True if the GameObject already existed and false if it was newly created.</returns>
        public abstract bool GetLeaf(Node node, out GameObject leaf);

        /// <summary>
        /// Returns a saved GameObject for a leaf or inner node or creates a new one if it does not already exist.
        /// If the node is a leaf, GetLeaf() will be used to create an leaf node; otherwise GetInnerNode()
        /// will be used instead to create an inner node.
        /// </summary>
        /// <param name="node">The node under which a GameObject may be stored.</param>
        /// <param name="leaf">The resulting GameObject associated to node or null if no GameObject could 
        /// be found or created.</param>
        /// <returns>True if the GameObject already existed and false if it was newly created.</returns>
        public virtual bool GetNode(Node node, out GameObject gameNode)
        {
            if (node.IsLeaf())
            {
                return GetLeaf(node, out gameNode);
            }
            else
            {
                return GetInnerNode(node, out gameNode);
            }
        }

        /// <summary>
        /// Removes the GameObject from a given node in the internal node cache
        /// and returns it for further use.
        /// </summary>
        /// <param name="node">The node under which a GameObject may be stored.</param>
        /// <param name="gameObject">The Gameobject, which belongs to the given node.</param>
        /// <returns>True if a GameObject was found for the given node.</returns>
        public abstract bool RemoveNode(Node node, out GameObject gameObject);

        /// <summary>
        /// Removes all generated GameObjects from the internally used node cache.
        /// This does not delete or destroy GameObjects.
        /// </summary>
        public abstract void Clear();
    }
}