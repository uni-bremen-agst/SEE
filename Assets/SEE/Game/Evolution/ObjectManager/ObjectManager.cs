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

using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.GameObjects;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// An <see cref="ObjectManager"/> creates and manages GameObjects by using a supplied
    /// <see cref="GraphRenderer"/> to create game objects for graph nodes. Those game objects
    /// will be cached, that is, non-existing GameObjects are created and stored
    /// for reuse during query. Each GameObject is identified by the ID of
    /// a node and can be retrieved via any node with the same ID.
    /// </summary>
    internal class ObjectManager
    {
        /// <summary>
        /// The graph renderer used to create the game objects. It is used for creating missing
        /// game objects.
        /// </summary>
        private readonly GraphRenderer graphRenderer;

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
        /// nodes contained may be an accumulation of all nodes created and added by
        /// <see cref="GetNode(Node, out GameObject)"/> so far and not just those of one
        /// single graph in the graph series (unless a node was removed by
        /// <see cref="RemoveNode(Node, out GameObject)"/> meanwhile).
        /// </summary>
        private readonly Dictionary<string, GameObject> nodes = new();

        /// <summary>
        /// A dictionary containing all created edges that are currently in use. The set of
        /// edges contained may be an accumulation of all edges created and added by
        /// <see cref="GetEdge(Edge, out GameObject)"/> so far and not just those of one single
        /// graph in the graph series (unless an edge was removed by
        /// <see cref="RemoveEdge(Edge, out GameObject)"/> meanwhile).
        /// </summary>
        private readonly Dictionary<string, GameObject> edges = new();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="renderer">the graph renderer used to create the game objects</param>
        /// <param name="city">the game object representing the city; its position and scale determines
        /// the position and scaling of the city elements visualized by the renderer</param>
        public ObjectManager(GraphRenderer renderer, GameObject city)
        {
            renderer.AssertNotNull("renderer");
            graphRenderer = renderer;
            this.city = city;
        }

        /// <summary>
        /// Sets <paramref name="gameNode"/> to a cached GameObject or creates a new one
        /// if none has existed yet. The game object is identified by the attribute <see cref="Node.ID"/>
        /// of <paramref name="node"/>.
        /// If a game object existed already, the given <paramref name="node"/> will be
        /// attached to <paramref name="gameNode"/> replacing its previously attached graph
        /// node and that previously attached graph node will be returned. If no such game object
        /// existed before, <paramref name="node"/> will be attached to the new game object
        /// and <c>null</c> will be returned.
        /// </summary>
        /// <param name="node">the node to be represented by <paramref name="gameNode"/></param>
        /// <param name="gameNode">the resulting GameObject representing <paramref name="node"/></param>
        /// <returns>the formerly attached graph node of <paramref name="gameNode"/> if
        /// such a game object existed or <c>null</c> if the game node was newly created</returns>
        public Node GetNode(Node node, out GameObject gameNode)
        {
            if (nodes.TryGetValue(node.ID, out gameNode))
            {
                // A game node with the requested ID exists already, which can
                // be re-used.

                // The game object has already a node attached to it, but that
                // node is part of a different graph (i.e,, different revision).
                // That is why we replace the attached node by this node here.
                return GraphElementReattacher.ReattachNode(gameNode, node);
            }
            else
            {
                // There is no game node with the given node ID, hence, we need to
                // create a new one.
                gameNode = graphRenderer.DrawNode(node, city);
                // Add the newly created gameNode to the cache.
                nodes[node.ID] = gameNode;
                return null;
            }
        }

        /// <summary>
        /// Sets <paramref name="gameEdge"/> to a cached GameObject for an edge
        /// or creates a new one if none has existed yet. The game object is identified
        /// by the attribute <see cref="Edge.ID"/> of <paramref name="edge"/>.
        /// If a game object existed already, the given <paramref name="edge"/> will be
        /// attached to <paramref name="gameEdge"/> replacing its previously attached graph
        /// edge and that previously attached graph edge will be returned. If no such game object
        /// existed before, <paramref name="edge"/> will be attached to the new game object
        /// and <c>null</c> will be returned.
        /// </summary>
        /// <param name="edge">the edge to be represented by <paramref name="gameEdge"/></param>
        /// <param name="gameEdge">the resulting GameObject representing <paramref name="edge"/></param>
        /// <returns>the formerly attached graph edge of <paramref name="gameEdge"/> if
        /// such a game object existed or <c>null</c> if the game edge was newly created</returns>
        public Edge GetEdge(Edge edge, out GameObject gameEdge)
        {
            if (edges.TryGetValue(edge.ID, out gameEdge))
            {
                // A game edge with the requested ID exists already, which can
                // be re-used.

                // The game object has already an edge attached to it, but that
                // edge is part of a different graph (i.e,, different revision).
                // That is why we replace the attached edge by this edge here.
                return GraphElementReattacher.ReattachEdge(gameEdge, edge);
            }
            else
            {
                // A game edge for this graph edge does not exist yet, hence, we need to create one.
                // In order to create the requested game edge, the entire edge layout must be calculated.
                // Yet, this needs to be done only once when we move from one graph to the next one
                // for the very first graph edge that does not have a game edge. To manage that we put
                // all recalculated edges into 'edges' and not just the game edge created specifically
                // for this particular graph edge at hand.

                // Important assumption: The node positions do not change between consecutive calls
                // to GetEdge() when moving from one graph to the next one.

                // Determine all game nodes.
                List<GameObject> gameNodes = new();
                foreach (Node node in edge.ItsGraph.Nodes())
                {
                    GetNode(node, out GameObject gameNode);
                    gameNodes.Add(gameNode);
                }

                // Create the entire edge layout from the game nodes and
                // put all game edges into the cache 'edges' (if not already present) and find `gameEdge'.
                foreach (GameObject newGameEdge in graphRenderer.EdgeLayout(gameNodes, city, false))
                {
                    string edgeID = newGameEdge.GetComponent<EdgeRef>().Value.ID;
                    if (!edges.TryAdd(edgeID, newGameEdge))
                    {
                        // Edge object has already been created in previous call.
                        Destroyer.Destroy(newGameEdge);
                    }
                    else
                    {
                        // FIXME (@koschke): This should be rewritten so that the GraphElementIDMap isn't called here.
                        //                   That part should be handled by GraphRenderer.CreateGameNode.
                        GraphElementIDMap.Add(newGameEdge);
                        // We need to set the portal ourselves. The portal cannot be derived from
                        // SceneQueries.GetCityRootNode(city) because the nodes are not yet
                        // children of the city. The node hierarchy will be established only at
                        // the end of an animation cycle.
                        Portal.GetDimensions(city, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
                        Portal.SetPortal(newGameEdge, leftFrontCorner, rightBackCorner);
                    }
                    // FIXME: if edges.ContainsKey(edgeID), newGameEdge will be destroyed.
                    // How can we then assign newGameEdge to gameEdge?
                    if (edgeID == edge.ID)
                    {
                        gameEdge = newGameEdge;
                    }
                }

                return null;
            }
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
            nodes.Remove(node.ID);
            return wasNodeRemoved;
        }

        /// <summary>
        /// Removes the game object representing the given <paramref name="edge"/> by using the ID
        /// of the <paramref name="edge"/> and returns the removed edge in <paramref name="gameObject"/>, if
        /// it existed. Returns true if such a game object existed in the cache.
        /// </summary>
        /// <param name="edge">edge determining the game object to be removed from the cache</param>
        /// <param name="gameObject">the corresponding game object that was removed from the cache or null</param>
        /// <returns>true if a corresponding game object existed and was removed from the cache</returns>
        public bool RemoveEdge(Edge edge, out GameObject gameObject)
        {
            edge.AssertNotNull("edge");

            bool wasEdgeRemoved = edges.TryGetValue(edge.ID, out gameObject);
            edges.Remove(edge.ID);
            return wasEdgeRemoved;
        }

        /// <summary>
        /// Clears the internal cache containing all game objects created for nodes
        /// and edges as well as the plane and also destroys those game objects.
        /// </summary>
        public void Clear()
        {
            ClearPlane();
            ClearNodes();
            ClearEdges();
        }

        /// <summary>
        /// Destroys the game object created for the <see cref="currentPlane"/> (if one exists).
        /// Postcondition: <see cref="currentPlane"/> equals <c>null</c>.
        /// </summary>
        private void ClearPlane()
        {
            if (currentPlane != null)
            {
                Destroyer.Destroy(currentPlane);
                currentPlane = null;
            }
        }

        /// <summary>
        /// Destroys all game objects in <see cref="nodes"/>.
        /// Clears the node cache <see cref="nodes"/>.
        /// </summary>
        private void ClearNodes()
        {
            foreach (GameObject gameObject in nodes.Values)
            {
                Destroyer.Destroy(gameObject);
            }
            nodes.Clear();
        }

        /// <summary>
        /// Destroys all game objects in <see cref="edges"/>.
        /// Clears the edge cache <see cref="edges"/>.
        /// </summary>
        private void ClearEdges()
        {
            foreach (GameObject gameObject in edges.Values)
            {
                Destroyer.Destroy(gameObject);
            }
            edges.Clear();
        }
    }
}
