﻿//Copyright 2020 Florian Garbade

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

using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// An ObjectManager creates and manages GameObjects by using a supplied
    /// GraphRenderer to create game objects for graph nodes. Those game objects
    /// will be cached, that is, non-existing GameObjects are created and stored 
    /// for reuse during query. Each GameObject is identified by the ID of 
    /// a node and can be retrieved via any node with the same ID.
    /// </summary>
    public class ObjectManager
    {
        /// <summary>
        /// The graph renderer used to create the game objects. It is used for creating missing
        /// game objects.
        /// </summary>
        private readonly GraphRenderer _graphRenderer;

        /// <summary>
        /// The game object representing the city; its position and scale determines
        /// the position and scaling of the city elements visualized by the renderer.
        /// </summary>
        private readonly GameObject city;

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
        /// <param name="city">the game object representing the city; its position and scale determines
        /// the position and scaling of the city elements visualized by the renderer</param>
        public ObjectManager(GraphRenderer renderer, GameObject city)
        {
            renderer.AssertNotNull("renderer");
            _graphRenderer = renderer;
            this.city = city;
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
        /// generated. It may need to be adjusted if it was not newly generated. 
        /// </summary>
        /// <param name="plane">the plane intended to enclose all game objects of the city; the y co-ordinate of the plane will be 0</param>
        /// <returns>true if the plane already existed (thus, can be re-used) and false if it was newly created</returns>
        public bool GetPlane(out GameObject plane)
        {
            bool hasPlane = currentPlane != null;
            if (!hasPlane)
            {
                currentPlane = _graphRenderer.NewPlane(gameObjects, city.transform.position.y);
            }
            plane = currentPlane;
            return hasPlane;
        }

        /// <summary>
        /// Adjusts the current plane so that all current game objects managed here 
        /// fit onto it. Height and y co-ordinate will be maintained. Only its 
        /// x and z co-ordinates will be adjusted.
        /// </summary>
        public void AdjustPlane()
        {
            _graphRenderer.AdjustPlane(currentPlane, gameObjects);
        }

        /// <summary>
        /// Determines the new <paramref name="centerPosition"/> and <paramref name="scale"/> for the 
        /// plane so that it would enclose all cached game objects of the city where
        /// the y co-ordinate and the height of the plane would remain the same. The plane itself
        /// is not actually changed.
        /// </summary>
        /// <param name="centerPosition">the new center of the plane</param>
        /// <param name="scale">the new scale of the plane</param>
        public void GetPlaneTransform(out Vector3 centerPosition, out Vector3 scale)
        {
            _graphRenderer.GetPlaneTransform(currentPlane, gameObjects, out centerPosition, out scale);
        }

        /// <summary>
        /// Sets <paramref name="gameNode"/> to a cached GameObject for a leaf or inner node 
        /// or creates a new one if none has existed. The game object is identified
        /// by the attribute ID of <paramref name="node"/>.
        /// If a game object existed already, the given <paramref name="node"/> will be
        /// attached to <paramref name="gameNode"/> replacing its previously attached graph
        /// node and that previously attached graph node will be returned. If no such game object
        /// existed before, <paramref name="node"/> will be attached to the new game object
        /// and null will be returned.
        /// </summary>
        /// <param name="node">the node to be represented by <paramref name="gameNode"/></param>
        /// <param name="gameNode">the resulting GameObject representing <paramref name="node"/></param>
        /// <returns>the formerly attached graph node of <paramref name="gameNode"/> if 
        /// such a game object existed or null if the game node was newly created</returns>
        public Node GetNode(Node node, out GameObject gameNode)
        {
            if (nodes.TryGetValue(node.ID, out gameNode))
            {
                // The game object has already a node attached to it, but that
                // node is part of a different graph (i.e,, different revision).
                // That is why we replace the attached node by this node here.
                Node result = ReattachNode(gameNode, node);
                return result;
            }
            else
            {
                if (node.IsLeaf())
                {
                    // NewLeafNode() will attach node to gameNode and will 
                    // also set the scale and style of gameNode.
                    gameNode = _graphRenderer.NewLeafNode(node);
                }
                else
                {
                    // NewInnerNode() will attach node to gameNode.
                    gameNode = _graphRenderer.NewInnerNode(node);
                    // Note: The scale of inner nodes will be adjusted later when
                    // we have the layout. 
                }
                // Add the newly created gameNode to the cache.
                nodes[node.ID] = gameNode;
                return null;
            }
        }

        /// <summary>
        /// Re-attaches the given <paramref name="node"/> to the given <paramref name="gameObject"/>,
        /// that is, the NodeRef component of <paramref name="gameObject"/> will refer to 
        /// <paramref name="node"/> afterwards. Returns the node formerly attached to 
        /// <paramref name="gameObject"/> if there was one or null if there was none.
        /// </summary>
        /// <param name="gameObject">the game object where the node is to be attached to</param>
        /// <param name="node">the node to be attached</param>
        /// <returns>the node formerly attached to <paramref name="gameObject"/> or null</returns>
        private static Node ReattachNode(GameObject gameObject, Node node)
        {
            Node formerNode = null;

            NodeRef noderef = gameObject.GetComponent<NodeRef>();
            if (noderef == null)
            {
                // noderef should not be null
                Debug.LogErrorFormat("Re-used game object for node '{0}' does not have a graph node attached to it\n",
                                     node.ID);
                noderef = gameObject.AddComponent<NodeRef>();
            }
            else
            {
                formerNode = noderef.Value;
            }
            noderef.Value = node;
            return formerNode;
        }

        /// <summary>
        /// Removes the game object representing the given <paramref name="node"/> by using the ID 
        /// of the <paramref name="node"/> and returns the removed node in <paramref name="gameObject"/>, if 
        /// it existed. Returns true if such a game object existed in the cache.
        /// </summary>
        /// <param name="node">node determining the game object to be removed from the cache</param>
        /// <param name="gameObject">the corresponding game object that was removed from the cache or null</param>
        /// <returns>true if a corresponding game object existed and was removed from the cache</returns>
        public bool RemoveNode(Node node, out GameObject gameObject)
        {
            node.AssertNotNull("node");
            
            bool wasNodeRemoved = nodes.TryGetValue(node.ID, out gameObject);
            // Create power beam for deleted node
            Vector3 powerBeamDimensions = gameObject.transform.position;
            MoveScaleShakeAnimator.BeamAnimator.GetInstance().CreatePowerBeam(powerBeamDimensions, 
                AdditionalBeamDetails.deletedBeamColor, AdditionalBeamDetails.powerBeamDimensions);
            nodes.Remove(node.ID);
            return wasNodeRemoved;
        }

        /// <summary>
        /// The list of edges rendered for this graph.
        /// </summary>
        private ICollection<GameObject> edges;

        /// <summary>
        /// Renders all edges for the nodes in the node cache according to the settings.
        /// If edges for these nodes existed already, their game objects are destroyed first.
        /// </summary>
        public void RenderEdges()
        {
            ClearEdges();
            // FIXME: Provide meaningful values for scaleFactor.
            edges = _graphRenderer.EdgeLayout(nodes.Values);
        }

        /// <summary>
        /// Clears the internal cache containing all game objects created by GetInnerNode(),
        /// GetLeaf(), GetNode(), or GetPlane() and also destroys those game objects.
        /// </summary>
        public void Clear()
        {
            ClearPlane();
            ClearNodes();
            ClearEdges();
        }

        /// <summary>
        /// Destroys the game object created for the plane (if one exists). 
        /// Postcondition: currentPlane = null.
        /// </summary>
        private void ClearPlane()
        {
            if (currentPlane ?? true)
            {
                Destroyer.DestroyGameObject(currentPlane);
                currentPlane = null;
            }
        }

        /// <summary>
        /// Destroys all game objects created for nodes. Clears the node cache.
        /// </summary>
        private void ClearNodes()
        {
            foreach (GameObject gameObject in nodes.Values)
            {
                Destroyer.DestroyGameObject(gameObject);
            }
            nodes.Clear();
        }

        /// <summary>
        /// Destroys all game objects created for edges.
        /// Postcondition: 
        /// </summary>
        private void ClearEdges()
        {
            if (edges != null)
            {
                foreach (GameObject gameObject in edges)
                {
                    Destroyer.DestroyGameObject(gameObject);
                }
                // edges will be overridden in RenderEdges() each time, that is why we
                // do not Clear() it but reset it to null
                edges = null;
            }
        }
    }
}