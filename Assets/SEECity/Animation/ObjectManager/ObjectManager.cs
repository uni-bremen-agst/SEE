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
using System.Linq;
using UnityEngine;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// An ObjectManager creates and manages GameObjects by using a supplied
    /// GraphRenderer to create game objects for graph nodes. Those game objects
    /// will be cached, that is, non-existing GameObjects are created and stored 
    /// for reuse during query. Each GameObject is identified by the LinkName of 
    /// a node and can be retrieved via any node with the same LinkName.
    /// </summary>
    public class ObjectManager
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
        /// The plane enclosing all game objects of the city.
        /// </summary>
        private GameObject currentPlane;

        /// <summary>
        /// A dictionary containing all created nodes that are currently in use. The set of
        /// nodes contained may be an accumulation of all nodes created and added by GetInnerNode()
        /// and GetLeaf() so far and not just those of one single graph in the graph series
        /// (unless a node was removed by RemoveNode() meanwhile).
        /// </summary>
        private readonly Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="renderer">the graph renderer used to create the game objects</param>
        public ObjectManager(GraphRenderer renderer)
        {
            renderer.AssertNotNull("renderer");
            _graphRenderer = renderer;
        }

        /// <summary>
        /// Returns all created GameObjects till now.
        /// </summary>
        public List<GameObject> GameObjects
        {
            get => gameObjects;
        }

        /// <summary>
        /// List of all created nodes that are in use.
        /// </summary>
        private List<GameObject> gameObjects => nodes.Values.ToList();

        /// <summary>
        /// Returns a saved plane or generates a new one if it does not already exist. The resulting
        /// plane encloses all currently cached game objects of the city only if it was newly 
        /// generated. It may need to be adjusted if it was not newly generated. TODO.
        /// </summary>
        /// <param name="plane">the plane intended to enclose all game objects of the city</param>
        /// <returns>true if the plane already existed (thus, can be re-used) and false if it was newly created</returns>
        public bool GetPlane(out GameObject plane)
        {
            bool hasPlane = currentPlane != null;
            if (!hasPlane)
            {
                currentPlane = GraphRenderer.NewPlane(gameObjects);
            }
            plane = currentPlane;
            return hasPlane;
        }

        /// <summary>
        /// Returns a saved GameObject for a leaf or inner node or creates a new one if it does not already exist.
        /// If the node is a leaf, GetLeaf() will be used to create an leaf node; otherwise GetInnerNode()
        /// will be used instead to create an inner node.
        /// </summary>
        /// <param name="node">The node under which a GameObject may be stored.</param>
        /// <param name="leaf">The resulting GameObject associated to node or null if no GameObject could 
        /// be found or created.</param>
        /// <returns>True if the GameObject already existed and false if it was newly created.</returns>
        public bool GetNode(Node node, out GameObject gameNode)
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
        /// Returns a saved GameObject for an inner node or creates and caches a new one if it does not 
        /// already exist. Scale and style of <paramref name="innerNode"/> remain unchanged.
        /// The given <paramref name="node"/> will be attached to <paramref name="leaf"/> and replaces
        /// its currently attached graph node.
        /// </summary>
        /// <param name="node">the inner node under which a GameObject may be stored</param>
        /// <param name="innerNode">the resulting GameObject associated to node or null if no GameObject 
        /// could be found or created</param>
        /// <returns>true if the GameObject already existed and false if it was newly created</returns>
        public bool GetInnerNode(Node node, out GameObject innerNode)
        {
            node.AssertNotNull("node");

            if (nodes.TryGetValue(node.LinkName, out innerNode))
            {
                // A game object for this inner code could be retrieved.
                // The game object has already a node attached to it, but that
                // node is part of a different graph (i.e,, different revision).
                // That is why we replace the attached node by this node here.
                ReattachNode(innerNode, node);
                return true;
            }
            else
            {
                // NewInnerNode() will attach node to innerNode
                innerNode = GraphRenderer.NewInnerNode(node);
                // Note: The scale of innerNode will be adjusted later when we have the
                // layout. 
                // TODO: Inner nodes have a style, too, as much as leaves. We may need to
                // adjust that style, too, either here or (likely better) later when we 
                // apply the layout to inner nodes.
                return false;
            }
        }

        /// <summary>
        /// Returns a saved GameObject for a leaf node or creates and caches a new one if it does not 
        /// already exist.
        /// The resulting game object will have the dimensions and style according to the attributes of 
        /// the given <paramref name="node"/> even if the game node existed already. The position of the
        /// resulting game object is random. The reason for that is the fact that layouts do not change 
        /// the scale (well, some of them -- for instance, TreeMap -- may shrink or extend the scale by  
        /// a factor). Instead the node layouters need to know the scale of the nodes they are to layout 
        /// upfront. On the other hand, the layouts determine the positions. That is why we adjust the 
        /// scale (and the style) but not the position here. 
        /// The given <paramref name="node"/> will be attached to <paramref name="leaf"/> and replaces
        /// its currently attached graph node.
        /// </summary>
        /// <param name="node">the leaf node under which a GameObject may be stored</param>
        /// <param name="leaf">the resulting GameObject associated to node or null if no GameObject 
        /// could be found or created</param>
        /// <returns>true if the GameObject already existed and false if it was newly created</returns>
        public bool GetLeaf(Node node, out GameObject leaf)
        {
            node.AssertNotNull("node");

            if (nodes.TryGetValue(node.LinkName, out leaf))
            {
                // We are re-using an existing node, but that node's attributes
                // determining its scale or style might have changed. That is why we 
                // need to adjust its scale and style, too.

                // The game object has already a node attached to it, but that
                // node is part of a different graph (i.e,, different revision).
                // That is why we replace the attached node by this node here.
                ReattachNode(leaf, node);

                // Now after having attached the new node to the game object,
                // we must adjust the visual attributes of it according to the
                // newly attached node. Actually, only the scale would need to
                // be adjusted because that is the information later needed by the
                // layouter.
                GraphRenderer.AdjustVisualsOfBlock(leaf);
                return true;
            }
            else
            {
                // NewLeafNode() will set the scale and style of the leaf
                // and will also attach the node to it.
                leaf = GraphRenderer.NewLeafNode(node);
                return false;
            }
        }

        /// <summary>
        /// Re-attaches the given <paramref name="node"/> to the given <paramref name="gameObject"/>,
        /// that is, the NodeRef component of <paramref name="gameObject"/> will refer to 
        /// <paramref name="node"/> afterwards.
        /// </summary>
        /// <param name="gameObject">the game object where the node is to be attached to</param>
        /// <param name="node">the node to be attached</param>
        private static void ReattachNode(GameObject gameObject, Node node)
        {
            NodeRef noderef = gameObject.GetComponent<NodeRef>();
            if (noderef == null)
            {
                // noderef should not be null
                Debug.LogErrorFormat("Re-used game object for node '{0}' does not have a graph node attached to it\n",
                                     node.LinkName);
                noderef = gameObject.AddComponent<NodeRef>();
            }
            noderef.node = node;
        }

        /// <summary>
        /// Removes a the game object representing the given <paramref name="node"/> by using the LinkName 
        /// of the <paramref name="node"/> and returns the removed node in <paramref name="gameObject"/>, if 
        /// it existed. Returns true if such a game object existed in the cache.
        /// </summary>
        /// <param name="node">node determining the game object to be removed from the cache</param>
        /// <param name="gameObject">the corresponding game object that was removed from the cache or null</param>
        /// <returns>true if a corresponding game object existed and was removed from the cache</returns>
        public bool RemoveNode(Node node, out GameObject gameObject)
        {
            node.AssertNotNull("node");

            var wasNodeRemoved = nodes.TryGetValue(node.LinkName, out gameObject);
            nodes.Remove(node.LinkName);
            return wasNodeRemoved;
        }

        /// <summary>
        /// Clears the internal cache containing all game objects created by GetInnerNode(),
        /// GetLeaf(), GetNode(), or GetPlane() without actually destroing those game objects.
        /// </summary>
        public void Clear()
        {
            currentPlane = null;
            nodes.Clear();
        }
    }
}