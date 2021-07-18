using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.Game.Architecture
{
    /// <summary>
    /// Static utils class containing heavily used helper functions while rendering the graph.
    /// </summary>
    public static class RendererUtils
    {
        /// <summary>
        /// Calculates the bounding box for the given list of game nodes.
        /// </summary>
        /// <param name="gameNodes">The game objects the bounding box should enclose</param>
        /// <param name="leftLowerCorner">The 2D-Position of the left lower corner</param>
        /// <param name="rightUpperCorner">The 2D-Position of the right upper corner</param>
        /// <param name="nodeFactories">The <see cref="ArchitectureElementType"/> to <see cref="NodeFactory"/> mapping.</param>
        /// <param name="typeToElementType">The string node type to <see cref="ArchitectureElementType"/> mapping</param>
        /// <exception cref="Exception">Thrown when an unhandled node type was found.</exception>
        public static void ArchitectureBoundingBox(ICollection<GameObject> gameNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner, NodeFactory[] nodeFactories, Dictionary<string, ArchitectureElementType> typeToElementType)
        {
            //TODO refactor into static helper class
            if (gameNodes.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (GameObject go in gameNodes)
                {
                    Node node = go.GetNode();
                    if (typeToElementType.TryGetValue(node.Type, out ArchitectureElementType type))
                    {
                        NodeFactory factory = nodeFactories[(int) type];
                        Vector3 extent = factory.GetSize(go) / 2.0f;
                        // Note: position denotes the center of the object
                        Vector3 position = factory.GetCenterPosition(go);
                        {
                            // x co-ordinate of lower left corner
                            float x = position.x - extent.x;
                            if (x < leftLowerCorner.x)
                            {
                                leftLowerCorner.x = x;
                            }
                        }
                        {
                            // z co-ordinate of lower left corner
                            float z = position.z - extent.z;
                            if (z < leftLowerCorner.y)
                            {
                                leftLowerCorner.y = z;
                            }
                        }
                        {   // x co-ordinate of upper right corner
                            float x = position.x + extent.x;
                            if (x > rightUpperCorner.x)
                            {
                                rightUpperCorner.x = x;
                            }
                        }
                        {
                            // z co-ordinate of upper right corner
                            float z = position.z + extent.z;
                            if (z > rightUpperCorner.y)
                            {
                                rightUpperCorner.y = z;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Caught unhandled node type {node.Type}");
                    }
                    
                }
            }
        }
        
        /// <summary>
        /// Adds all <paramref name="children"/> as a child to <paramref name="parent"/>.
        /// </summary>
        /// <param name="children">children to be added</param>
        /// <param name="parent">new parent of children</param>
        public static void AddToParent(ICollection<GameObject> children, GameObject parent)
        {
            foreach (GameObject child in children)
            {
                AddToParent(child, parent);
            }
        }
        
        /// <summary>
        /// Adds <paramref name="child"/> as a child to <paramref name="parent"/>,
        /// maintaining the world position of <paramref name="child"/>.
        /// </summary>
        /// <param name="child">child to be added</param>
        /// <param name="parent">new parent of child</param>
        public  static void AddToParent(GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.transform, true);
        }

        /// <summary>
        /// Adds <paramref name="node"/> and all its transitive parent game objects
        /// tagged by Tags.Node to <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="node">The game object whose transitive parent should be queried</param>
        /// <param name="gameObjects">The result set of transitive parent game objects</param>
        public static void AddAscendants(GameObject node, HashSet<GameObject> gameObjects)
        {
            GameObject element = node;
            while (element != null && element.CompareTag(Tags.Node))
            {
                gameObjects.Add(element);
                element = element.transform.parent.gameObject;
            }
        }
        
        
        /// <summary>
        /// Creates the same nesting of all game objects in <paramref name="nodeMap"/> as in
        /// the graph node hierarchy. Every root node in the graph node hierarchy will become
        /// a child of the given <paramref name="root"/>.
        /// </summary>
        /// <param name="nodeMap">mapping of graph nodes onto their representing game objects</param>
        /// <param name="root">the parent of every game object not nested in any other game object</param>
        public static void CreateObjectHierarchy(Dictionary<Node, GameObject> nodeMap, GameObject root)
        {
            //TODO refactor into static helper class
            foreach (KeyValuePair<Node, GameObject> entry in nodeMap)
            {
                Node node = entry.Key;
                Node parent = node.Parent;

                if (parent == null)
                {
                    // node is a root => it will be added to parent as a child
                    entry.Value.transform.SetParent(root.transform, true);
                }
                else
                {
                    // node is a child of another game node
                    try
                    {
                        entry.Value.transform.SetParent(nodeMap[parent].transform, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Exception raised while adding the game object corresponding to {0} to the parent {1}: {2}\n", node.ID, parent.ID, e);
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds a LOD group to <paramref name="gameObject"/> with only a single LOD.
        /// This is used to cull the object if it gets too small. The percentage
        /// by which to cull is retrieved from <see cref="settings.LODCulling"/>
        /// </summary>
        /// <param name="gameObject">object where to add the LOD group</param>
        public static void AddLOD(GameObject go)
        {
            LODGroup lodGroup = go.AddComponent<LODGroup>();
            LOD[] lods = new LOD[1];
            Renderer[] renderers = new Renderer[1];
            renderers[0] = go.GetComponent<Renderer>();
            lods[0] = new LOD(0.01f, renderers);
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
        
        
        /// <summary>
        /// Adds a LOD group to each element in the passed list.
        /// </summary>
        /// <param name="gameObjects">The elements to add the LOD group to</param>
        public static void AddLOD(ICollection<GameObject> gameObjects)
        {
            gameObjects.ForEach(AddLOD);
        }
        
        /// <summary>
        /// Returns the list of layout edges for all edges in between <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">set of game nodes whose connecting edges are requested</param>
        /// <returns>list of layout edges/returns>
        public static ICollection<LayoutEdge> ConnectingEdges(ICollection<GameNode> gameNodes)
        {
            ICollection<LayoutEdge> edges = new List<LayoutEdge>();
            Dictionary<Node, GameNode> map = NodeToGameNodeMap(gameNodes);
            foreach (GameNode source in gameNodes)
            {
                Node sourceNode = source.ItsNode;

                foreach (Edge edge in sourceNode.Outgoings)
                {
                    Node target = edge.Target;
                    edges.Add(new LayoutEdge(source, map[target], edge));
                }
            }
            return edges;
        }
        
        /// <summary>
        /// Returns a mapping of each graph Node onto its containing GameNode for every
        /// element in <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>mapping of graph node onto its corresponding game node</returns>
        public static Dictionary<Node, GameNode> NodeToGameNodeMap(ICollection<GameNode> gameNodes)
        {
            Dictionary<Node, GameNode> map = new Dictionary<Node, GameNode>();
            foreach (GameNode node in gameNodes)
            {
                map[node.ItsNode] = node;
            }
            return map;
        }
        
        /// <summary>
        /// Returns the child object of <paramref name="codeCity"/> tagged by Tags.Node.
        /// If there is no such child or if there are more than one, an exception will
        /// be thrown.
        /// </summary>
        /// <param name="codeCity">game object representing a code city</param>
        /// <returns>child object of <paramref name="codeCity"/> tagged by Tags.Node</returns>
        public static GameObject RootGameNode(GameObject codeCity)
        {
            
            GameObject result = null;
            foreach (Transform child in codeCity.transform)
            {
                if (child.tag == Tags.Node)
                {
                    if (result == null)
                    {
                        result = child.gameObject;
                    }
                    else
                    {
                        throw new Exception("Code city " + codeCity.name + " has multiple children tagged by " + Tags.Node
                                            + ": " + result.name + " and " + child.name);
                    }
                }
            }
            if (result == null)
            {
                throw new Exception("Code city " + codeCity.name + " has no child tagged by " + Tags.Node);
            }

            return result;
        }
    }
}