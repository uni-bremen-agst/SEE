using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Creates new game objects representing graph nodes or deleting these again,
    /// respectively.
    /// </summary>
    public static class GameNodeAdder
    {
        /// <summary>
        /// Creates and returns a new graph node with a random unique ID,
        /// an empty source name, and an unknown node type. This node is
        /// not yet in any graph.
        /// </summary>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, an empty ID will be used</param>
        /// <returns>new graph node</returns>
        private static Node NewGraphNode(string nodeID)
        {
            string ID = string.IsNullOrEmpty(nodeID) ? Guid.NewGuid().ToString() : nodeID;
            return new Node()
            {
                ID = ID,
                SourceName = string.Empty,
                Type = Graph.UnknownType
            };
        }

        /// <summary>
        /// Adds a <paramref name="node"/> as a child of <paramref name="parent"/> to the
        /// graph containing <paramref name="parent"/> with a random unique ID.
        ///
        /// If node has no ID yet (null or empty), a random unique ID will be used. If it has
        /// an ID, that ID will be kept. In case this ID is not unique, an exception will
        /// be thrown.
        ///
        /// Precondition: <paramref name="parent"/> must not be null, neither may
        /// <paramref name="parent"/> and <paramref name="node"/> be equal; otherwise an exception
        /// will be thrown.
        /// </summary>
        /// <param name="parent">The node that should be the parent of <paramref name="node"/></param>
        /// <param name="node">The node to add to the graph</param>
        private static void AddNodeToGraph(Node parent, Node node)
        {
            if (parent == null)
            {
                throw new Exception("Parent must not be null.");
            }
            else if (parent == node)
            {
                throw new Exception("Node must not be its own parent.");
            }
            else
            {
                Graph graph = parent.ItsGraph;
                if (graph == null)
                {
                    throw new Exception("Parent must be in a graph.");
                }
                if (string.IsNullOrEmpty(node.ID))
                {
                    // Loop until the node.ID is unique within the graph.
                    node.ID = Guid.NewGuid().ToString();
                    while (graph.GetNode(node.ID) != null)
                    {
                        node.ID = Guid.NewGuid().ToString();
                    }
                }
                graph.AddNode(node);
                parent.AddChild(node);
            }
        }

        /// <summary>
        /// Creates and returns a new game node as a child of <paramref name="parent"/> having
        /// a nodeRef referencing <paramref name="node"/> at the given <paramref name="position"/>
        /// with the given <paramref name="worldSpaceScale"/>.
        /// 
        /// If the <paramref name="parent"/> <paramref name="isLeaf"/>, the parent needs to change 
        /// from a leafNode to an innerNode. Therefore all Data will be stored and the old leafNode
        /// will be deleted. Afterwards a new node with the same attributes will be created.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference.
        /// </summary>
        /// <param name="parent">parent of the new node</param>
        /// <param name="node">the graph node that is represented by the new node</param>
        /// <param name="position">the position in world space for the center point of the new game node</param>
        /// <param name="worldSpaceScale">the scale in world space of the new game node</param>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, a random ID will be used</param>
        /// <param name="isLeaf">Status of parent</param>
        /// <returns>new child game node or null if none could be created</returns>
        public static GameObject Add(GameObject parent, Vector3 position, Vector3 worldSpaceScale, string nodeID = null, bool isLeaf = false)
        {
            SEECity city = parent.ContainingCity();
            if (city != null)
            {
                Transform parentTrans = parent.transform;

                if (isLeaf)
                {
                    //preparing transition of leafNode to innerNode
                    //therefore its parent (grandparent) is needed
                    Transform grandparentTrans = parentTrans.parent.transform;
                    //saving data
                    Node oldNode = parent.GetNode();
                    String cache = oldNode.ID;
                    Node grandparentNode = oldNode.Parent;
                    //destroy the old LeafNode as GameObject and as Node
                    Remove(parent);
                    GameObject.Destroy(parent);
                    //creating parent as innerNode and adding it
                    Node parentNode = NewGraphNode(cache);
                    AddNodeToGraph(grandparentNode, parentNode);
                    parent = city.Renderer.DrawInnerNode(parentNode);
                    parentTrans.position = new Vector3(parentTrans.position.x, grandparentTrans.position.y, parentTrans.position.z);
                    parentTrans.localScale = new Vector3(parentTrans.localScale.x, grandparentTrans.lossyScale.y, parentTrans.localScale.z);
                    parentTrans.SetParent(grandparentTrans.parent);
                    //synching new innerNode
                    GameNodeMover.FinalizePosition(parent, parent.transform.position);
                    GameNodeMover.NetworkFinalizeNodePosition(parent, grandparentTrans.name, parent.transform.position);
                }

                Node node = NewGraphNode(nodeID);
                AddNodeToGraph(parent.GetNode(), node);
                GameObject result = city.Renderer.DrawLeafNode(node);
                result.transform.localScale = worldSpaceScale;
                result.transform.position = new Vector3(position.x, parentTrans.position.y , position.z);
                
                result.transform.SetParent(parentTrans);
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Inverse operation of <see cref="Add(GameObject, Vector3, Vector3, string)"/>.
        /// Removes the given <paramref name="gameNode"/> from the scene and its associated
        /// graph node from its graph.
        ///
        /// Notes:
        ///
        /// <paramref name="gameNode"/> is not actually destroyed.
        ///
        /// If <paramref name="gameNode"/> represents an inner node of the node
        /// hierarchy, its ancestors will not be deleted.
        ///
        /// Precondition: <paramref name="gameNode"/> must have a valid NodeRef; otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <param name="gameNode">game node to be removed</param>
        public static void Remove(GameObject gameNode)
        {
            Node node = gameNode.GetNode();
            Graph graph = node.ItsGraph;
            graph.RemoveNode(node);
        }
    }
}
